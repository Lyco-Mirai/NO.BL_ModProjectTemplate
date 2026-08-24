using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraChaseState : CameraBaseState
{
	private enum ChasePos
	{
		Back = 0,
		Front = 1,
		Top = 2,
		Bottom = 3,
		Belly = 4,
		Tail = 5,
		WingL = 6,
		WingR = 7,
		WingRootL = 8,
		WingRootR = 9,
		Custom = 10
	}

	private struct CameraPos
	{
		public struct POV
		{
			public Vector3 position;

			public Vector3 orientation;
		}

		public POV back;

		public POV front;

		public POV top;

		public POV bottom;

		public POV belly;

		public POV tail;

		public POV wingL;

		public POV wingR;

		public POV wingRootL;

		public POV wingRootR;
	}

	private RaycastHit hit;

	private float orbitDist;

	private Vector3 posVector;

	private Vector3 targetVector;

	private Quaternion cameraRotationPrev;

	private Quaternion cameraCustomRotation;

	private float viewDistAdjust;

	private int layerMask = PhysicsLayers.StaticsMask;

	private float zoomSpeed;

	private float FOVAdjustment;

	private float minFOV = 20f;

	private float maxFOV = 120f;

	private Transform transitionTarget;

	private float transitionTimer;

	private ChasePos currentPos;

	private bool showHUD;

	private Dictionary<AircraftDefinition, CameraPos> AircraftCameraPos = new Dictionary<AircraftDefinition, CameraPos>();

	public void Initialize()
	{
		foreach (AircraftDefinition item in Encyclopedia.i.aircraft)
		{
			CameraPos value = default(CameraPos);
			float num = Mathf.Max(item.length, item.width * 0.7f) + Mathf.Max(item.width, item.length * 0.7f);
			value.back.position = new Vector3(0f, 0.1f * num, 0f - num);
			value.back.orientation = Vector3.forward;
			value.front.position = new Vector3(0f, 0f, num);
			value.front.orientation = -Vector3.forward;
			value.top.position = new Vector3(0f, num, 0f);
			value.top.orientation = -Vector3.up + 0.1f * Vector3.forward;
			value.bottom.position = new Vector3(0f, 0f - num, 0f);
			value.bottom.orientation = Vector3.up + 0.1f * Vector3.forward;
			value.belly.position = new Vector3(0f, -0.1f * num, -0.25f * num);
			value.belly.orientation = Vector3.forward;
			value.tail.position = new Vector3(0f, 0.1f * num, -0.25f * num);
			value.tail.orientation = Vector3.forward;
			value.wingL.position = new Vector3(0f - num, 0f, 0f);
			value.wingL.orientation = Vector3.right;
			value.wingR.position = new Vector3(num, 0f, 0f);
			value.wingR.orientation = -Vector3.right;
			value.wingRootL.position = new Vector3(-0.1f * num, 0.05f * num, 0f);
			value.wingRootL.orientation = Vector3.forward;
			value.wingRootR.position = new Vector3(0.1f * num, 0.05f * num, 0f);
			value.wingRootR.orientation = Vector3.forward;
			AircraftCameraPos.Add(item, value);
		}
	}

	public override void EnterState(CameraStateManager cam)
	{
		if (!(cam.followingUnit is Aircraft))
		{
			cam.SwitchState(cam.orbitState);
			return;
		}
		viewDistAdjust = 0f;
		zoomSpeed = 0f;
		transitionTimer = 0f;
		cam.cameraPivot.SetParent(cam.followingUnit.transform);
		cam.transform.SetParent(cam.cameraPivot);
		cam.cameraPivot.localPosition = Vector3.zero;
		cam.cameraPivot.localRotation = Quaternion.identity;
		cam.transform.localEulerAngles = Vector3.zero;
		FOVAdjustment = 0f;
		orbitDist = Mathf.Max(cam.followingUnit.definition.length, cam.followingUnit.definition.width * 0.7f) + Mathf.Max(cam.followingUnit.definition.width, cam.followingUnit.definition.length * 0.7f);
		if (cam.followingUnit is Aircraft aircraft)
		{
			if (aircraft.cockpit.IsDetached())
			{
				cam.followingRB = aircraft.cockpit.rb;
				cam.cameraPivot.SetParent(aircraft.cockpit.transform);
				cam.cameraPivot.localPosition = Vector3.zero;
			}
			else
			{
				aircraft.cockpit.onParentDetached += CameraChaseState_OnCockpitDetach;
			}
		}
		cam.followingUnit.SetDoppler(enabled: false);
		cam.mainCamera.nearClipPlane = 0.2f;
		currentPos = ChasePos.Back;
		if (AircraftCameraPos.TryGetValue(cam.followingUnit.definition as AircraftDefinition, out var value))
		{
			posVector = value.back.position;
			targetVector = value.back.orientation;
		}
		else
		{
			posVector = -cam.followingUnit.transform.forward * orbitDist;
			targetVector = cam.followingUnit.transform.forward;
		}
		cameraRotationPrev = cam.cameraPivot.rotation;
		cameraCustomRotation = cam.transform.localRotation;
		showHUD = false;
		CheckHUD();
		CameraStateManager.cameraMode = CameraMode.chase;
		((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).shadowDistance = Mathf.Max(2000f, 2000f * orbitDist * 2f / 30f);
	}

	public override void LeaveState(CameraStateManager cam)
	{
		cam.cameraPivot.SetParent(null);
		((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).shadowDistance = 2000f;
		if (cam.followingUnit is Aircraft aircraft)
		{
			aircraft.cockpit.onParentDetached -= CameraChaseState_OnCockpitDetach;
		}
		cam.followingUnit.SetDoppler(enabled: true);
	}

	private void CameraChaseState_OnCockpitDetach(UnitPart cockpitPart)
	{
		transitionTimer = 0.001f;
		transitionTarget = cockpitPart.transform;
		SceneSingleton<CameraStateManager>.i.cameraPivot.transform.SetParent(null);
	}

	public override void UpdateState(CameraStateManager cam)
	{
		if (cam.followingRB == null)
		{
			return;
		}
		Vector3 vector = cam.followingRB.velocity;
		CheckInput(cam);
		if (transitionTimer > 0f)
		{
			cam.cameraPivot.SetParent(null);
			Aircraft aircraft = cam.followingUnit as Aircraft;
			transitionTimer += Time.deltaTime;
			cam.cameraPivot.position = Vector3.Lerp(aircraft.transform.position, aircraft.cockpit.transform.position, transitionTimer);
			vector = Vector3.Lerp(vector, aircraft.cockpit.rb.velocity, transitionTimer);
			if (transitionTimer > 1f)
			{
				cam.followingRB = aircraft.cockpit.rb;
				cam.cameraPivot.SetParent(aircraft.cockpit.transform);
				cam.cameraPivot.localPosition = Vector3.zero;
				transitionTimer = 0f;
			}
		}
		else
		{
			cam.cameraPivot.localPosition = Vector3.zero;
		}
		cam.cameraVelocity = vector * Time.timeScale;
		FOVAdjustment -= cam.fovChangeSpeed * Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("FOV");
		FOVAdjustment = Mathf.Clamp(FOVAdjustment, minFOV - cam.desiredFOV, maxFOV - cam.desiredFOV);
		float b = Mathf.Clamp(cam.desiredFOV + FOVAdjustment, minFOV, maxFOV);
		cam.mainCamera.fieldOfView = Mathf.Lerp(cam.mainCamera.fieldOfView, b, 1f / (1f + 100f * cam.fovChangeInertia));
		float num = 1f + viewDistAdjust;
		Vector3 vector2 = (posVector.x * cam.followingUnit.transform.right + posVector.y * cam.followingUnit.transform.up + posVector.z * cam.followingUnit.transform.forward) * num;
		Quaternion b2 = Quaternion.LookRotation(targetVector.x * cam.followingUnit.transform.right + targetVector.y * cam.followingUnit.transform.up + targetVector.z * cam.followingUnit.transform.forward, cam.followingUnit.transform.up);
		cam.transform.position = Vector3.Lerp(cam.transform.position, cam.cameraPivot.position + vector2, Mathf.Min(3f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		Vector3 vector3 = cam.cameraPivot.position - cam.transform.position;
		if (Physics.Linecast(cam.cameraPivot.position, cam.cameraPivot.position - vector3 * 2f, out hit, layerMask))
		{
			float num2 = Mathf.Max(Vector3.Dot(vector3.normalized, hit.normal), 0.1f);
			Vector3 vector4 = hit.point + vector3.normalized / num2;
			if (FastMath.SquareDistance(vector4, cam.cameraPivot.position) < vector3.sqrMagnitude)
			{
				cam.transform.position = vector4;
			}
		}
		cam.cameraPivot.rotation = Quaternion.Lerp(cameraRotationPrev, b2, Mathf.Min(5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		cameraRotationPrev = cam.cameraPivot.rotation;
		cam.transform.localRotation = Quaternion.Lerp(cam.transform.localRotation, cameraCustomRotation, Mathf.Min(5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		if (GameManager.flightControlsEnabled)
		{
			if (!Cursor.visible)
			{
				zoomSpeed -= GameManager.playerInput.GetAxis("Zoom View") * 40f * Time.unscaledDeltaTime;
			}
			zoomSpeed = Mathf.Lerp(zoomSpeed, 0f, 4f * Time.unscaledDeltaTime);
			zoomSpeed = Mathf.Clamp(zoomSpeed, -10f, 10f);
			viewDistAdjust += zoomSpeed * Time.unscaledDeltaTime;
			viewDistAdjust = Mathf.Clamp(viewDistAdjust, 0f, 10f);
			if (GameManager.gameState != GameState.Editor && GameManager.playerInput.GetButtonDown("Center"))
			{
				cam.SwitchState(cam.orbitState);
			}
			if (GameManager.gameState != GameState.Editor && GameManager.playerInput.GetButtonTimedPressUp("Switch View", 0f, PlayerSettings.clickDelay))
			{
				cam.SwitchState(cam.TVState);
			}
			if (!GameManager.IsLocalAircraft(cam.followingUnit) && AnyMoveInput())
			{
				cam.SetFollowingUnit(null);
				cam.SwitchState(cam.freeState);
			}
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
	}

	public void CheckInput(CameraStateManager cam)
	{
		if (!AircraftCameraPos.TryGetValue(cam.followingUnit.definition as AircraftDefinition, out var value))
		{
			Debug.Log($"Chase State - no camera position found for {cam.followingUnit.definition}");
		}
		else if (!SceneSingleton<CameraControlUI>.i.isOpen)
		{
			if (Input.GetKeyDown(KeyCode.Keypad0))
			{
				currentPos = ChasePos.Back;
				posVector = value.back.position;
				targetVector = value.back.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad1))
			{
				currentPos = ChasePos.WingRootL;
				posVector = value.wingRootL.position;
				targetVector = value.wingRootL.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad2))
			{
				currentPos = ChasePos.Belly;
				posVector = value.belly.position;
				targetVector = value.belly.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad3))
			{
				currentPos = ChasePos.WingRootR;
				posVector = value.wingRootR.position;
				targetVector = value.wingRootR.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad4))
			{
				currentPos = ChasePos.WingL;
				posVector = value.wingL.position;
				targetVector = value.wingL.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad5))
			{
				currentPos = ChasePos.Tail;
				posVector = value.tail.position;
				targetVector = value.tail.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad6))
			{
				currentPos = ChasePos.WingR;
				posVector = value.wingR.position;
				targetVector = value.wingR.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad7))
			{
				currentPos = ChasePos.Bottom;
				posVector = value.bottom.position;
				targetVector = value.bottom.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad8))
			{
				currentPos = ChasePos.Front;
				posVector = value.front.position;
				targetVector = value.front.orientation;
			}
			else if (Input.GetKeyDown(KeyCode.Keypad9))
			{
				currentPos = ChasePos.Top;
				posVector = value.top.position;
				targetVector = value.top.orientation;
			}
			if (GameManager.gameState == GameState.Editor && Input.GetKeyDown(KeyCode.H))
			{
				ToggleHUD();
			}
		}
	}

	public void CheckHUD()
	{
		if (!showHUD)
		{
			FlightHud.EnableCanvas(enable: false);
		}
		else if (currentPos == ChasePos.Back || currentPos == ChasePos.Tail || currentPos == ChasePos.WingRootL || currentPos == ChasePos.WingRootR || currentPos == ChasePos.Belly)
		{
			FlightHud.EnableCanvas(enable: true);
			DynamicMap.EnableCanvas(enable: true);
		}
		else
		{
			FlightHud.EnableCanvas(enable: false);
			DynamicMap.EnableCanvas(enable: false);
		}
	}

	public void ToggleHUD()
	{
		showHUD = !showHUD;
		CheckHUD();
	}

	public void SetCustomTransform(Vector3 position, Vector3 eulerAngles)
	{
		currentPos = ChasePos.Custom;
		posVector = position;
		cameraCustomRotation = Quaternion.Euler(eulerAngles);
	}
}
