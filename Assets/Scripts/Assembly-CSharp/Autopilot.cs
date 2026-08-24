using System;
using NuclearOption;
using UnityEngine;

public class Autopilot : MonoBehaviour
{
	[Serializable]
	protected class ForwardFlightController
	{
		public bool Enabled;

		[SerializeField]
		private float referenceAirspeed;

		[SerializeField]
		private PIDFactors pitchFlightPID;

		[SerializeField]
		private PIDFactors yawFlightPID;

		[SerializeField]
		private PIDFactors rollFlightPID;

		private AeroPID xPID;

		private AeroPID yPID;

		private AeroPID zPID;

		public void Initialize()
		{
			if (Enabled)
			{
				xPID = new AeroPID(pitchFlightPID, referenceAirspeed);
				yPID = new AeroPID(yawFlightPID, referenceAirspeed);
				zPID = new AeroPID(rollFlightPID, referenceAirspeed);
			}
		}

		public void ApplyInputs(ControlInputs inputs, float airspeed, Vector3 error)
		{
			inputs.pitch = Mathf.Clamp(xPID.GetOutputClampP(error.x, 20f, 5f, airspeed), -1f, 1f);
			inputs.yaw = Mathf.Clamp(yPID.GetOutputClampP(error.y, 60f, 5f, airspeed), -1f, 1f);
			inputs.roll = Mathf.Clamp(zPID.GetOutputClampP(error.z, 60f, 5f, airspeed), -1f, 1f);
		}

		public void ApplyInputs(ControlInputs inputs, float airspeed, Vector3 error, float opacity)
		{
			inputs.pitch = Mathf.Lerp(inputs.pitch, Mathf.Clamp(xPID.GetOutput(error.x, 3f, airspeed), -1f, 1f), opacity);
			inputs.yaw = Mathf.Lerp(inputs.yaw, Mathf.Clamp(yPID.GetOutput(error.y, 3f, airspeed), -1f, 1f), opacity);
			inputs.roll = Mathf.Lerp(inputs.roll, Mathf.Clamp(zPID.GetOutput(error.z, 3f, airspeed), -1f, 1f), opacity);
		}
	}

	[Serializable]
	protected class HoverController
	{
		public bool Enabled;

		[SerializeField]
		private PIDFactors pitchHoverPID;

		[SerializeField]
		private PIDFactors yawHoverPID;

		[SerializeField]
		private PIDFactors rollHoverPID;

		public float hoverThrottle = 0.5f;

		private PID xPID;

		private PID yPID;

		private PID zPID;

		public void Initialize()
		{
			if (Enabled)
			{
				xPID = new PID(pitchHoverPID);
				yPID = new PID(yawHoverPID);
				zPID = new PID(rollHoverPID);
			}
		}

		public void ApplyInputs(ControlInputs inputs, Vector3 error, Vector3 localAngularVelocity)
		{
			inputs.pitch = Mathf.Clamp(xPID.GetOutput(error.x, 0f - localAngularVelocity.x, 0f, Time.fixedDeltaTime), -1f, 1f);
			inputs.yaw = Mathf.Clamp(yPID.GetOutput(error.y, 0f - localAngularVelocity.y, 0f, Time.fixedDeltaTime), -1f, 1f);
			inputs.roll = Mathf.Clamp(zPID.GetOutput(error.z, localAngularVelocity.z, 0f, Time.fixedDeltaTime), -1f, 1f);
		}
	}

	protected class AirspeedChecker
	{
		private float airspeed;

		private float lastCheck;

		private Aircraft aircraft;

		public AirspeedChecker(Aircraft aircraft)
		{
			this.aircraft = aircraft;
		}

		public float GetAirspeed()
		{
			if (Time.timeSinceLevelLoad - lastCheck < 0.5f)
			{
				return airspeed;
			}
			Vector3 rhs = aircraft.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(aircraft.GlobalPosition());
			airspeed = Vector3.Dot(aircraft.cockpit.xform.forward, rhs);
			return airspeed;
		}
	}

	public class ExclusionZoneChecker
	{
		private readonly Aircraft aircraft;

		private float lastCheck;

		private Vector3 avoidanceVector;

		private bool warning;

		public ExclusionZoneChecker(Aircraft aircraft)
		{
			this.aircraft = aircraft;
		}

		public bool TryGetWarning(out Vector3 avoidanceVector)
		{
			if (Time.timeSinceLevelLoad - lastCheck < 1f || aircraft.NetworkHQ == null)
			{
				avoidanceVector = this.avoidanceVector;
				return warning;
			}
			lastCheck = Time.timeSinceLevelLoad;
			this.avoidanceVector = Vector3.zero;
			warning = false;
			foreach (ExclusionZone exclusionZone in aircraft.NetworkHQ.GetExclusionZones())
			{
				if (FastMath.InRange(aircraft.GlobalPosition(), exclusionZone.position, exclusionZone.radius) || FastMath.InRange(aircraft.GlobalPosition() + aircraft.rb.velocity * 10f, exclusionZone.position, exclusionZone.radius * 1.1f))
				{
					this.avoidanceVector += (aircraft.GlobalPosition() - exclusionZone.position).normalized;
					warning = true;
				}
			}
			avoidanceVector = this.avoidanceVector;
			return warning;
		}
	}

	public Aircraft aircraft;

	protected GlobalPosition waypoint;

	protected GlobalPosition smoothedDestination;

	protected Vector3 waypointDelta;

	protected Vector3 obstacleNormal;

	protected ControlInputs controlInputs;

	protected AircraftParameters aircraftParameters;

