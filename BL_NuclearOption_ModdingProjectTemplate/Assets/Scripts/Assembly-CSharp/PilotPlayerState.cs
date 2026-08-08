using NuclearOption.UI;
using Rewired;
using Unity.Profiling;
using UnityEngine;

public class PilotPlayerState : PilotBaseState
{
	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("PilotPlayerState.FixedUpdateState");

	private float airspeed;

	private float maxG;

	private bool collective = true;

	private GLOC gloc;

	private float pilotStrength;

	private Player player;

	private float simulatedThrottle;

	private float simulatedCustomAxis1;

	public float pitchInput;

	public float rollInput;

	public float yawInput;

	public override void EnterState(Pilot pilot)
	{
		stateDisplayName = "player controlled";
		player = ReInput.players.GetPlayer(0);
		SceneSingleton<FlightHud>.i.virtualJoystickPos.transform.localPosition = Vector3.zero;
		base.pilot = pilot;
		controlInputs = pilot.aircraft.GetInputs();
		gloc = pilot.gameObject.AddComponent<GLOC>();
		simulatedThrottle = ((pilot.aircraft.radarAlt > pilot.aircraft.definition.spawnOffset.y + 1f) ? 0.4f : (-1f));
		collective = pilot.aircraft.GetAircraftParameters().takeoffDistance == 0f;
	}

	public override void LeaveState()
	{
		gloc.ResetGLOC();
		SceneSingleton<CombatHUD>.i.ClearIcons();
		SceneSingleton<CombatHUD>.i.aircraft = null;
		FlightHud.EnableCanvas(enable: false);
	}

