using NuclearOption.MissionEditorScripts;
using UnityEngine;

public class CameraFreeState : CameraBaseState
{
	private float FOVAdjustment;

	private float minFOV = 1f;

	private float maxFOV = 120f;

	private float stateTime;

	private RaycastHit hit;

	private bool trackTarget;

	private float panView;

	private float tiltView;

	public bool DontResetRotationFlag;

	public override void EnterState(CameraStateManager cam)
	{
		cam.cameraPivot.SetParent(null);
		cam.transform.SetParent(null, worldPositionStays: true);
		CameraStateManager.cameraMode = CameraMode.free;
		cam.cockpitCamRender.enabled = false;
		FlightHud.EnableCanvas(enable: false);
		FOVAdjustment = 0f;
		stateTime = 0f;
		trackTarget = true;
		panView = cam.transform.eulerAngles.y;
		if (DontResetRotationFlag)
		{
			tiltView = cam.transform.eulerAngles.x;
		}
		else
		{
			tiltView = 0f;
		}
		DontResetRotationFlag = false;
		if (cam.previousFollowingUnit != null)
		{
			cam.transform.LookAt(cam.previousFollowingUnit.transform.position);
		}
	}

	public override void LeaveState(CameraStateManager cam)
	{
	}

	public override void UpdateState(CameraStateManager cam)
	{
		stateTime += Time.deltaTime;
		cam.windNoiseExternal.volume = 0f;
		FOVAdjustment -= cam.fovChangeSpeed * Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("FOV");
		FOVAdjustment = Mathf.Clamp(FOVAdjustment, minFOV - cam.desiredFOV, maxFOV - cam.desiredFOV);
		float b = Mathf.Clamp(cam.desiredFOV + FOVAdjustment, minFOV, maxFOV);
		cam.mainCamera.fieldOfView = Mathf.Lerp(cam.mainCamera.fieldOfView, b, 1f / (1f + 100f * cam.fovChangeInertia));
		if (Input.GetMouseButton(1))
		{
			CursorManager.Refresh();
		}
		bool flag = false;
		if (!InputFieldChecker.InsideInputField)
		{
			float num = Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("Pan View") * 0.3f * PlayerSettings.viewSensitivity * (float)((!PlayerSettings.viewInvertPitch) ? 1 : (-1));
			float num2 = Mathf.Min(cam.mainCamera.fieldOfView / 20f, 1f) * GameManager.playerInput.GetAxis("Tilt View") * 0.3f * PlayerSettings.viewSensitivity;
			if (!GameManager.playerInput.GetButton("Free Look"))
			{
				num = 0f;
				num2 = 0f;
			}
			if (Mathf.Abs(num) > 0f || Mathf.Abs(num2) > 0f)
			{
				tiltView += num2;
				panView += num;
				trackTarget = false;
			}
			float axis = GameManager.playerInput.GetAxis("Move Longitudinal");
			float axis2 = GameManager.playerInput.GetAxis("Move Lateral");
			float axis3 = GameManager.playerInput.GetAxis("Move Vertical");
			if (cam.allowInputs && (axis != 0f || axis2 != 0f || axis3 != 0f))
			{
				float num3 = 500f;
				if (Input.GetKey(KeyCode.LeftShift))
				{
					num3 = 5000f;
				}
				Vector3 vector = cam.desiredTransSpeed * num3 * Time.unscaledDeltaTime * (cam.transform.forward * axis + cam.transform.right * axis2 + Vector3.up * axis3);
				cam.cameraVelocity += vector;
				flag = true;
			}
		}
		cam.transform.GetPositionAndRotation(out var position, out var rotation);
		position += cam.cameraVelocity * Time.unscaledDeltaTime;
		rotation = Quaternion.Lerp(rotation, Quaternion.Euler(tiltView, panView, 0f), Mathf.Min(2f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		if ((SceneSingleton<MissionEditor>.i == null || !SceneSingleton<MissionEditor>.i.allowCameraClip) && Physics.Linecast(position + Vector3.up * 5000f, position - Vector3.up * 5000f, out hit, PhysicsLayers.StaticsMask))
		{
			position = new Vector3(position.x, Mathf.Max(position.y, hit.point.y + 1.7f), position.z);
		}
		float num4 = Datum.LocalSeaY + 1.7f;
		if (position.y < num4)
		{
			position.y = num4;
			cam.transform.position = position;
		}
		cam.transform.SetPositionAndRotation(position, rotation);
		float sqrMagnitude = cam.cameraVelocity.sqrMagnitude;
		float num5 = (flag ? 0.96f : ((!(sqrMagnitude < 1f)) ? Mathf.Lerp(0.7f, 0.96f, sqrMagnitude / 250000f) : 0f));
		cam.cameraVelocity *= num5;
		if (GameManager.playerInput.GetButtonTimedPressUp("Switch View", 0f, PlayerSettings.clickDelay))
		{
			cam.SwitchState(cam.controlledState);
		}
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
	}
}
