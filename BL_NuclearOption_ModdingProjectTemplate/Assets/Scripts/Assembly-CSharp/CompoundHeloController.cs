using UnityEngine;

public class CompoundHeloController : MonoBehaviour
{
	private enum ThrustMode
	{
		forward = 0,
		reverse = 1,
		neutral = 2,
		failsafe = 3
	}

	private ThrustMode thrustMode = ThrustMode.neutral;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private float failsafeThreshold = 50f;

	[SerializeField]
	private UnitPart[] failsafeSources;

	private float inputTarget;

	private ControlInputs inputs;

	private void Awake()
	{
		base.enabled = aircraft.flightAssist;
		aircraft.onSetFlightAssist += CompoundHeloController_OnFlightAssist;
		inputs = aircraft.GetInputs();
		UpdateThrustMode(ThrustMode.neutral);
		UnitPart[] array = failsafeSources;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].onApplyDamage += CompoundHeloController_OnDamage;
		}
	}

	private void CompoundHeloController_OnFlightAssist(Aircraft.OnFlightAssistToggle e)
	{
		base.enabled = e.enabled;
	}

	private void CompoundHeloController_OnDamage(UnitPart.OnApplyDamage e)
	{
		if (e.hitPoints < failsafeThreshold)
		{
			UpdateThrustMode(ThrustMode.failsafe);
			UnitPart[] array = failsafeSources;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].onApplyDamage -= CompoundHeloController_OnDamage;
			}
		}
	}

	private void UpdateThrustMode(ThrustMode newThrustMode)
	{
		thrustMode = newThrustMode;
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			return;
		}
		switch (thrustMode)
		{
		case ThrustMode.neutral:
			inputTarget = 0.5f;
			if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Thrust mode set to Neutral", 3f);
			}
			break;
		case ThrustMode.forward:
			inputTarget = 1f;
			if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Thrust mode set to Forward", 3f);
			}
			break;
		case ThrustMode.reverse:
			inputTarget = 0f;
			if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Thrust mode set to Reverse", 3f);
			}
			break;
		case ThrustMode.failsafe:
			inputTarget = 0.5f;
			if (aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Thrust mode set to Fail-safe", 3f);
			}
			break;
		}
	}

	private void FixedUpdate()
	{
		ThrustMode thrustMode = this.thrustMode;
		if (aircraft.speed < 35f)
		{
			thrustMode = ThrustMode.neutral;
		}
		else
		{
			float num = Vector3.Dot(base.transform.forward, Vector3.up);
			if (num > 0f && inputs.throttle < 0.25f)
			{
				thrustMode = ThrustMode.reverse;
			}
			if (num < 0f && inputs.throttle > 0.25f)
			{
				thrustMode = ThrustMode.forward;
			}
		}
		if (this.thrustMode == ThrustMode.failsafe)
		{
			thrustMode = ThrustMode.failsafe;
		}
		if (thrustMode != this.thrustMode)
		{
			UpdateThrustMode(thrustMode);
		}
		inputs.customAxis1 += Mathf.Clamp(inputTarget - inputs.customAxis1, -0.3f * Time.fixedDeltaTime, 0.3f * Time.fixedDeltaTime);
	}
}
