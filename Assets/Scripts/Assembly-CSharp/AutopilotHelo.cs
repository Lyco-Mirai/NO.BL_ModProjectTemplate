using NuclearOption.DebugScripts;
using UnityEngine;

public class AutopilotHelo : Autopilot
{
	private float nominalRPM;

	public RotorShaft[] rotorShafts;

	public DuctedFan tailRotor;

	private PID collectivePIDController;

	private PID2D tiltPID;

	private bool tailRotorFailure;

	private float terrainAvoidanceUrgency;

	[SerializeField]
	private float maxTilt = 0.3f;

	[SerializeField]
	private bool compoundHelo = true;

	private GameObject destinationDebug;

	public override void Awake()
	{
		base.Awake();
		collectivePIDController = new PID(1f, 0f, 0.1f);
		obstacleNormal = Vector3.up;
		PIDFactors factors = new PIDFactors(0.001f, 0f, 0.01f);
		tiltPID = new PID2D(factors, 500f, 0.5f);
		if (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			controlInputs.throttle = 0.5f;
		}
		nominalRPM = 0f;
		RotorShaft[] array = rotorShafts;
		foreach (RotorShaft rotorShaft in array)
		{
			nominalRPM += rotorShaft.GetMaxRPM() * 0.92f;
		}
		nominalRPM /= rotorShafts.Length;
	}

	public override void BoresightAim(GlobalPosition aimPoint, float altitudeHold)
	{
		float value = aircraft.radarAlt - altitudeHold;
		value = Mathf.Clamp(value, -40f, 20f);
		Vector3 self = aimPoint - aircraft.GlobalPosition();
		Vector3 localAngularVelocity = aircraft.rb.transform.InverseTransformDirection(aircraft.rb.angularVelocity);
		Vector3 vector = self.normalized * aircraft.speed - aircraft.rb.velocity;
		_ = self.normalized - aircraft.transform.forward;
		value -= vector.y;
		float x = 0f - TargetCalc.GetAngleOnAxis(self, aircraft.transform.forward, aircraft.transform.right);
		float y = 0f - TargetCalc.GetAngleOnAxis(self, aircraft.transform.forward, aircraft.transform.up);
		Vector3 vector2 = new Vector3(aircraft.transform.right.x, 0f, aircraft.transform.right.z);
		float num = Vector3.Dot(aircraft.rb.velocity, vector2.normalized);
		Vector3 other = Vector3.up - aircraft.transform.right * Mathf.Clamp(num * 0.1f, -0.25f, 0.25f);
		float z = 0f - TargetCalc.GetAngleOnAxis(aircraft.transform.up, other, aircraft.transform.forward);
		float num2 = 0.4f - value * ((value < 0f) ? 1f : 0.02f) - aircraft.rb.velocity.y * 0.2f;
		if (rotorShafts.Length != 0)
		{
			float num3 = 0f;
			float num4 = 0f;
			RotorShaft[] array = rotorShafts;
			foreach (RotorShaft rotorShaft in array)
			{
				num3 += rotorShaft.GetVRSFactor();
				num4 += rotorShaft.GetRPM();
			}
			num4 /= (float)rotorShafts.Length;
			num3 /= (float)rotorShafts.Length;
			float num5 = num4 - nominalRPM;
			num2 += Mathf.Min(num5 * 1.5f, 0f);
			num2 += num3 * 2f;
		}
		if (compoundHelo)
		{
			float num6 = FastMath.Distance(aimPoint, aircraft.GlobalPosition());
			controlInputs.customAxis1 = num6 * 0.001f - aircraft.speed * 0.005f;
			if (aircraft.speed > 80f)
			{
				controlInputs.throttle = Mathf.Min(controlInputs.throttle, 0.5f);
			}
		}
		hoverController.ApplyInputs(controlInputs, new Vector3(x, y, z), localAngularVelocity);
		controlInputs.throttle = Mathf.Clamp01(num2);
		aircraft.FilterInputs();
	}

