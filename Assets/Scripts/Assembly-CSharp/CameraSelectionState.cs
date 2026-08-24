using UnityEngine;

public class CameraSelectionState : CameraBaseState
{
	private bool manualViewMode;

	private float cameraDistance = 10f;

	private float cameraSmoothingVel;

	private float cameraHeight;

	private bool allowMoveToDropFocus;

	private float viewDistance;

	private bool fixedTarget;

	private Transform target;

	private float targetSize;

	private Transform followProxy;

	private float orbitalAngle;

	public void FocusAirbase(CameraStateManager cam, Airbase airbase, bool allowMoveToDropFocus, float viewDistance = 20f, float upDistance = 1.75f)
	{
		if (airbase.fixedCameraTransform != null)
		{
			fixedTarget = true;
			target = airbase.fixedCameraTransform;
		}
		else
		{
			fixedTarget = false;
			target = airbase.aircraftSelectionTransform;
		}
		this.allowMoveToDropFocus = allowMoveToDropFocus;
		cam.SwitchState(this);
		if (followProxy != null)
		{
			Object.Destroy(followProxy.gameObject);
		}
		followProxy = new GameObject("CameraFollowProxy").transform;
		followProxy.SetParent(airbase.center.transform);
		cameraDistance = viewDistance;
		this.viewDistance = viewDistance;
		Vector3 vector = Vector3.up * upDistance;
		Vector3 vector2 = airbase.aircraftSelectionTransform.position + vector;
		cam.cameraPivot.SetPositionAndRotation(vector2, Quaternion.identity);
		followProxy.SetPositionAndRotation(vector2, Quaternion.identity);
		Vector3 position = vector2 + Vector3.forward * (viewDistance + 8f);
		cam.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.back, Vector3.up));
		orbitalAngle = 30f;
		UpdateOrbit(cam);
	}

	private void UpdateOrbit(CameraStateManager cam)
	{
		Quaternion quaternion = ((!(followProxy != null)) ? Quaternion.identity : followProxy.rotation);
		orbitalAngle %= 360f;
		Quaternion rotation = quaternion * Quaternion.Euler(0f, orbitalAngle, 0f);
		cam.cameraPivot.rotation = rotation;
	}

	public void SetPreviewAircraft(Aircraft previewAircraft)
	{
		if (!fixedTarget)
		{
			target = previewAircraft.transform;
		}
		viewDistance = (Mathf.Max(previewAircraft.definition.length, previewAircraft.definition.width * 0.7f) + Mathf.Max(previewAircraft.definition.width, previewAircraft.definition.length * 0.7f)) * 0.5f;
		float value = Mathf.Pow(viewDistance * 0.1f, 0.2f);
		value = Mathf.Clamp(value, 0.6f, 1.5f);
		viewDistance /= value;
	}

	public override void EnterState(CameraStateManager cam)
	{
		cam.mainCamera.nearClipPlane = 1f;
		cam.mainCamera.fieldOfView = 50f;
		FlightHud.EnableCanvas(enable: false);
		cam.transform.SetParent(cam.cameraPivot);
		CameraStateManager.cameraMode = CameraMode.selection;
		cam.cameraVelocity = Vector3.zero;
		SceneSingleton<DynamicMap>.i.Minimize();
	}

	public override void LeaveState(CameraStateManager cam)
	{
		cam.mainCamera.fieldOfView = cam.desiredFOV;
		if (followProxy != null)
		{
			Object.Destroy(followProxy.gameObject);
		}
	}

	public override void UpdateState(CameraStateManager cam)
	{
		if (target == null)
		{
			ColorLog<CameraSelectionState>.Info("Target destroyed changing to free cam");
			cam.SetFollowingUnit(null);
			cam.SwitchState(cam.freeState);
			return;
		}
		if (followProxy != null)
		{
			cam.cameraPivot.position = followProxy.position;
		}
		if (fixedTarget)
		{
			target.GetPositionAndRotation(out var position, out var rotation);
			cam.transform.SetPositionAndRotation(position, rotation);
		}
		else
		{
			float unscaledDeltaTime = Time.unscaledDeltaTime;
			MoveCamera(cam, unscaledDeltaTime);
		}
		if (allowMoveToDropFocus)
		{
			CheckChangeState(cam);
		}
	}

	private void MoveCamera(CameraStateManager cam, float deltaTime)
	{
		float num = GameManager.playerInput.GetAxis("Pan View") * PlayerSettings.viewSensitivity;
		float num2 = GameManager.playerInput.GetAxis("Tilt View") * PlayerSettings.viewSensitivity;
		num *= -0.2f;
		num2 *= -0.2f;
		if (!GameManager.playerInput.GetButton("Free Look"))
		{
			num = 0f;
			num2 = 0f;
		}
		if (Mathf.Abs(num) > 0.75f || Mathf.Abs(num2) > 0.75f)
		{
			manualViewMode = true;
		}
		if (manualViewMode)
		{
			orbitalAngle += num * -150f * deltaTime;
			cameraHeight += num2 * -0.5f * deltaTime;
			cameraHeight = Mathf.Clamp01(cameraHeight);
		}
		else
		{
			orbitalAngle -= 12f * deltaTime;
		}
		UpdateOrbit(cam);
		Quaternion b = Quaternion.LookRotation(target.position - cam.transform.position);
		Vector3 position = cam.cameraPivot.position + (cam.cameraPivot.forward + Vector3.up * cameraHeight) * cameraDistance * 1.4f;
		Quaternion rotation = Quaternion.Slerp(cam.transform.rotation, b, 5f * deltaTime);
		cam.transform.SetPositionAndRotation(position, rotation);
		cameraDistance = Mathf.SmoothDamp(cameraDistance, viewDistance, ref cameraSmoothingVel, 0.5f, float.MaxValue, deltaTime);
	}

	private void CheckChangeState(CameraStateManager cam)
	{
		if (AnyMoveInput())
		{
			cam.SetFollowingUnit(null);
			cam.SwitchState(cam.freeState);
		}
		static bool AnyMoveInput()
		{
			if (!(Mathf.Abs(GameManager.playerInput.GetAxis("Move Longitudinal")) > 0.1f))
			{
				return Mathf.Abs(GameManager.playerInput.GetAxis("Move Lateral")) > 0.1f;
			}
			return true;
		}
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
	}
}
