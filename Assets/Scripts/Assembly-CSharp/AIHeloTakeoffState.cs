using Unity.Profiling;
using UnityEngine;

public class AIHeloTakeoffState : PilotBaseState
{
	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIHeloTakeoffState.FixedUpdateState");

	private UnitPart mainPart;

	private RotorShaft rotorShaft;

	private float maxRPM;

	private PID collectivePID;

	private float spawnTime;

	private AutopilotHelo autopilotHelo;

	public override void EnterState(Pilot pilot)
	{
		spawnTime = 0f;
		stateDisplayName = "taking off";
		aircraft = pilot.aircraft;
		base.pilot = pilot;
		controlInputs = aircraft.GetInputs();
		mainPart = aircraft.gameObject.GetComponent<UnitPart>();
		mainPart.onApplyDamage += Takeoff_OnTakeDamage;
		collectivePID = new PID(0.5f, 0.02f, 0.1f);
		aircraft.SetFlightAssist(enabled: true);
		aircraft.GetControlsFilter().SetAutoHover(enabled: true);
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.gameObject.GetComponent<IEngine>() is RotorShaft rotorShaft)
			{
				this.rotorShaft = rotorShaft;
				maxRPM = rotorShaft.GetMaxRPM();
				break;
			}
		}
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			float num = 0f;
			if (rotorShaft != null)
			{
				num = rotorShaft.GetRPM() - maxRPM * 0.9f;
			}
			spawnTime += Time.deltaTime;
			controlInputs.brake = 1f;
			if (spawnTime > 30f)
			{
				aircraft.StartEjectionSequence();
				return;
			}
			if (spawnTime < 2f && Mathf.Abs(aircraft.rb.velocity.y) > 1f)
			{
				controlInputs.throttle = 0f;
				return;
			}
			if (num < 0f)
			{
				controlInputs.throttle = 0.05f;
			}
			else
			{
				float num2 = aircraft.rb.velocity.y - 6f;
				float b = 0.5f - num2 * 0.1f;
				controlInputs.throttle = Mathf.Lerp(controlInputs.throttle, b, Time.fixedDeltaTime);
			}
			if (aircraft.radarAlt - 20f > 0f)
			{
				pilot.SwitchState(pilot.AIHeloCombatState);
				return;
			}
			_ = aircraft.rb.velocity.y;
			if (aircraft.radarAlt > 1f)
			{
				aircraft.GetControlsFilter().SetAutoHover(enabled: true);
				aircraft.SetFlightAssist(enabled: false);
				aircraft.autopilot.Hover(aircraft.GlobalPosition(), 50f, aircraft.transform.forward);
			}
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}

	private void Takeoff_OnTakeDamage(UnitPart.OnApplyDamage _)
	{
		if (!pilot.flightInfo.HasTakenOff)
		{
			aircraft.StartEjectionSequence();
			mainPart.onApplyDamage -= Takeoff_OnTakeDamage;
		}
	}

	public override void LeaveState()
	{
		mainPart.onApplyDamage -= Takeoff_OnTakeDamage;
		aircraft.GetControlsFilter().SetAutoHover(enabled: false);
		pilot.flightInfo.HasTakenOff = true;
	}
}
