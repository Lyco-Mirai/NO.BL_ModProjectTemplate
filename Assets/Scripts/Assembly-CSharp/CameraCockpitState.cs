using System;
using UnityEngine;

public class CameraCockpitState : CameraBaseState
{
	private float panView;

	private float tiltView;

	private float gForce;

	private float gForcePrev;

	private float jerk;

	private float lowFreqShake;

	private float highFreqShake;

	private Vector3 velocityPrev;

	private Vector3 accelPrev;

	private Aircraft aircraft;

	private Pilot pilot;

	private Rigidbody cockpitRB;

	private float FOVAdjustment;

	private float minFOV = 20f;

	private float maxFOV = 120f;

	private float maxSpeed;

	private float antiSlump;

	private Vector3 lowFreqShakeOffset;

	private Vector3 highFreqShakeOffset;

	private Vector3 accel;

	private Vector3 camRelativePos;

	private Vector3 camRelativeVel;

	private bool padLock;

	private float viewOffset;

	public override void EnterState(CameraStateManager cam)
	{
		aircraft = cam.followingUnit as Aircraft;
		pilot = aircraft.pilots[0];
		cockpitRB = aircraft.cockpit.rb;
		velocityPrev = Vector3.zero;
		SceneSingleton<FlightHud>.i.SetAircraft(aircraft);
		if (aircraft != null)
		{
			Pilot[] pilots = aircraft.pilots;
			for (int i = 0; i < pilots.Length; i++)
			{
				pilots[i].TogglePilotVisibility(enabled: false);
			}
			aircraft.SetCockpitRenderers(enabled: true);
			aircraft.onShake += CockpitCam_OnShake;
			if (GameManager.GetLocalAircraft(out var localAircraft) && aircraft == localAircraft)
			{
				FlightHud.EnableCanvas(enable: true);
			}
			aircraft.SetDoppler(enabled: false);
			maxSpeed = aircraft.definition.aircraftInfo.maxSpeed / 3.6f;
			cam.transform.rotation = aircraft.transform.rotation;
		}
		cam.followingUnit.cockpitViewPoint.transform.rotation = aircraft.cockpit.transform.rotation;
		cam.cameraPivot.transform.SetParent((cam.followingUnit.cockpitViewPoint != null) ? cam.followingUnit.cockpitViewPoint : cam.followingUnit.transform);
		cam.cameraPivot.transform.localPosition = Vector3.zero;
		cam.cameraPivot.transform.localRotation = Quaternion.identity;
		cam.transform.SetParent(cam.cameraPivot.transform);
		cam.transform.localPosition = Vector3.zero;
		cam.transform.localRotation = Quaternion.identity;
		panView = 0f;
		tiltView = 0f;
		cam.mainCamera.nearClipPlane = 0.2f;
		cam.cockpitCamRender.enabled = true;
		CameraStateManager.cameraMode = CameraMode.cockpit;
		FOVAdjustment = 0f;
		cam.SetDesiredFoV(PlayerSettings.defaultFoV, PlayerSettings.defaultFoV);
		cam.cockpitCamRender.fieldOfView = cam.mainCamera.fieldOfView;
		gForcePrev = 0f;
		if (aircraft != null)
		{
			velocityPrev = Vector3.zero;
		}
		lowFreqShake = 0f;
	}

	public override void LeaveState(CameraStateManager cam)
	{
		cam.SetDesiredFoV(PlayerSettings.defaultExternalFoV, PlayerSettings.defaultExternalFoV);
		if (aircraft != null)
		{
			Pilot[] pilots = aircraft.pilots;
			for (int i = 0; i < pilots.Length; i++)
			{
				pilots[i].TogglePilotVisibility(enabled: true);
			}
			aircraft.onShake -= CockpitCam_OnShake;
			aircraft.SetDoppler(enabled: true);
			aircraft.SetCockpitRenderers(enabled: false);
		}
		AoAFeedback.RunAoAFeedback(null);
		cam.cockpitCamRender.enabled = false;
	}