	protected float lastWaypointTime;

	protected GameObject waypointDebug;

	protected GameObject aimVectorDebug;

	protected GameObject vectorDebug2;

	protected ExclusionZoneChecker exclusionZoneChecker;

	protected TerrainWarningSystem terrainWarning;

	protected AirspeedChecker airspeedChecker;

	private float verticalVelocitySmoothed;

	private float verticalVelocitySmoothingVel;

	[SerializeField]
	protected ForwardFlightController forwardFlightController;

	[SerializeField]
	protected HoverController hoverController;

	private Vector3 hoverErrorPrev;

	public TerrainWarningSystem GetTerrainWarningSystem()
	{
		return terrainWarning;
	}

	public virtual void Awake()
	{
		aircraft.autopilot = this;
		controlInputs = aircraft.GetInputs();
		aircraftParameters = aircraft.GetAircraftParameters();
		obstacleNormal = Vector3.up;
		exclusionZoneChecker = new ExclusionZoneChecker(aircraft);
		airspeedChecker = new AirspeedChecker(aircraft);
		forwardFlightController.Initialize();
		hoverController.Initialize();
	}

	public GlobalPosition TerrainWaypoint(Vector3 direction, float altitudeTarget, float lookAheadDistance)
	{
		direction.y = 0f;
		Vector3 vector = aircraft.transform.position + direction.normalized * lookAheadDistance;
		vector.y = Datum.LocalSeaY;
		if (Physics.Linecast(vector + Vector3.up * 5000f, vector - Vector3.up * 5000f, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask))
		{
			vector.y = Mathf.Max(hitInfo.point.y + 1f, Datum.LocalSeaY);
		}
		int num = 0;
		while (num < 6 && Physics.Linecast(vector, aircraft.transform.position, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask))
		{
			num++;
			vector += 30 * num * Vector3.up;
		}
		vector += Vector3.up * altitudeTarget;
		return vector.ToGlobalPosition();
	}

	protected virtual float TerrainAvoidanceCheck()
	{
		Vector3 vector = aircraft.transform.position - Vector3.up * aircraft.definition.spawnOffset.y * 2f;
		float num = 10f;
		if (aircraft.rb.velocity.y < 0f)
		{
			num = Mathf.Min(num, aircraft.transform.position.GlobalY() / (0f - aircraft.rb.velocity.y));
		}
		float enter;
		if (Physics.Linecast(vector, vector + aircraft.rb.velocity * 20f, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask) && hitInfo.point.y > Datum.LocalSeaY)
		{
			obstacleNormal = hitInfo.normal;
			num = Mathf.Min(num, hitInfo.distance / aircraft.speed);
		}
		else if (Datum.WaterPlane().Raycast(new Ray(aircraft.transform.position, aircraft.rb.velocity), out enter))
		{
			obstacleNormal = Vector3.up;
			num = Mathf.Min(num, enter / aircraft.speed);
		}
		return num;
	}

	public virtual void AutoLand(GlobalPosition destination, float heightAboveGlideslope, float touchdownDist, float altitudeHold)
	{
	}

	public virtual void Hover(GlobalPosition destination, float altitudeHold, Vector3 aimDirection)
	{
		Vector3 vector = destination - aircraft.GlobalPosition();
		float num = vector.y + altitudeHold;
		vector.y = 0f;
		Vector3 vector2 = (vector - hoverErrorPrev) / Time.fixedDeltaTime;
		hoverErrorPrev = vector;
		vector = Vector3.ClampMagnitude(vector, 100f);
		vector += vector2 * 6f;
		Vector3 vector3 = Vector3.up * 20f + Vector3.ClampMagnitude(vector * 0.5f, 6f);
		float angleOnAxis = TargetCalc.GetAngleOnAxis(vector3, aircraft.transform.up, aircraft.transform.forward);
		float y = 0f - TargetCalc.GetAngleOnAxis(aimDirection, aircraft.transform.forward, aircraft.transform.up);
		_ = aircraft.GetControlsFilter().ReverseThrust;
		float angleOnAxis2 = TargetCalc.GetAngleOnAxis(aircraft.transform.up, vector3, aircraft.transform.right);
		Vector3 localAngularVelocity = aircraft.rb.transform.InverseTransformDirection(aircraft.rb.angularVelocity);
		verticalVelocitySmoothed = Mathf.SmoothDamp(verticalVelocitySmoothed, aircraft.rb.velocity.y, ref verticalVelocitySmoothingVel, 0.5f);
		controlInputs.throttle = Mathf.Clamp01(hoverController.hoverThrottle + Mathf.Clamp(num * 0.25f, -1f, 1f) - Mathf.Clamp(aircraft.rb.velocity.y * 0.25f, -1f, 0.5f));
		hoverController.ApplyInputs(controlInputs, new Vector3(angleOnAxis2, y, angleOnAxis), localAngularVelocity);
		controlInputs.pitch *= 2f;
		controlInputs.roll *= 2f;
		controlInputs.yaw *= 2f;
		aircraft.FilterInputs();
	}

	public virtual void AutoAim(GlobalPosition destination, bool aimVelocity, bool ignoreCollisions, bool runwayAlign, float effort, float bankAllowed, bool followTerrain, float altitudeHold, Vector3 targetVelocity)
	{
	}

	public virtual void BoresightAim(GlobalPosition destination, float desiredHeight)
	{
	}

	public virtual void AutoAim(GlobalPosition destination, float altitudeHold, Vector3 aimDirection, Vector3 targetVelocity, bool followTerrain)
	{
	}
}
