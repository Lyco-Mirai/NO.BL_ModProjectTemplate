using System;
using UnityEngine;

public class CameraEncyclopediaState : CameraBaseState
{
	private bool manualViewMode;

	private float cameraDistance = 10f;

	private float cameraSmoothingVel;

	private float cameraHeight;

	private float cameraAngle;

	private float cameraHeightSmoothed;

	private float cameraDistSmoothed;

	private float cameraHeightSmoothingVel;

	private float cameraDistSmoothingVel;

	private float distPrev;

	private bool allowMoveToDropFocus;

	private float viewDistance;

	private bool fixedTarget;

	private Transform target;

	private float targetSize;

	public override void EnterState(CameraStateManager cam)
	{
		cam.mainCamera.nearClipPlane = 1f;
		cam.mainCamera.fieldOfView = 50f;
		viewDistance = 20f;
		cam.cameraVelocity = Vector3.zero;
		cameraHeightSmoothed = 1.8f;
	}

	public override void LeaveState(CameraStateManager cam)
	{
	}

	public override void UpdateState(CameraStateManager cam)
	{
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
	}

	private void MoveCamera(CameraStateManager cam, float deltaTime)
	{
		if (!(SceneSingleton<EncyclopediaBrowser>.i.spawnedUnitObject == null))
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
			Unit spawnedUnit = SceneSingleton<EncyclopediaBrowser>.i.GetSpawnedUnit();
			float num3 = spawnedUnit.maxRadius * 2.6f;
			if (num3 != distPrev)
			{
				cameraHeight = num3 * 0.25f;
			}
			distPrev = num3;
			cameraDistSmoothed = Mathf.Max(cameraDistSmoothed, num3 * 0.5f);
			cameraDistSmoothed = Mathf.SmoothDamp(cameraDistSmoothed, num3, ref cameraDistSmoothingVel, 1f);
			cameraAngle += num * 100f * Time.deltaTime;
			cameraHeight += num2 * -3f * Time.deltaTime * cameraDistSmoothed;
			cameraHeight = Mathf.Clamp(cameraHeight, 1.7f, num3);
			cameraHeightSmoothed = Mathf.Max(cameraHeightSmoothed, 1.7f);
			cameraHeightSmoothed = Mathf.SmoothDamp(cameraHeightSmoothed, cameraHeight, ref cameraHeightSmoothingVel, 0.25f);
			if (Mathf.Abs(num) > 0.75f || Mathf.Abs(num2) > 0.75f)
			{
				manualViewMode = true;
			}
			if (!manualViewMode)
			{
				cameraAngle += 15f * Time.deltaTime;
			}
			Vector3 vector = new Vector3(Mathf.Cos(cameraAngle * (MathF.PI / 180f)), 0f, Mathf.Sin(cameraAngle * (MathF.PI / 180f)));
			cam.transform.position = SceneSingleton<EncyclopediaBrowser>.i.spawnedUnitObject.transform.position + vector.normalized * cameraDistSmoothed + Vector3.up * cameraHeightSmoothed;
			cam.transform.LookAt(SceneSingleton<EncyclopediaBrowser>.i.spawnedUnitObject.transform.position - spawnedUnit.definition.spawnOffset * 0.5f, Vector3.up);
		}
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
	}
}
