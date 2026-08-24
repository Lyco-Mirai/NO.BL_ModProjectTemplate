using NuclearOption.MissionEditorScripts;
using UnityEngine;

public class CameraRelativeState : CameraBaseState
{
	private float desiredFOV = 50f;

	public override void EnterState(CameraStateManager cam)
	{
		cam.cameraPivot.SetParent(null);
		cam.transform.SetParent(cam.followingUnit.transform, worldPositionStays: true);
		CameraStateManager.cameraMode = CameraMode.free;
		FlightHud.EnableCanvas(enable: false);
	}

	public override void LeaveState(CameraStateManager cam)
	{
	}

	public override void UpdateState(CameraStateManager cam)
	{
		cam.windNoiseExternal.volume = 0f;
		if (Input.GetKey(KeyCode.PageUp))
		{
			desiredFOV -= 25f * Time.unscaledDeltaTime;
		}
		if (Input.GetKey(KeyCode.PageDown))
		{
			desiredFOV += 25f * Time.unscaledDeltaTime;
		}
		desiredFOV = Mathf.Clamp(desiredFOV, 5f, 90f);
		cam.mainCamera.fieldOfView = Mathf.Lerp(cam.mainCamera.fieldOfView, desiredFOV, Time.unscaledDeltaTime);
		if (Input.GetMouseButton(1))
		{
			CursorManager.Refresh();
		}
		if (!InputFieldChecker.InsideInputField)
		{
			GameManager.playerInput.GetAxis("Move Longitudinal");
			GameManager.playerInput.GetAxis("Move Lateral");
			if (cam.followingUnit == null || cam.followingUnit.disabled)
			{
				cam.SetFollowingUnit(null);
				cam.SwitchState(cam.freeState);
			}
		}
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
	}
}
