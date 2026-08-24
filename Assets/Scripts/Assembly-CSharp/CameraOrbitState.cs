using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraOrbitState : CameraBaseState
{
	private RaycastHit hit;

	private float panView;

	private float tiltView;

	private Vector3 followVector;

	private Vector3 flatVelSmoothed;

	private Rigidbody follow1;

	private Rigidbody follow2;

	private float viewDistAdjust;

	private int layerMask = PhysicsLayers.StaticsMask;

	private float zoomSpeed;

	private Quaternion pivotRotationPrev;

	private Quaternion cameraRotationPrev;

	private float FOVAdjustment;

	private float minFOV = 20f;

	private float maxFOV = 120f;

	private float followingMaxRadius;

	private float followLerp;

	private float lookAtTargetLerp;

	private float lastCloseEnemyAircraftCheck;

	private float targetSwitchLerp;

	private Unit targetUnit;

	public override void EnterState(CameraStateManager cam)
	{
		viewDistAdjust = 0f;
		zoomSpeed = 0f;
		followLerp = 0f;
		panView = 0f;
		tiltView = 20f;
		targetUnit = null;
		targetSwitchLerp = 0f;
		lookAtTargetLerp = 0f;
		follow1 = cam.followingRB;
		if (cam.followingUnit != null)
		{
			follow1 = cam.followingUnit.rb;
			if (cam.followingUnit is Aircraft aircraft)
			{
				if (aircraft.cockpit.IsDetached())
				{
					cam.followingRB = aircraft.CockpitRB();
					follow1 = cam.followingRB;
					followVector = follow1.transform.forward;
					followVector.y = 0f;
					cam.cameraPivot.SetParent(aircraft.cockpit.transform);
					cam.cameraPivot.localPosition = Vector3.zero;
				}
				else
				{
					aircraft.cockpit.onParentDetached += CameraOrbitState_OnCockpitDetach;
				}
			}
		}
		cam.followingUnit.SetDoppler(enabled: false);
		cam.followingRB = follow1;
		cam.cameraPivot.SetParent(follow1.transform);
		cam.cameraPivot.localPosition = Vector3.zero;
		cam.transform.SetParent(cam.cameraPivot);
		FOVAdjustment = 0f;
		cam.mainCamera.nearClipPlane = 1f;
		flatVelSmoothed = follow1.velocity * 10f + follow1.transform.forward;
		flatVelSmoothed.y = 0f;
		flatVelSmoothed = Vector3.ProjectOnPlane(follow1.velocity * 10f + follow1.transform.forward, Vector3.up);
		if (flatVelSmoothed.sqrMagnitude < 0.001f)
		{
			flatVelSmoothed = Vector3.forward;
		}
		followVector = flatVelSmoothed;
		cam.cameraPivot.rotation = Quaternion.LookRotation(followVector, Vector3.up);
		pivotRotationPrev = cam.cameraPivot.rotation;
		FlightHud.EnableCanvas(enable: false);
		CameraStateManager.cameraMode = CameraMode.orbit;
		((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).shadowDistance = Mathf.Max(2000f, 2000f * cam.followingUnit.maxRadius * 2f / 30f);
	}

	public override void LeaveState(CameraStateManager cam)
	{
		cam.cameraPivot.SetParent(null);
		((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).shadowDistance = 2000f;
		follow2 = null;
		cam.cameraVelocity = Vector3.zero;
		if (cam.followingUnit != null)
		{
			cam.followingUnit.SetDoppler(enabled: true);
			if (cam.followingUnit is Aircraft aircraft)
			{
				aircraft.cockpit.onParentDetached -= CameraOrbitState_OnCockpitDetach;
			}
		}
	}

	private void CameraOrbitState_OnCockpitDetach(UnitPart cockpitPart)
	{
		follow2 = cockpitPart.rb;
		SceneSingleton<CameraStateManager>.i.cameraPivot.transform.SetParent(null);
	}

	public override void UpdateState(CameraStateManager cam)
	{
		CameraMotion(cam);
		if (GameManager.flightControlsEnabled)
		{
			Inputs(cam);
		}
	}

	private static bool AnyMoveInput()
	{
		if (!(Mathf.Abs(GameManager.playerInput.GetAxis("Move Longitudinal")) > 0.1f))
		{
			return Mathf.Abs(GameManager.playerInput.GetAxis("Move Lateral")) > 0.1f;
		}
		return true;
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
		if (cam.followingRB != null)
		{
			cam.cameraVelocity = cam.followingRB.velocity * Time.timeScale;
		}
		else
		{
			cam.cameraVelocity = Vector3.zero;
		}
	}

	private void CameraMotion(CameraStateManager cam)
	{
		if (follow2 != null)
		{
			followLerp += Time.deltaTime;
			cam.cameraPivot.SetParent(null);
			cam.cameraPivot.position = Vector3.Lerp(follow1.transform.position, follow2.transform.position, followLerp);
			if (followLerp >= 1f)
			{
				cam.cameraPivot.SetParent(follow2.transform);
				followLerp = 0f;
				follow1 = follow2;
				follow2 = null;
				cam.followingRB = follow1;
			}
		}
		else
		{
			cam.cameraPivot.transform.localPosition = Vector3.zero;
		}
		float num = Mathf.Min(Time.unscaledDeltaTime / Mathf.Max(PlayerSettings.viewSmoothing, 0.01f));
		Vector3 b = cam.followingRB.velocity + cam.followingRB.transform.forward * 10f;
		b.y = 0f;
		if (b.sqrMagnitude > 0.001f)
		{
			flatVelSmoothed = Vector3.Lerp(flatVelSmoothed, b, 10f * Time.unscaledDeltaTime);
		}
		followVector = flatVelSmoothed;
		if (cam.followingUnit != null)
		{
			followingMaxRadius = cam.followingUnit.maxRadius;
		}
		float num2 = 1f + followingMaxRadius * (1f + viewDistAdjust);
		cam.cameraPivot.rotation = Quaternion.LookRotation(followVector, Vector3.up);
		cam.cameraPivot.Rotate(0f, panView, 0f, Space.World);
		cam.cameraPivot.Rotate(tiltView, 0f, 0f, Space.Self);
		cam.cameraPivot.rotation = Quaternion.Lerp(pivotRotationPrev, cam.cameraPivot.rotation, num * 5f);
		pivotRotationPrev = cam.cameraPivot.rotation;
		cam.transform.position = cam.cameraPivot.position - num2 * 2f * cam.cameraPivot.forward;
		Vector3 vector = cam.cameraPivot.position - cam.transform.position;
		if (Physics.Linecast(cam.cameraPivot.position, cam.cameraPivot.position - vector, out hit, layerMask))
		{
			float num3 = Mathf.Max(Vector3.Dot(vector.normalized, hit.normal), 0.1f);
			Vector3 vector2 = vector.normalized / num3;
			cam.transform.position = hit.point + vector2;
		}
		Unit unit = targetUnit;
		Vector3 vector3 = follow1.transform.position;
		targetUnit = ((LookingAtTarget(cam, out var target) || LookingAtEnemyAircraft(cam, out target)) ? target : null);
		if (targetUnit != unit)
		{
			targetSwitchLerp = 0f;
			cameraRotationPrev = cam.transform.rotation;
		}
		if (follow2 != null)
		{
			vector3 = Vector3.Lerp(follow1.transform.position, follow2.transform.position, followLerp);
		}
		bool flag = false;
		flag = ((!GameManager.GetLocalHQ(out var localHq)) ? (targetUnit != null) : (targetUnit != null && localHq.IsTargetPositionAccurate(targetUnit, 100f)));
		Quaternion b2 = cam.transform.rotation;
		if (flag)
		{
			lookAtTargetLerp = Mathf.Clamp01(lookAtTargetLerp + 2f * Time.unscaledDeltaTime);
			b2 = Quaternion.LookRotation(targetUnit.transform.position - cam.transform.position, Vector3.up);
			if (targetSwitchLerp < 1f)
			{
				targetSwitchLerp = Mathf.Clamp01(targetSwitchLerp + 2f * Time.unscaledDeltaTime);
				float t = Mathf.SmoothStep(0f, 1f, targetSwitchLerp);
				b2 = Quaternion.Lerp(cameraRotationPrev, b2, t);
			}
		}
		else
		{
			cameraRotationPrev = cam.transform.rotation;
			lookAtTargetLerp = Mathf.Clamp01(lookAtTargetLerp - 2f * Time.unscaledDeltaTime);
		}
		Quaternion quaternion = Quaternion.LookRotation(vector3 - cam.transform.position, Vector3.up);
		if (lookAtTargetLerp > 0f)
		{
			float t2 = Mathf.SmoothStep(0f, 1f, lookAtTargetLerp);
			float num4 = 1f + followingMaxRadius * (1f + viewDistAdjust);
			b2 = Quaternion.Lerp(quaternion, b2, t2);
			Vector3 vector4 = b2 * Vector3.forward;
			Vector3 b3 = follow1.transform.position - num4 * 2f * vector4 + num4 * 0.4f * Vector3.up;
			cam.transform.SetPositionAndRotation(Vector3.Lerp(cam.transform.position, b3, t2), b2);
		}
		else
		{
			cam.transform.rotation = quaternion;
		}
		cameraRotationPrev = cam.transform.rotation;
		cam.cameraVelocity = follow1.velocity;
	}

	private void Inputs(CameraStateManager cam)
	{
		if (!Cursor.visible)
		{
			float axis = GameManager.playerInput.GetAxis("Pan View");
			float axis2 = GameManager.playerInput.GetAxis("Tilt View");
			panView += Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * axis * 90f * PlayerSettings.viewSensitivity * Time.unscaledDeltaTime;
			tiltView += Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * axis2 * 90f * PlayerSettings.viewSensitivity * Time.unscaledDeltaTime * (float)((!PlayerSettings.viewInvertPitch) ? 1 : (-1));
			panView = Mathf.DeltaAngle(0f, panView);
			tiltView = Mathf.Clamp(tiltView, -89f, 89f);
			zoomSpeed -= GameManager.playerInput.GetAxis("Zoom View") * 60f * Time.unscaledDeltaTime;
		}
		FOVAdjustment -= cam.fovChangeSpeed * Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("FOV");
		FOVAdjustment = Mathf.Clamp(FOVAdjustment, minFOV - cam.desiredFOV, maxFOV - cam.desiredFOV);
		float b = Mathf.Clamp(cam.desiredFOV + FOVAdjustment, minFOV, maxFOV);
		cam.mainCamera.fieldOfView = Mathf.Lerp(cam.mainCamera.fieldOfView, b, 1f / (1f + 100f * cam.fovChangeInertia));
		zoomSpeed = Mathf.Lerp(zoomSpeed, 0f, 4f * Time.unscaledDeltaTime);
		zoomSpeed = Mathf.Clamp(zoomSpeed, -10f, 10f);
		viewDistAdjust += zoomSpeed * Time.unscaledDeltaTime;
		viewDistAdjust = Mathf.Clamp(viewDistAdjust, 0f, 10f);
		bool flag = GameManager.IsLocalAircraft(cam.followingUnit);
		if (GameManager.playerInput.GetButtonDown("Center"))
		{
			panView = 0f;
			tiltView = 0f;
			if (GameManager.gameState != GameState.Editor)
			{
				cam.SwitchState(cam.chaseState);
			}
		}
		if (!flag && GameManager.playerInput.GetButtonDown("Change Spectate Faction") && TryGetNearestEnemyAircraft(out targetUnit))
		{
			SceneSingleton<CameraStateManager>.i.SetFollowingUnit(targetUnit);
			return;
		}
		if (GameManager.gameState != GameState.Editor && GameManager.playerInput.GetButtonTimedPressUp("Switch View", 0f, PlayerSettings.clickDelay))
		{
			cam.SwitchState(cam.TVState);
		}
		if (Input.GetMouseButton(1))
		{
			CursorManager.Refresh();
		}
		if ((cam.followingUnit == null || cam.followingUnit.disabled || !flag) && AnyMoveInput())
		{
			cam.SetFollowingUnit(null);
			cam.SwitchState(cam.freeState);
		}
	}

	private bool TryGetNearestEnemyAircraft(out Unit nearestEnemyAircraft)
	{
		nearestEnemyAircraft = null;
		if (SceneSingleton<CameraStateManager>.i.followingUnit == null)
		{
			return false;
		}
		if (Time.timeSinceLevelLoad - lastCloseEnemyAircraftCheck < 1f)
		{
			nearestEnemyAircraft = targetUnit;
			return targetUnit != null;
		}
		lastCloseEnemyAircraftCheck = Time.timeSinceLevelLoad;
		FactionHQ networkHQ = SceneSingleton<CameraStateManager>.i.followingUnit.NetworkHQ;
		GlobalPosition b = SceneSingleton<CameraStateManager>.i.followingUnit.GlobalPosition();
		float range = float.MaxValue;
		foreach (Unit allUnit in UnitRegistry.allUnits)
		{
			if (allUnit.NetworkHQ != null && allUnit.NetworkHQ != networkHQ && allUnit is Aircraft aircraft && FastMath.InRange(aircraft.GlobalPosition(), b, range) && (!GameManager.GetLocalHQ(out var localHq) || localHq.IsTargetBeingTracked(aircraft)))
			{
				range = FastMath.Distance(aircraft.GlobalPosition(), b);
				nearestEnemyAircraft = allUnit;
				targetUnit = allUnit;
			}
		}
		return nearestEnemyAircraft != null;
	}

	public bool LookingAtEnemyAircraft(CameraStateManager cam, out Unit target)
	{
		target = null;
		if (!GameManager.playerInput.GetButton("Spectate Next Aircraft"))
		{
			return false;
		}
		if (TryGetNearestEnemyAircraft(out var nearestEnemyAircraft))
		{
			target = nearestEnemyAircraft;
		}
		return target != null;
	}

	public bool LookingAtTarget(CameraStateManager cam, out Unit target)
	{
		target = null;
		if (!GameManager.playerInput.GetButton("Cycle Look At"))
		{
			return false;
		}
		if (cam.followingUnit == null || cam.followingUnit.NetworkHQ == null)
		{
			return false;
		}
		if (cam.followingUnit is Missile missile)
		{
			UnitRegistry.TryGetUnit(missile.targetID, out target);
		}
		else if (cam.followingUnit is Aircraft aircraft)
		{
			List<Unit> targetList = aircraft.weaponManager.GetTargetList();
			if (targetList.Count > 0)
			{
				target = targetList[0];
			}
		}
		else
		{
			Turret componentInChildren = cam.followingUnit.gameObject.GetComponentInChildren<Turret>();
			if (componentInChildren != null)
			{
				target = componentInChildren.GetTarget();
			}
		}
		return target != null;
	}

	public bool TryGetLookAtUnit(out Unit lookAtUnit)
	{
		lookAtUnit = targetUnit;
		return lookAtUnit != null;
	}
}
