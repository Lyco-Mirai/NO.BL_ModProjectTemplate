using Unity.Profiling;
using UnityEngine;

public class AIPilotTakeoffState : PilotBaseState
{
	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIPilotTakeoffState.FixedUpdateState");

	private Airbase airbase;

	private Airbase.Runway.RunwayUsage? runwayUsage;

	private Vector3 runwayVector;

	private AircraftInfo aircraftInfo;

	private AircraftParameters aircraftParameters;

	private float runwayLength;

	private float stuckTimer;

	private float startupTime;

	private GameObject aimPointVis;

	private float pitchAccumulate;

	private UnitPart mainPart;

	private bool startedTakeoffRun;

	private bool takenOff;

	public override void EnterState(Pilot pilot)
	{
		base.pilot = pilot;
		stateDisplayName = "taking off";
		if (pilot.aircraft.NetworkHQ == null)
		{
			return;
		}
		startedTakeoffRun = false;
		takenOff = false;
		aircraft = pilot.aircraft;
		mainPart = aircraft.gameObject.GetComponent<UnitPart>();
		mainPart.onApplyDamage += Takeoff_OnTakeDamage;
		airbase = pilot.aircraft.NetworkHQ.GetNearestAirbase(pilot.transform.position);
		if (airbase == null)
		{
			return;
		}
		runwayUsage = airbase.GetTakeoffRunway(aircraft, 0f);
		if (runwayUsage.HasValue)
		{
			Airbase.Runway runway = runwayUsage.Value.Runway;
			bool reverse = runwayUsage.Value.Reverse;
			runwayLength = runway.Length;
			runway.RegisterStartTakeoff(aircraft);
			runwayVector = runway.GetDirection(reverse);
			if (!reverse)
			{
				runway.Start.transform.position.ToGlobalPosition();
			}
			else
			{
				runway.End.transform.position.ToGlobalPosition();
			}
		}
		controlInputs = pilot.aircraft.GetInputs();
		aircraftInfo = pilot.aircraft.definition.aircraftInfo;
		aircraftParameters = pilot.aircraft.definition.aircraftParameters;
		aircraft.SetFlightAssist(enabled: true);
		if (PlayerSettings.debugVis)
		{
			aimPointVis = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugPoint, pilot.aircraft.transform);
		}
	}

	private void Takeoff_OnTakeDamage(UnitPart.OnApplyDamage _)
	{
		aircraft.StartEjectionSequence();
		mainPart.onApplyDamage -= Takeoff_OnTakeDamage;
	}

	public override void LeaveState()
	{
		controlInputs.customAxis1 = 1f;
		mainPart.onApplyDamage -= Takeoff_OnTakeDamage;
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			float magnitude = (pilot.aircraft.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(aircraft.transform.GlobalPosition())).magnitude;
			stuckTimer = ((aircraft.speed < 1f) ? (stuckTimer + Time.deltaTime) : 0f);
			controlInputs.customAxis1 = ((magnitude > aircraftParameters.takeoffSpeed * 0.7f) ? 0.5f : 1f);
			if (stuckTimer > 12f || airbase == null || airbase.CurrentHQ != aircraft.NetworkHQ)
			{
				pilot.aircraft.StartEjectionSequence();
				pilot.SwitchState(pilot.parkedState);
				return;
			}
			if (aircraft.radarAlt > 1f)
			{
				controlInputs.customAxis1 = 0.5f;
				if (!takenOff)
				{
					takenOff = true;
					runwayUsage?.Runway.RegisterTakeoffLeftRunway(aircraft);
				}
				if (aircraft.radarAlt > 75f)
				{
					if (pilot.AICombatState == null)
					{
						pilot.AICombatState = new AIPilotCombatModes(pilot.aircraft);
					}
					controlInputs.customAxis1 = 1f;
					pilot.SwitchState(pilot.AICombatState);
					return;
				}
			}
			if (!runwayUsage.HasValue)
			{
				return;
			}
			Airbase.Runway runway = runwayUsage.Value.Runway;
			bool reverse = runwayUsage.Value.Reverse;
			Vector3 normalized = runway.GetDirection(reverse).normalized;
			Vector3 vector = runway.GetNearestPoint(aircraft.transform.position, extend: false) + normalized * (50f + aircraft.speed * 3f);
			GlobalPosition globalPosition = vector.ToGlobalPosition();
			if (aimPointVis != null)
			{
				aimPointVis.transform.position = vector;
			}
			bool flag = runway.AircraftOnRunway(aircraft);
			bool flag2 = magnitude > aircraftParameters.takeoffSpeed * 0.7f || !flag;
			if (runway.SkiJump)
			{
				flag2 = !flag;
			}
			if (!flag2)
			{
				Vector3 other = vector - pilot.aircraft.transform.position;
				if (pilot.aircraft.radarAlt > 3f)
				{
					other = runwayVector;
				}
				TargetCalc.GetAngleOnAxis(pilot.aircraft.rb.velocity + pilot.aircraft.transform.forward * 20f, other, pilot.aircraft.transform.up);
				globalPosition = runway.GetNearestPoint(aircraft.transform.position + aircraft.transform.forward * 50f, extend: true).ToGlobalPosition();
				if (Vector3.Dot(aircraft.transform.forward, normalized) < 0f)
				{
					globalPosition += normalized.normalized * 100f;
				}
				if (Vector3.Dot(aircraft.transform.forward, other.normalized) > 0.95f)
				{
					startedTakeoffRun = true;
				}
				if (startedTakeoffRun)
				{
					controlInputs.throttle = 1f;
					controlInputs.brake = 0f;
				}
				else
				{
					controlInputs.throttle = Mathf.Clamp01(1f - aircraft.speed * 0.1f);
					controlInputs.brake = Mathf.Clamp01(aircraft.speed * 0.1f);
				}
			}
			else
			{
				Vector3 vector2 = (reverse ? (runway.End.transform.position + runwayVector * 5f - aircraft.transform.position) : (runway.Start.transform.position + runwayVector * 5f - aircraft.transform.position));
				vector2.y = runwayLength * 0.5f;
				controlInputs.throttle = 1f;
				controlInputs.brake = 0f;
				int num = ((aircraft.radarAlt < 1f) ? 50 : 50);
				globalPosition = (aircraft.transform.position + normalized * 300f + Vector3.up * num).ToGlobalPosition();
				if (runway.SkiJump)
				{
					Vector3 vector3 = aircraft.rb.velocity.normalized * 1000f;
					vector3.y = Mathf.Clamp(vector3.y, 50f, 150f);
					globalPosition = aircraft.GlobalPosition() + vector3;
				}
			}
			aircraft.autopilot.AutoAim(globalPosition, aimVelocity: true, ignoreCollisions: false, aircraft.radarAlt < 3f, 0.95f, 30f, followTerrain: false, 0f, Vector3.zero);
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}
}