	public override void UpdateState(CameraStateManager cam)
	{
		if (pilot.dead)
		{
			cam.SwitchState(cam.freeState);
		}
		else
		{
			if (!GameManager.flightControlsEnabled)
			{
				return;
			}
			if (GameManager.playerInput.GetButtonTimedPressUp("Switch View", 0f, PlayerSettings.clickDelay))
			{
				if (aircraft != null)
				{
					aircraft.onShake -= CockpitCam_OnShake;
				}
				cam.SwitchState(cam.orbitState);
			}
			if (!DynamicMap.mapMaximized)
			{
				FOVAdjustment -= 5f * GameManager.playerInput.GetAxis("Zoom View");
			}
			FOVAdjustment = Mathf.Clamp(FOVAdjustment, minFOV - cam.desiredFOV, maxFOV - cam.desiredFOV);
			float b = Mathf.Clamp(cam.desiredFOV + FOVAdjustment, minFOV, maxFOV);
			cam.mainCamera.fieldOfView = Mathf.Lerp(cam.mainCamera.fieldOfView, b, 0.2f);
			cam.cockpitCamRender.fieldOfView = cam.mainCamera.fieldOfView;
			float axis = GameManager.playerInput.GetAxis("Move Vertical");
			if (axis != 0f)
			{
				viewOffset = Mathf.Clamp(viewOffset + 0.2f * axis * Time.unscaledDeltaTime, 0f, 0.15f);
			}
			if (PlayerSettings.virtualJoystickEnabled)
			{
				if (GameManager.playerInput.GetButton("Free Look"))
				{
					panView += GameManager.playerInput.GetAxis("Pan View") * 120f * PlayerSettings.viewSensitivity * Time.unscaledDeltaTime;
					tiltView += GameManager.playerInput.GetAxis("Tilt View") * 120f * PlayerSettings.viewSensitivity * Time.unscaledDeltaTime * (float)((!PlayerSettings.viewInvertPitch) ? 1 : (-1));
					CursorManager.Refresh();
				}
				else
				{
					panView = 0f;
					tiltView = 0f;
				}
			}
			else if (!Cursor.visible && !RadialMenuMain.IsInUse())
			{
				panView += GameManager.playerInput.GetAxis("Pan View") * 120f * PlayerSettings.viewSensitivity * Time.unscaledDeltaTime;
				tiltView += GameManager.playerInput.GetAxis("Tilt View") * 120f * PlayerSettings.viewSensitivity * Time.unscaledDeltaTime * (float)((!PlayerSettings.viewInvertPitch) ? 1 : (-1));
			}
			if (GameManager.playerInput.GetButtonDown("Center"))
			{
				if (PlayerSettings.padLockTarget && SceneSingleton<CombatHUD>.i.aircraft != null && SceneSingleton<CombatHUD>.i.GetTargetList().Count > 0)
				{
					padLock = !padLock;
				}
				if (!padLock || SceneSingleton<CombatHUD>.i.GetTargetList().Count == 0)
				{
					padLock = false;
					panView = 0f;
					tiltView = 0f;
				}
			}
			if (PlayerSettings.padLockTarget && padLock && SceneSingleton<CombatHUD>.i.aircraft != null && SceneSingleton<CombatHUD>.i.GetTargetList().Count > 0)
			{
				Unit unit = SceneSingleton<CombatHUD>.i.GetTargetList()[0];
				if (unit != null && SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ.TryGetKnownPosition(unit, out var knownPosition))
				{
					Vector3 normalized = (knownPosition.ToLocalPosition() - aircraft.transform.position).normalized;
					Vector3 vector = Vector3.Dot(normalized, aircraft.transform.forward) * aircraft.transform.forward + Vector3.Dot(normalized, aircraft.transform.right) * aircraft.transform.right;
					Vector3 vector2 = Vector3.Cross(vector.normalized, aircraft.transform.up);
					panView = Vector3.SignedAngle(vector.normalized, aircraft.transform.forward, -aircraft.transform.up);
					tiltView = Vector3.SignedAngle(normalized, aircraft.transform.up, vector2.normalized) - 90f;
				}
				panView = Mathf.Clamp(panView, -165f, 165f);
				tiltView = Mathf.Clamp(tiltView, -65f, 45f);
			}
			panView = Mathf.Clamp(panView, -165f, 165f);
			tiltView = Mathf.Clamp(tiltView, -65f, 65f);
			float a = ((cam.transform.localEulerAngles.y <= 180f) ? cam.transform.localEulerAngles.y : (cam.transform.localEulerAngles.y - 360f));
			float x = Mathf.Lerp((cam.transform.localEulerAngles.x <= 180f) ? cam.transform.localEulerAngles.x : (cam.transform.localEulerAngles.x - 360f), tiltView, Mathf.Min(2f * Time.unscaledDeltaTime / Mathf.Max(PlayerSettings.viewSmoothing, 0.01f), 1f));
			float num = Mathf.Lerp(a, panView, Mathf.Min(2f * Time.unscaledDeltaTime / Mathf.Max(PlayerSettings.viewSmoothing, 0.01f), 1f));
			Quaternion quaternion = Quaternion.Euler(x, num, 0f);
			if (aircraft != null && aircraft.CockpitRB() != null)
			{
				cam.cameraVelocity = aircraft.CockpitRB().velocity;
				camRelativePos += camRelativeVel * Mathf.Min(Time.deltaTime, 0.01666667f);
				if (camRelativePos.magnitude > 0.15f)
				{
					camRelativeVel = Vector3.zero;
					camRelativePos = Vector3.ClampMagnitude(camRelativePos, 0.15f);
				}
			}
			cam.cameraPivot.position = cam.followingUnit.cockpitViewPoint.position + camRelativePos * PlayerSettings.cockpitCamInertia + CameraShake();
			float num2 = Mathf.Lerp(0f, 0.2f, Mathf.Abs(num * 0.015f) - 0.5f) * Mathf.Sign(num);
			if (PlayerSettings.useTrackIR && PlayerSettings.headTrackerType != HeadTrackerType.None)
			{
				if (GameManager.playerInput.GetButtonDown("Center"))
				{
					cam.transform.localPosition = Vector3.zero;
					cam.transform.localRotation = Quaternion.LookRotation(Vector3.forward);
					HeadTrackerManager.Recenter();
				}
				else
				{
					Tuple<Vector3, Quaternion> offset = HeadTrackerManager.GetOffset(Vector3.right * num2, quaternion);
					cam.transform.localPosition = new Vector3(Mathf.Clamp(offset.Item1.x, -0.25f, 0.25f), Mathf.Clamp(offset.Item1.y, -0.15f, 0.15f), Mathf.Clamp(offset.Item1.z, -0.1f, 0.45f));
					cam.transform.localRotation = offset.Item2;
				}
			}
			else
			{
				cam.transform.localPosition = viewOffset * (Vector3.up + 0.5f * Vector3.forward) + Vector3.right * num2;
				cam.transform.localRotation = quaternion;
			}
		}
	}

