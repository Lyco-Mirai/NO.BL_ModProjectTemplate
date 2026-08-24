using UnityEngine;

public class CameraTVState : CameraBaseState
{
	private RaycastHit hit;

	private Vector2 panTiltView;

	private Vector2 desiredPanTiltView;

	private CameraStateManager cam;

	private float shotTime;

	private float camTimer = 3f;

	private float targetSize;

	private float zoomSpeed;

	private float FOVAdjustment;

	private float transitionTimer;

	private Vector3 minimumOffset = Vector3.zero;

	public override void EnterState(CameraStateManager cam)
	{
		if (PlayerSettings.cinematicMode)
		{
			CursorManager.Refresh();
		}
		if (cam.followingUnit is Aircraft aircraft)
		{
			if (aircraft.cockpit.IsDetached())
			{
				cam.followingRB = aircraft.cockpit.rb;
				transitionTimer = 0f;
			}
			else
			{
				aircraft.cockpit.onParentDetached += CameraTVState_OnCockpitDetach;
			}
		}
		cam.followingUnit.SetDoppler(enabled: true);
		cam.cameraPivot.SetParent(Datum.origin);
		cam.transform.SetParent(cam.cameraPivot);
		cam.transform.localPosition = Vector3.zero;
		cam.cameraVelocity = Vector3.zero;
		cam.mainCamera.nearClipPlane = 1f;
		FlightHud.EnableCanvas(enable: false);
		this.cam = cam;
		camTimer = 3f;
		shotTime = 0f;
		CameraStateManager.cameraMode = CameraMode.tv;
		FOVAdjustment = 0f;
		targetSize = 20f;
		if (cam.followingUnit != null)
		{
			targetSize = Mathf.Max(cam.followingUnit.definition.length, cam.followingUnit.definition.width * 0.7f) + Mathf.Max(cam.followingUnit.definition.width, cam.followingUnit.definition.length * 0.5f);
			float value = Mathf.Pow(targetSize * 0.1f, 0.2f);
			value = Mathf.Clamp(value, 0.6f, 1.5f);
			targetSize /= value;
		}
		if (cam.followingUnit != null)
		{
			GetTVCamPoint(cam.followingUnit.transform.position, cam.followingUnit.rb.velocity);
		}
	}

	private void CameraTVState_OnCockpitDetach(UnitPart cockpitPart)
	{
		transitionTimer = 0.0001f;
	}

	public override void LeaveState(CameraStateManager cam)
	{
		cam.mainCamera.fieldOfView = cam.desiredFOV;
		if (cam.followingUnit is Aircraft aircraft)
		{
			aircraft.cockpit.onParentDetached -= CameraTVState_OnCockpitDetach;
		}
	}

	public override void UpdateState(CameraStateManager cam)
	{
		if (Physics.Linecast(cam.cameraPivot.position + Vector3.up * 2000f, cam.cameraPivot.position - Vector3.up * 1.7f, out hit, ~(int)PhysicsLayers.ExclusionZonesMask))
		{
			cam.cameraPivot.position = hit.point + Vector3.up * 1.7f;
		}
		cam.cameraVelocity = Vector3.zero;
		float num = Vector3.Distance(cam.followingRB.position, cam.transform.position);
		cam.transform.LookAt(cam.followingRB.transform.position);
		if (transitionTimer > 0f)
		{
			Aircraft aircraft = cam.followingUnit as Aircraft;
			transitionTimer += Time.deltaTime;
			Vector3 worldPosition = Vector3.Lerp(cam.followingRB.position, aircraft.cockpit.transform.position, transitionTimer);
			cam.transform.LookAt(worldPosition);
			if (transitionTimer > 1f)
			{
				transitionTimer = 0f;
				cam.followingRB = aircraft.cockpit.rb;
			}
		}
		desiredPanTiltView.x += GameManager.playerInput.GetAxis("Pan View") * 3f * Time.unscaledDeltaTime;
		desiredPanTiltView.y += GameManager.playerInput.GetAxis("Tilt View") * 3f * Time.unscaledDeltaTime;
		panTiltView = Vector2.Lerp(panTiltView, desiredPanTiltView, Mathf.Min(0.5f * Time.unscaledDeltaTime / PlayerSettings.viewSmoothing, 1f));
		cam.transform.rotation *= Quaternion.AngleAxis(panTiltView.x, Vector3.up);
		cam.transform.rotation *= Quaternion.AngleAxis(panTiltView.y, Vector3.right);
		float num2 = 1.5f * targetSize - num * 0.2f;
		FOVAdjustment -= 1f * GameManager.playerInput.GetAxis("Zoom View");
		FOVAdjustment = Mathf.Clamp(FOVAdjustment, 5f - num2, 80f - num2);
		float target = Mathf.Clamp(num2 + FOVAdjustment, 5f, 80f);
		cam.mainCamera.fieldOfView = Mathf.SmoothDamp(cam.mainCamera.fieldOfView, target, ref zoomSpeed, 0.7f, float.MaxValue, Time.unscaledDeltaTime);
		if (Vector3.Dot(cam.followingRB.velocity, cam.transform.position - cam.followingRB.position) < 0f)
		{
			shotTime += Time.deltaTime;
		}
		else
		{
			shotTime = 0f;
		}
		if (shotTime > camTimer)
		{
			GetTVCamPoint(cam.followingRB.position, cam.followingRB.velocity);
			shotTime = 0f;
		}
		if (GameManager.flightControlsEnabled)
		{
			if (GameManager.playerInput.GetButtonDown("Center"))
			{
				panTiltView = Vector2.zero;
			}
			if (GameManager.playerInput.GetButtonTimedPressUp("Switch View", 0f, PlayerSettings.clickDelay))
			{
				cam.SwitchState((cam.followingUnit.cockpitViewPoint != null) ? ((CameraBaseState)cam.cockpitState) : ((CameraBaseState)cam.orbitState));
			}
		}
	}

	public override void FixedUpdateState(CameraStateManager cam)
	{
	}

	private void GetTVCamPoint(Vector3 targetPos, Vector3 targetVel)
	{
		panTiltView = Vector2.zero;
		desiredPanTiltView = Vector2.zero;
		Vector3 vector = targetPos + targetVel * 2f + minimumOffset;
		Vector3 vector2 = Vector3.Cross(targetVel, Vector3.up).normalized * Mathf.Sign(Random.value - 0.5f) * Random.Range(1, 4) * targetSize;
		Vector3 vector3 = vector + vector2;
		vector3 += Random.Range(-20, 20) * Vector3.up;
		if (targetVel.sqrMagnitude < 10f)
		{
			vector3 = targetPos + new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1)).normalized * Random.Range(20, 50);
		}
		if (Physics.Linecast(vector3 + Vector3.up * 2000f, vector3 - Vector3.up * 1.7f, out hit, ~(int)PhysicsLayers.ExclusionZonesMask))
		{
			vector3.y = hit.point.y + 1.7f;
		}
		GlobalPosition globalPosition = vector3.ToGlobalPosition();
		globalPosition.y = Mathf.Max(globalPosition.y, 1.7f);
		cam.cameraPivot.localPosition = globalPosition.AsVector3();
	}

	public void SetCustomTransform(Vector3 position, Vector3 eulerAngles)
	{
		minimumOffset = position;
	}
}