	public override void AutoAim(GlobalPosition destination, float altitudeHold, Vector3 aimDirection, Vector3 targetVelocity, bool followTerrain)
	{
		float num = 0f;
		float num2 = 0f;
		if (rotorShafts.Length != 0)
		{
			RotorShaft[] array = rotorShafts;
			foreach (RotorShaft rotorShaft in array)
			{
				num += rotorShaft.GetVRSFactor();
				num2 += Mathf.Min(rotorShaft.GetRPM() - nominalRPM, 0f);
			}
			num /= (float)rotorShafts.Length;
			num2 /= (float)rotorShafts.Length;
		}
		if (num > 0.4f)
		{
			destination = aircraft.GlobalPosition() + new Vector3(aircraft.transform.forward.x, 0f, aircraft.transform.forward.z) * 10000f;
		}
		float t = aircraft.speed / aircraftParameters.maxSpeed;
		Vector3 vector = destination - aircraft.GlobalPosition();
		Vector3 vector2 = aircraft.rb.velocity - targetVelocity;
		if (tailRotorFailure)
		{
			altitudeHold = 0f;
			vector = vector.normalized * 20f + aircraft.rb.velocity * aircraft.radarAlt * 2f;
			vector = destination - aircraft.GlobalPosition();
			if (aircraft.radarAlt > 10f && aircraft.gearState == LandingGear.GearState.LockedRetracted)
			{
				aircraft.SetGear(deployed: true);
			}
			if (aircraft.radarAlt < aircraft.definition.spawnOffset.y + 0.2f)
			{
				controlInputs.brake = 1f;
				controlInputs.throttle = 0f;
				return;
			}
			if (aircraft.radarAlt < 20f && aircraft.speed < 15f)
			{
				controlInputs.throttle = 0f;
				controlInputs.brake = 1f;
				return;
			}
		}
		float magnitude = vector.magnitude;
		Vector3 vector3 = vector;
		vector3.y = 0f;
		Mathf.Clamp01(Vector3.Dot(aircraft.transform.forward, vector2));
		float num3 = Mathf.SmoothStep(0f, 1f, t);
		if (Time.timeSinceLevelLoad - lastWaypointTime > 1f)
		{
			lastWaypointTime = Time.timeSinceLevelLoad;
			Vector3 current = vector2 + aircraft.transform.forward * 20f;
			current.y = 0f;
			Vector3 direction = Vector3.RotateTowards(current, vector3, 0.8f, 0f);
			terrainAvoidanceUrgency = 7.5f - TerrainAvoidanceCheck();
			terrainAvoidanceUrgency = Mathf.Max(terrainAvoidanceUrgency, 0f);
			waypoint = TerrainWaypoint(direction, altitudeHold + terrainAvoidanceUrgency, Mathf.Max(aircraft.speed, 100f) * 6f);
			if (tailRotor != null)
			{
				tailRotorFailure = tailRotor.GetRPM() < 1000f;
			}
		}
		waypointDelta = waypoint - aircraft.GlobalPosition();
		Vector3 vector4 = waypointDelta;
		vector4.y = 0f;
		float num4 = (followTerrain ? (aircraft.radarAlt - altitudeHold) : (0f - vector.y - altitudeHold));
		Vector3 vector5 = destination - aircraft.GlobalPosition();
		Vector3 vector6 = new Vector3(aircraft.transform.right.x, 0f, aircraft.transform.right.z);
		Vector2 output = tiltPID.GetOutput(new Vector2(vector5.x, vector5.z), Time.fixedDeltaTime);
		Vector3 localAngularVelocity = aircraft.rb.transform.InverseTransformDirection(aircraft.rb.angularVelocity);
		Vector3 self = Vector3.up + new Vector3(Mathf.Clamp(output.x, 0f - maxTilt, maxTilt), 0f, Mathf.Clamp(output.y, 0f - maxTilt, maxTilt)) + obstacleNormal * terrainAvoidanceUrgency * 0.2f;
		float a = 0f - TargetCalc.GetAngleOnAxis(self, aircraft.transform.up, aircraft.transform.right);
		float num5 = 0f - TargetCalc.GetAngleOnAxis(waypointDelta, aircraft.rb.velocity + aircraft.transform.forward * 20f, aircraft.transform.right);
		float x = Mathf.Lerp(a, num5 * 4f, num3);
		float a2 = 0f - TargetCalc.GetAngleOnAxis(waypointDelta, aircraft.transform.forward, aircraft.transform.up);
		if (aimDirection != Vector3.zero)
		{
			a2 = 0f - TargetCalc.GetAngleOnAxis(aimDirection, aircraft.transform.forward, aircraft.transform.up);
		}
		float num6 = 0f - TargetCalc.GetAngleOnAxis(waypointDelta, aircraft.rb.velocity, aircraft.transform.up);
		float y = Mathf.Lerp(a2, num6, num3);
		float angleOnAxis = TargetCalc.GetAngleOnAxis(self, aircraft.transform.up, aircraft.transform.forward);
		float b = TargetCalc.GetAngleOnAxis(other: Vector3.up + Mathf.Clamp(num6 * -0.05f, -2f, 2f) * -vector6, self: aircraft.transform.up, axis: -aircraft.transform.forward);
		float z = Mathf.Lerp(angleOnAxis, b, num3);
		hoverController.ApplyInputs(controlInputs, new Vector3(x, y, z), localAngularVelocity);
		float num7 = Mathf.Clamp01(0.5f + magnitude * 0.001f - aircraft.speed * 0.02f);
		float a3 = 0.4f - num4 * ((num4 < 0f) ? 1f : 0.05f) - aircraft.rb.velocity.y * 0.2f;
		float b2 = num7;
		float num8 = Mathf.Lerp(a3, b2, num3 * 2f);
		num8 += 0.5f * terrainAvoidanceUrgency;
		num8 += num * 2f;
		if (aircraft.radarAlt > 7f)
		{
			num8 += Mathf.Min(num2, 0f);
		}
		controlInputs.throttle += Mathf.Clamp(Mathf.Clamp01(num8) - controlInputs.throttle, -1f * Time.deltaTime, 1f * Time.deltaTime);
		if (compoundHelo)
		{
			controlInputs.customAxis1 = num7;
			if (num2 < -5f || Vector3.Dot(aircraft.transform.forward, vector3) < 0f)
			{
				controlInputs.customAxis1 = 0.5f;
			}
			if (aircraft.speed > 80f)
			{
				controlInputs.throttle = Mathf.Min(controlInputs.throttle, 0.5f);
			}
		}
		if (DebugVis.Enabled && SceneSingleton<CameraStateManager>.i.followingUnit == aircraft)
		{
			DebugVis.Create(ref waypointDebug, GameAssets.i.waypointDebug, Datum.origin);
			DebugVis.Create(ref aimVectorDebug, GameAssets.i.debugArrowGreen, aircraft.cockpit.transform);
			DebugVis.Create(ref vectorDebug2, GameAssets.i.debugArrow, Datum.origin);
			waypointDebug.transform.localPosition = waypoint.AsVector3();
			aimVectorDebug.transform.rotation = Quaternion.LookRotation(waypoint - aircraft.GlobalPosition());
			waypointDebug.transform.rotation = aimVectorDebug.transform.rotation;
			aimVectorDebug.transform.localScale = new Vector3(1f, 1f, FastMath.Distance(waypoint, aircraft.GlobalPosition()));
		}
		aircraft.FilterInputs();
	}
}