	private Vector3 CameraShake()
	{
		lowFreqShake = Mathf.Min(lowFreqShake, 1f);
		highFreqShake = Mathf.Min(highFreqShake, 1f);
		if (lowFreqShake < 0.01f && highFreqShake < 0.05f)
		{
			return Vector3.zero;
		}
		float num = 8f;
		float num2 = 16f;
		lowFreqShakeOffset = 0.03f * new Vector3(Mathf.PerlinNoise1D(Time.timeSinceLevelLoad * num * 2f) - 0.5f, Mathf.PerlinNoise1D(Time.timeSinceLevelLoad * num * 1.6666667f) - 0.5f, Mathf.PerlinNoise1D(Time.timeSinceLevelLoad * num * 1.2107f) - 0.5f);
		highFreqShakeOffset = 0.01f * new Vector3(Mathf.PerlinNoise1D(Time.timeSinceLevelLoad * num2 * 2f) - 0.5f, Mathf.PerlinNoise1D(Time.timeSinceLevelLoad * num2 * 1.6666667f) - 0.5f, Mathf.PerlinNoise1D(Time.timeSinceLevelLoad * num2 * 1.2107f) - 0.5f);
		lowFreqShakeOffset *= Mathf.Max(lowFreqShake - 0.01f, 0f);
		highFreqShakeOffset *= Mathf.Max(highFreqShake - 0.05f, 0f);
		return lowFreqShakeOffset + highFreqShakeOffset;
	}

	public void AddShake(float lowFreqShake, float highFreqShake)
	{
		this.lowFreqShake += lowFreqShake;
		this.highFreqShake += highFreqShake;
	}

	private void CockpitCam_OnShake(Aircraft.OnShake e)
	{
		lowFreqShake += e.lowFreqShake;
		highFreqShake += e.highFreqShake;
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
		gForce = 0f;
		jerk = 0f;
		if (!(aircraft != null) || !(aircraft.CockpitRB() != null) || !(Time.deltaTime > 0f))
		{
			return;
		}
		Vector3 pointVelocity = aircraft.CockpitRB().GetPointVelocity(cam.transform.position);
		accel = ((velocityPrev == Vector3.zero) ? Vector3.zero : ((pointVelocity - velocityPrev) / Time.deltaTime));
		Vector3 vector = -500f * camRelativePos.magnitude * camRelativePos.normalized;
		float num = Vector3.Dot(cam.transform.up, -camRelativePos);
		antiSlump += num * 1000f * Time.deltaTime;
		vector += cam.transform.up * antiSlump;
		camRelativeVel += (-Vector3.ClampMagnitude(accel, 500f) + vector) * Time.deltaTime;
		camRelativeVel -= Vector3.ClampMagnitude(camRelativeVel * 20f * Time.deltaTime, camRelativeVel.magnitude);
		gForce = accel.magnitude / 9.81f;
		jerk = ((gForcePrev == 0f) ? 0f : ((gForce - gForcePrev) / Time.deltaTime));
		velocityPrev = pointVelocity;
		gForcePrev = gForce;
		accelPrev = accel;
		lowFreqShake = Mathf.Clamp(jerk * 0.005f, lowFreqShake, 1f);
		lowFreqShake = Mathf.Lerp(lowFreqShake, 0f, 5f * Time.fixedDeltaTime);
		highFreqShake = Mathf.Lerp(highFreqShake, 0f, 4f * Time.fixedDeltaTime);
		cam.cockpitRattle.volume = lowFreqShake;
		if (!cam.cockpitRattle.isPlaying)
		{
			if (lowFreqShake > 0f)
			{
				cam.cockpitRattle.Play();
			}
		}
		else if (lowFreqShake == 0f)
		{
			cam.cockpitRattle.Stop();
		}
		AoAFeedback.RunAoAFeedback(aircraft);
	}
}