	public override void UpdateState(Pilot pilot)
	{
		if (pilot.aircraft != null)
		{
			PlayerControls();
		}
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			pilotStrength = gloc.SimulateGLOC(pilot.gForce);
			if (!(pilot.aircraft == null))
			{
				PlayerAxisControls();
				pilot.aircraft.FilterInputs();
			}
		}
	}

	private void PlayerAxisControls()
	{
		if (pilot.aircraft.cockpit.IsDetached())
		{
			return;
		}
		float num = ((!PlayerSettings.virtualJoystickInvertPitch) ? 1 : (-1));
		if (PlayerSettings.virtualJoystickEnabled && (DynamicMap.mapMaximized || RadialMenuMain.IsInUse()))
		{
			controlInputs.pitch = Mathf.Clamp(pitchInput, -1f, 1f);
			controlInputs.roll = Mathf.Clamp(rollInput, -1f, 1f);
			controlInputs.yaw = Mathf.Clamp(yawInput, -1f, 1f);
			return;
		}
		if ((double)pilotStrength < 0.2)
		{
			controlInputs.pitch = 0f;
			controlInputs.roll = 0f;
			controlInputs.yaw = 0f;
			return;
		}
		pitchInput = 0f;
		rollInput = 0f;
		yawInput = 0f;
		if (PlayerSettings.virtualJoystickEnabled)
		{
			if (!SceneSingleton<FlightHud>.i.virtualJoystickPos.gameObject.activeSelf)
			{
				SceneSingleton<FlightHud>.i.virtualJoystickPos.gameObject.SetActive(value: true);
			}
			if (!player.GetButton("Free Look"))
			{
				Vector3 vector = SceneSingleton<FlightHud>.i.virtualJoystickPos.transform.localPosition;
				if (CameraStateManager.cameraMode == CameraMode.cockpit)
				{
					vector += PlayerSettings.virtualJoystickSensitivity * Mathf.Min(Time.unscaledDeltaTime, 0.1f) * 30f * new Vector3(GameManager.playerInput.GetAxis("Pan View"), (0f - num) * GameManager.playerInput.GetAxis("Tilt View"), 0f);
					vector = Vector3.ClampMagnitude(vector, 150f);
				}
				vector = Vector3.Lerp(vector, Vector3.zero, PlayerSettings.virtualJoystickCentering * 2f * Time.deltaTime);
				SceneSingleton<FlightHud>.i.SetVirtualJoystick(vector);
			}
			if (!DynamicMap.mapMaximized && !RadialMenuMain.IsInUse() && !LeaderboardMenu.IsOpen())
			{
				pitchInput = (0f - SceneSingleton<FlightHud>.i.virtualJoystickPos.transform.localPosition.y) / 150f;
				rollInput = SceneSingleton<FlightHud>.i.virtualJoystickPos.transform.localPosition.x / 150f;
				if (pilot.aircraft.radarAlt < pilot.aircraft.definition.spawnOffset.y + 1f)
				{
					yawInput = SceneSingleton<FlightHud>.i.virtualJoystickPos.transform.localPosition.x / 150f;
				}
			}
		}
		else if (SceneSingleton<FlightHud>.i.virtualJoystickPos.gameObject.activeSelf)
		{
			SceneSingleton<FlightHud>.i.virtualJoystickPos.gameObject.SetActive(value: false);
		}
		pitchInput += player.GetAxis("Pitch");
		rollInput += player.GetAxis("Roll");
		yawInput += player.GetAxis("Yaw");
		controlInputs.pitch = Mathf.Clamp(pitchInput, -1f, 1f);
		controlInputs.roll = Mathf.Clamp(rollInput, -1f, 1f);
		controlInputs.yaw = Mathf.Clamp(yawInput, -1f, 1f);
		if (pilot.aircraft.IsAutoHoverEnabled())
		{
			PlayerThrottleAxis1Controls();
		}
	}

	private void PlayerControls()
	{
		if (!GameManager.flightControlsEnabled || (double)pilotStrength < 0.2)
		{
			return;
		}
		if (!pilot.aircraft.IsAutoHoverEnabled())
		{
			PlayerThrottleAxis1Controls();
		}
		controlInputs.brake = (player.GetButton("Brake") ? 1 : 0);
		if (player.GetButtonTimedPressUp("Next Weapon", 0f, PlayerSettings.clickDelay))
		{
			pilot.NextWeapon();
		}
		if (player.GetButtonTimedPressUp("Previous Weapon", 0f, PlayerSettings.clickDelay))
		{
			pilot.PreviousWeapon();
		}
		if (player.GetButtonTimedPressUp("Turret Control", 0f, PlayerSettings.clickDelay))
		{
			SceneSingleton<CombatHUD>.i.ToggleAutoControl();
		}
		if (player.GetButtonDown("Gear") && pilot.aircraft.radarAlt > 0.2f)
		{
			if (pilot.aircraft.gearState == LandingGear.GearState.LockedExtended)
			{
				pilot.aircraft.SetGear(deployed: false);
			}
			if (pilot.aircraft.gearState == LandingGear.GearState.LockedRetracted)
			{
				pilot.aircraft.SetGear(deployed: true);
			}
		}
		if (player.GetButtonDown("Eject"))
		{
			pilot.aircraft.StartEjectionSequence();
			if (pilot.aircraft.IsLanded())
			{
				pilot.SwitchState(pilot.parkedState);
			}
		}
		if (player.GetButton("Fire") && (!PlayerSettings.menuWeaponSafety || !Cursor.visible))
		{
			pilot.Fire();
		}
		if (player.GetButton("Countermeasures") && pilot.aircraft.radarAlt > 0.2f)
		{
			if (!pilot.aircraft.countermeasureTrigger)
			{
				pilot.aircraft.Countermeasures(active: true, pilot.aircraft.countermeasureManager.activeIndex);
			}
		}
		else if (pilot.aircraft.countermeasureTrigger)
		{
			pilot.aircraft.Countermeasures(active: false, pilot.aircraft.countermeasureManager.activeIndex);
		}
		if (player.GetButtonTimedPressUp("Next Countermeasure", 0f, PlayerSettings.clickDelay))
		{
			pilot.aircraft.countermeasureManager.NextCountermeasure();
		}
		if (player.GetButtonTimedPressUp("Flight Assist", 0f, PlayerSettings.clickDelay))
		{
			pilot.aircraft.TogglePitchLimiter();
		}
		else if (player.GetButtonTimedPressDown("Flight Assist", PlayerSettings.pressDelay))
		{
			pilot.aircraft.GetControlsFilter().ToggleAutoHover();
		}
		if (player.GetButtonTimedPressUp("Radar", 0f, PlayerSettings.clickDelay))
		{
			if (pilot.aircraft.radar == null)
			{
				return;
			}
			pilot.aircraft.CmdToggleRadar();
		}
		if (player.GetButtonTimedPressUp("Nav Lights", 0f, PlayerSettings.clickDelay))
		{
			pilot.aircraft.ToggleNavLights();
		}
		if (player.GetButtonTimedPressUp("Toggle Engine", 0f, PlayerSettings.clickDelay))
		{
			pilot.aircraft.CmdToggleIgnition();
		}
		if (player.GetButtonTimedPressUp("Link Guns", 0f, PlayerSettings.clickDelay))
		{
			pilot.aircraft.weaponManager.ToggleGunsLinked();
		}
	}

	private void PlayerThrottleAxis1Controls()
	{
		float num = Mathf.Clamp(player.GetAxisRaw("Throttle"), -1f, 1f);
		float num2 = Mathf.Clamp(player.GetAxisRawPrev("Throttle"), -1f, 1f);
		float num3 = Mathf.Clamp(player.GetAxisRaw("Custom Axis 1"), -1f, 1f);
		if (PlayerSettings.throttleUseRelative)
		{
			num = ((Mathf.Abs(num) > 0.1f) ? (Mathf.Sign(num) * 1f) : 0f);
			num2 = ((Mathf.Abs(num2) > 0.1f) ? (Mathf.Sign(num2) * 1f) : 0f);
		}
		if (player.GetButton("Axis Modifier"))
		{
			num3 += num;
			num = 0f;
		}
		float num4 = Mathf.Abs(num - num2);
		if (num4 > 0f && num4 < 0.5f)
		{
			simulatedThrottle = num;
		}
		else if (Mathf.Abs(num) > 0.5f)
		{
			simulatedThrottle += Mathf.Clamp(num - simulatedThrottle, 0f - Time.deltaTime, Time.deltaTime);
		}
		float num5 = simulatedThrottle;
		float num6 = Mathf.Clamp(player.GetAxisRawPrev("Custom Axis 1"), -1f, 1f);
		float num7 = Mathf.Abs(num3 - num6);
		float num8 = controlInputs.customAxis1;
		if (num7 > 0f && num7 < 0.5f)
		{
			num8 = num3;
		}
		else if (Mathf.Abs(num3) > 0.5f)
		{
			num8 += Mathf.Clamp(num3 - num8, 0f - Time.deltaTime, Time.deltaTime);
		}
		if (controlInputs.customAxis1 != num8)
		{
			controlInputs.customAxis1 = Mathf.Clamp01(num8);
		}
		if (PlayerSettings.throttleUseNegative)
		{
			num5 = 0.5f * (num5 + 1f);
		}
		if (collective && PlayerSettings.invertCollective)
		{
			num5 = 1f - num5;
		}
		controlInputs.throttle = Mathf.Clamp01(num5);
	}
}
