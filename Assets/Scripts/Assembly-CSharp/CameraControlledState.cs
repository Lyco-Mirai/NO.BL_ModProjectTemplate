using UnityEngine;

public class CameraControlledState : CameraBaseState
{
	private enum VectorUp
	{
		World = 0,
		Unit = 1
	}

	private Vector3 cameraPosition;

	private Vector3 cameraAngle;

	private Vector3 pivotPosition;

	private Vector3 pivotAngle;

	private Vector3 pivotRotation;

	private Vector3 cameraTranslation;

	private Vector3 translationVector;

	private Vector3 rotationVector;

	private Vector3 moveVector;

	private float FOVAdjustment;

	private float minFOV = 1f;

	private float maxFOV = 120f;

	private Quaternion pivotRotationPrev;

	private Transform lookAt;

	private VectorUp vectorUp;

	private RaycastHit hit;

	private int layerMask = PhysicsLayers.StaticsMask;

	public override void EnterState(CameraStateManager cam)
	{
		if (cam.followingUnit != null)
		{
			cam.cameraPivot.SetParent(cam.followingUnit.transform);
			cam.cameraPivot.localPosition = Vector3.zero;
			cam.cameraPivot.localEulerAngles = Vector3.zero;
			cam.transform.SetParent(cam.cameraPivot, worldPositionStays: true);
			vectorUp = VectorUp.Unit;
		}
		else
		{
			cam.cameraPivot.position = cam.transform.position;
			cam.cameraPivot.eulerAngles = Vector3.zero;
			cam.cameraPivot.SetParent(Datum.origin, worldPositionStays: true);
			cam.transform.SetParent(cam.cameraPivot);
			cam.transform.localPosition = Vector3.zero;
			vectorUp = VectorUp.World;
		}
		CameraStateManager.cameraMode = CameraMode.free;
		FlightHud.EnableCanvas(enable: false);
		FOVAdjustment = 0f;
		pivotPosition = cam.cameraPivot.localPosition;
		pivotAngle = cam.cameraPivot.localEulerAngles;
		cameraPosition = cam.transform.localPosition;
		cameraAngle = cam.transform.localEulerAngles;
		moveVector = Vector3.zero;
		cameraTranslation = Vector3.zero;
		pivotRotation = Vector3.zero;
		translationVector = Vector3.zero;
		rotationVector = Vector3.zero;
		lookAt = null;
		if (cam.followingUnit != null)
		{
			pivotRotationPrev = cam.cameraPivot.localRotation;
		}
		else
		{
			pivotRotationPrev = cam.transform.localRotation;
		}
	}

	public override void LeaveState(CameraStateManager cam)
	{
	}

	public override void UpdateState(CameraStateManager cam)
	{
		if (GameManager.playerInput.GetButton("Free Look"))
		{
			float num = Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("Pan View") * 0.3f * PlayerSettings.viewSensitivity * (float)((!PlayerSettings.viewInvertPitch) ? 1 : (-1));
			float num2 = Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("Tilt View") * 0.3f * PlayerSettings.viewSensitivity;
			if (Mathf.Abs(num) > 0f || Mathf.Abs(num2) > 0f)
			{
				cameraAngle += cam.desiredRotSpeed * (num * Vector3.up + num2 * Vector3.right);
			}
		}
		moveVector = Vector3.zero;
		float axis = GameManager.playerInput.GetAxis("Move Longitudinal");
		float axis2 = GameManager.playerInput.GetAxis("Move Lateral");
		float axis3 = GameManager.playerInput.GetAxis("Move Vertical");
		if (cam.allowInputs && (axis != 0f || axis2 != 0f || axis3 != 0f))
		{
			float num3 = 50f;
			if (Input.GetKey(KeyCode.LeftShift))
			{
				num3 = 500f;
			}
			moveVector = cam.desiredTransSpeed * num3 * Time.unscaledDeltaTime * (cam.transform.forward * axis + cam.transform.right * axis2 + Vector3.up * axis3);
		}
		if (cam.followingUnit != null)
		{
			UpdateFollowing(cam);
		}
		else
		{
			UpdateFree(cam);
		}
		FOVAdjustment -= cam.fovChangeSpeed * Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("FOV");
		FOVAdjustment = Mathf.Clamp(FOVAdjustment, minFOV - cam.desiredFOV, maxFOV - cam.desiredFOV);
		float b = Mathf.Clamp(cam.desiredFOV + FOVAdjustment, minFOV, maxFOV);
		cam.mainCamera.fieldOfView = Mathf.Lerp(cam.mainCamera.fieldOfView, b, 1f / (1f + 100f * cam.fovChangeInertia));
		if (Physics.Linecast(cam.transform.position + Vector3.up * 5000f, cam.transform.position - Vector3.up * 5000f, out hit, PhysicsLayers.StaticsMask))
		{
			cam.transform.position = new Vector3(cam.transform.position.x, Mathf.Max(cam.transform.position.y, hit.point.y + 1.7f), cam.transform.position.z);
		}
	}

