using NuclearOption.DebugScripts;
using UnityEngine;

public class AutopilotTiltwing : Autopilot
{
	private PID collectivePIDController;

	[SerializeField]
	private PIDFactors tiltPIDFactors;

	[SerializeField]
	private PIDFactors collectivePIDFactors;

	[SerializeField]
	private float maxTilt = 0.2f;

	private PID3D tiltPID;

	private Vector3 tiltTarget;

	private Vector3 yawVector;

	private float waypointGradient;

	private GameObject destinationDebug;

	public override void Awake()
	{
		base.Awake();
		terrainWarning = new TerrainWarningSystem(aircraft);
		collectivePIDController = new PID(collectivePIDFactors);
		tiltPID = new PID3D();
		if (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			controlInputs.throttle = 0.5f;
		}
	}

	public override void BoresightAim(GlobalPosition aimPoint, float altitudeHold)
	{
		AutoAim(aimPoint, altitudeHold, aimPoint - aircraft.GlobalPosition(), Vector3.zero, followTerrain: true);
	}

	public override void AutoAim(GlobalPosition destination, float altitudeHold, Vector3 aimDirection, Vector3 targetVelocity, bool followTerrain)
	{
		float t = aircraft.speed * 2f / aircraftParameters.maxSpeed;
		if (exclusionZoneChecker.TryGetWarning(out var avoidanceVector))
		{
			destination = aircraft.GlobalPosition() + avoidanceVector.normalized * 10000f;
			controlInputs.throttle = 1f;
		}
		destination.y = Mathf.Max(destination.y, aircraft.definition.spawnOffset.y + 2f);
		Vector3 vector = destination - aircraft.GlobalPosition();
		float magnitude = new Vector3(vector.x, 0f, vector.z).magnitude;
		altitudeHold *= Mathf.Clamp01(magnitude * 0.005f + aircraft.speed * 0.01f - 1f);
		Vector3 vector2 = aircraft.rb.velocity - targetVelocity;
		Vector3 vector3 = vector;
		vector3.y = 0f;
		_ = vector3.normalized;
		float num = Mathf.SmoothStep(0f, 1f, t);
		if (Time.timeSinceLevelLoad - lastWaypointTime > 0.5f)
		{
			lastWaypointTime = Time.timeSinceLevelLoad;
			Vector3 vector4 = vector2 + aircraft.transform.forward * 20f;
			vector4.y = 0f;
			Vector3 direction = ((aircraft.speed < 60f) ? vector3 : Vector3.RotateTowards(vector4, vector3, 0.5f, 0f));
			if (Vector3.Angle(vector4, vector3) > 30f && aircraft.speed > 30f)
			{
				altitudeHold += 20f;
			}
			float a = Mathf.Min(Mathf.Max(aircraft.speed * aircraft.speed * 0.1f, 200f), magnitude);
			a = Mathf.Max(a, magnitude * 0.1f);
			waypoint = TerrainWaypoint(direction, altitudeHold, a);
			waypointGradient = (waypoint - aircraft.GlobalPosition()).y / a;
		}
		bool flag = false;
		if (magnitude > 500f && waypointGradient > 0.1f && aircraft.speed < 60f)
		{
			flag = true;
			controlInputs.customAxis1 = 0.25f;
		}
		if (terrainWarning.urgency > 0f)
		{
			controlInputs.customAxis1 = 0.18f;
			flag = true;
		}
		if (flag)
		{
			aircraft.SetFlightAssist(enabled: false);
		}
		else
		{
			aircraft.SetFlightAssist(enabled: true);
		}
		float value = (followTerrain ? (aircraft.radarAlt - altitudeHold) : (0f - vector.y - altitudeHold));
		terrainWarning.CheckTerrain();
		waypoint += (Vector3)(Vector2.up * terrainWarning.urgency * 50f);
		waypointDelta = waypoint - aircraft.GlobalPosition();
		num *= Mathf.Clamp01((magnitude - 500f) * 0.0005f);
		Vector3 localAngularVelocity = aircraft.rb.transform.InverseTransformDirection(aircraft.rb.angularVelocity);
		_ = new Vector3(waypointDelta.x, 0f, waypointDelta.y).normalized;
		Vector3 normalized = aircraft.rb.velocity.normalized;
		value = Mathf.Clamp(value, -40f, 20f);
		Vector3 vector5 = waypointDelta.normalized * aircraft.speed - vector2;
		vector5 += Vector3.up * Mathf.Max(0f - value, 0f) * 0.5f;
		float num2 = maxTilt;
		num2 /= 1f + terrainWarning.urgency * 0.25f;
		tiltTarget = tiltPID.GetOutputSqrt(vector3, 50f, Time.fixedDeltaTime, tiltPIDFactors);
		if (terrainWarning.urgency > 0f)
		{
			tiltTarget -= new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z);
		}
		Vector3 vector6 = Vector3.up + Vector3.ClampMagnitude(tiltTarget, num2);
		float angleOnAxis = TargetCalc.GetAngleOnAxis(aircraft.transform.up, vector6, aircraft.transform.right);
		float angleOnAxis2 = TargetCalc.GetAngleOnAxis(normalized, waypointDelta, aircraft.transform.right);
		yawVector = waypointDelta;
		float angleOnAxis3 = TargetCalc.GetAngleOnAxis(aircraft.transform.forward, yawVector, aircraft.transform.up);
		float angleOnAxis4 = TargetCalc.GetAngleOnAxis(aircraft.cockpit.xform.forward, waypointDelta, aircraft.cockpit.xform.up);
		float angleOnAxis5 = TargetCalc.GetAngleOnAxis(aircraft.transform.up, vector6, -aircraft.transform.forward);
		Vector3 vector7 = -Vector3.Cross(aircraft.cockpit.xform.forward, Vector3.up);
		Vector3 other = Vector3.up + 0.1f * Mathf.Clamp(angleOnAxis4 * 30f, -3f, 3f) * vector7;
		float angleOnAxis6 = TargetCalc.GetAngleOnAxis(aircraft.transform.up, other, -aircraft.transform.forward);
		hoverController.ApplyInputs(controlInputs, new Vector3(angleOnAxis, angleOnAxis3, angleOnAxis5), localAngularVelocity);
		forwardFlightController.ApplyInputs(controlInputs, aircraft.speed, new Vector3(angleOnAxis2, angleOnAxis4, angleOnAxis6), num);
		value += Mathf.Clamp(vector2.y * 3f, -15f, 15f);
		value -= vector5.y;
		float num3 = 0.5f;
		num3 -= value * 0.15f;
		if (followTerrain)
		{
			num3 += terrainWarning.urgency;
		}
		float num4 = Mathf.Sqrt(magnitude) * 1.7f;
		float b = 0.5f + (num4 - aircraft.speed) * 0.1f;
		float num5 = Mathf.Lerp(num3, b, num);
		controlInputs.throttle += collectivePIDController.GetOutput(num5 - controlInputs.throttle, 0.25f, Time.fixedDeltaTime, aircraftParameters.collectivePID);
		controlInputs.throttle = Mathf.Clamp01(controlInputs.throttle);
		if (terrainWarning.urgency > 0f)
		{
			controlInputs.throttle = 1f;
		}
		if (DebugVis.Enabled && SceneSingleton<CameraStateManager>.i.followingUnit == aircraft)
		{
			DebugVis.Create(ref waypointDebug, GameAssets.i.waypointDebug, Datum.origin);
			DebugVis.Create(ref aimVectorDebug, GameAssets.i.debugArrow, aircraft.cockpit.transform);
			DebugVis.Create(ref vectorDebug2, GameAssets.i.debugArrow, Datum.origin);
			if (DebugVis.Create(ref destinationDebug, GameAssets.i.debugPoint, Datum.origin))
			{
				destinationDebug.transform.localScale = Vector3.one * 10f;
			}
			aimVectorDebug.transform.position = aircraft.transform.position + aircraft.transform.up * 2f;
			aimVectorDebug.transform.rotation = Quaternion.LookRotation(vector6);
			aimVectorDebug.transform.localScale = new Vector3(2f, 2f, 2f);
			destinationDebug.transform.position = waypoint.ToLocalPosition();
			Debug.Log($"destinationDist: {magnitude}, altitudeHold: {altitudeHold}");
		}
		aircraft.FilterInputs();
	}
}