	private void UpdateFree(CameraStateManager cam)
	{
		Vector3 vector = pivotPosition.x * Vector3.right + pivotPosition.y * Vector3.up + pivotPosition.z * Vector3.forward;
		if (translationVector.magnitude > 0f)
		{
			cameraTranslation += (translationVector.x * cam.cameraPivot.right + translationVector.y * cam.cameraPivot.up + translationVector.z * cam.cameraPivot.forward) * Time.unscaledDeltaTime;
		}
		cam.cameraPivot.localPosition = Vector3.Lerp(cam.cameraPivot.localPosition, vector + cameraTranslation, Mathf.Min(3f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		cam.cameraPivot.localRotation = Quaternion.Lerp(cam.cameraPivot.localRotation, Quaternion.Euler(pivotAngle), Mathf.Min(5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		if (moveVector.magnitude > 0f)
		{
			pivotPosition += moveVector;
		}
		Vector3 vector2 = cameraPosition.x * cam.cameraPivot.right + cameraPosition.y * cam.cameraPivot.up + cameraPosition.z * cam.cameraPivot.forward;
		cam.transform.position = Vector3.Lerp(cam.transform.position, cam.cameraPivot.position + vector2, Mathf.Min(3f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		if (lookAt != null)
		{
			cam.transform.LookAt(lookAt);
		}
		else
		{
			if (rotationVector.magnitude > 0f)
			{
				pivotRotation += rotationVector * Time.unscaledDeltaTime;
			}
			cam.transform.localRotation = Quaternion.Lerp(pivotRotationPrev, Quaternion.Euler(cameraAngle + pivotRotation), Mathf.Min(5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		}
		pivotRotationPrev = cam.transform.localRotation;
		if (GameManager.playerInput.GetButtonTimedPressUp("Switch View", 0f, PlayerSettings.clickDelay))
		{
			cam.SwitchState(cam.freeState);
		}
	}

	private void UpdateFollowing(CameraStateManager cam)
	{
		Vector3 vector = pivotPosition.x * cam.followingUnit.transform.right + pivotPosition.y * cam.followingUnit.transform.up + pivotPosition.z * cam.followingUnit.transform.forward;
		cam.cameraPivot.position = Vector3.Lerp(cam.cameraPivot.position, cam.followingUnit.transform.position + vector, Mathf.Min(3f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		if (rotationVector.magnitude > 0f)
		{
			pivotRotation += rotationVector * Time.unscaledDeltaTime;
		}
		if (vectorUp == VectorUp.World)
		{
			cam.cameraPivot.rotation = Quaternion.Lerp(pivotRotationPrev, Quaternion.Euler(cam.followingUnit.transform.eulerAngles + pivotAngle + pivotRotation), Mathf.Min(5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
			pivotRotationPrev = cam.cameraPivot.rotation;
			cam.cameraPivot.rotation = Quaternion.LookRotation(cam.cameraPivot.forward, Vector3.up);
		}
		else
		{
			cam.cameraPivot.localRotation = Quaternion.Lerp(pivotRotationPrev, Quaternion.Euler(pivotAngle + pivotRotation), Mathf.Min(5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
			pivotRotationPrev = cam.cameraPivot.localRotation;
		}
		if (translationVector.magnitude > 0f)
		{
			cameraTranslation += translationVector * Time.unscaledDeltaTime;
		}
		_ = cameraPosition.x * cam.cameraPivot.right + cameraPosition.y * cam.cameraPivot.up + cameraPosition.z * cam.cameraPivot.forward;
		cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, cameraPosition + cameraTranslation, Mathf.Min(3f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		if (moveVector.magnitude > 0f)
		{
			cameraPosition += new Vector3(Vector3.Dot(moveVector, cam.cameraPivot.right), Vector3.Dot(moveVector, cam.cameraPivot.up), Vector3.Dot(moveVector, cam.cameraPivot.forward));
		}
		if (lookAt != null)
		{
			cam.transform.LookAt(lookAt);
		}
		else
		{
			cam.transform.localRotation = Quaternion.Lerp(cam.transform.localRotation, Quaternion.Euler(cameraAngle), Mathf.Min(5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		}
		if (GameManager.playerInput.GetButtonTimedPressUp("Switch View", 0f, PlayerSettings.clickDelay))
		{
			cam.SwitchState(cam.orbitState);
		}
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
	}

	public void SetCustomTransform(Vector3 position, Vector3 eulerAngles)
	{
		cameraPosition = position;
		cameraAngle = eulerAngles;
	}

	public void SetCustomPivot(Vector3 position, Vector3 eulerAngles)
	{
		if (SceneSingleton<CameraStateManager>.i.followingUnit != null)
		{
			pivotPosition = position;
		}
		else
		{
			pivotPosition = position.ToGlobalPosition().AsVector3();
		}
		pivotAngle = eulerAngles;
	}

	public void SwitchPivotUp()
	{
		if (!(SceneSingleton<CameraStateManager>.i.followingUnit == null))
		{
			if (vectorUp == VectorUp.World)
			{
				vectorUp = VectorUp.Unit;
			}
			else
			{
				vectorUp = VectorUp.World;
			}
		}
	}

	public string GetPivotUp()
	{
		if (vectorUp != VectorUp.World)
		{
			return "U";
		}
		return "W";
	}

	public void SetCustomMovement(Vector3 translation, Vector3 rotation, bool cancel)
	{
		translationVector = translation;
		rotationVector = rotation;
		if (cancel)
		{
			cameraTranslation = Vector3.zero;
			pivotRotation = Vector3.zero;
		}
	}

	public void SetLookAt(Transform target)
	{
		lookAt = target;
	}

	public bool IsLookingAt()
	{
		return lookAt != null;
	}
}
