using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class TargetCam : MonoBehaviour
{
	public enum CamMode
	{
		targetForward = 0,
		targetRear = 1,
		landingMode = 2
	}

	public struct OnCamToggle
	{
		public bool enabled;

		public CamMode camMode;
	}

	private Camera cam;

	private Camera UICam;

	[SerializeField]
	private Transform currentMount;

	[SerializeField]
	private Transform camMountForward;

	[SerializeField]
	private Transform camMountRear;

	[SerializeField]
	private Transform camMountLanding;

	[SerializeField]
	private UnitPart attachedPart;

	[SerializeField]
	private Renderer targetScreenRenderer;

	[SerializeField]
	private float landingCamFoV = 90f;

	private Volume screenVolume;

	private ColorAdjustments colorAdjustments;

	private GlobalPosition targetPosition;

	private GlobalPosition targetPositionPrev;

	private float camTimeout;

	private float timeOnTarget;

	private GameObject canvasObjectTarget;

	private GameObject canvasObjectLanding;

	private TargetScreenUI targetScreenUI;

	private LandingScreenUI landingScreenUI;

	private Aircraft aircraft;

	private bool IRMode;

	private float targetFOV = 1f;

	private float targetDist;

	private Vector3 rotationalVelocity;

	private CamMode currentMode;

	private List<Unit> targetList;

	private float lastExposureUpdate;

	private Vector3 landingCamVector = Vector3.zero;

	[FormerlySerializedAs("enableLandingCam")]
	[SerializeField]
	private bool vtolLandingCam;

	public event Action<OnCamToggle> onCamToggle;

	public event Action<bool> onSetup;

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		if (camera == cam)
		{
			RenderSettings.fog = !IRMode;
		}
	}

	private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		if (camera == cam)
		{
			RenderSettings.fog = true;
		}
	}

	private void Awake()
	{
		aircraft = attachedPart.parentUnit as Aircraft;
		targetList = aircraft.weaponManager.GetTargetList();
		aircraft.Identity.OnStartClient.AddListener(Initialize);
		landingCamVector = camMountLanding.transform.localEulerAngles;
	}

	public void Initialize()
	{
		aircraft = attachedPart.parentUnit as Aircraft;
		if (aircraft != null && aircraft.Identity.HasAuthority)
		{
			camMountForward.localEulerAngles = Vector3.zero;
			camMountRear.localEulerAngles = new Vector3(0f, 180f, 0f);
			currentMount = camMountForward;
			Camera[] componentsInChildren = UnityEngine.Object.Instantiate(GameAssets.i.targetCam, currentMount).GetComponentsInChildren<Camera>();
			cam = componentsInChildren[0];
			UICam = componentsInChildren[1];
			screenVolume = cam.GetComponentInChildren<Volume>();
			screenVolume.enabled = false;
			attachedPart.onParentDetached += TargetCam_OnDetach;
			if (aircraft.cockpit != null)
			{
				aircraft.cockpit.onParentDetached += TargetCam_OnDetach;
			}
			aircraft.targetCam = this;
			aircraft.OnTouchdown += TargetCam_OnTouchdown;
			aircraft.onDisableUnit += TargetCam_OnUnitDisable;
			aircraft.onSetGear += TargetCam_OnSetGear;
			base.enabled = false;
			SwitchIRState(IR: false);
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
		}
	}

	private void SwitchIRState(bool IR)
	{
		IRMode = IR;
		if (!screenVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
		{
			throw new NullReferenceException("colorAdjustments");
		}
		colorAdjustments.saturation.overrideState = IR;
	}

	private void UpdateExposure()
	{
		if (!(aircraft == null))
		{
			float t = Mathf.InverseLerp(0.02f, 0.4f, NetworkSceneSingleton<LevelInfo>.i.GetAmbientLight());
			if (IRMode)
			{
				colorAdjustments.postExposure.value = Mathf.Lerp(3f, -0.5f, t);
				colorAdjustments.contrast.value = 1f;
			}
			else
			{
				colorAdjustments.postExposure.value = Mathf.Lerp(0.5f, -1f, t);
				colorAdjustments.contrast.value = 5f;
			}
		}
	}

	private void TargetCam_OnDetach(UnitPart part)
	{
		attachedPart.onParentDetached -= TargetCam_OnDetach;
		if (aircraft.cockpit != null)
		{
			aircraft.cockpit.onParentDetached -= TargetCam_OnDetach;
		}
		targetScreenRenderer.material = GameAssets.i.BSOD;
		UnityEngine.Object.Destroy(this);
	}

	private void TargetCam_OnUnitDisable(Unit unit)
	{
		UnityEngine.Object.Destroy(this);
		cam.enabled = false;
		UICam.enabled = false;
		UnityEngine.Object.Destroy(screenVolume);
	}

	private void TargetCam_OnTouchdown()
	{
		cam.enabled = false;
		currentMode = CamMode.targetForward;
		this.onCamToggle?.Invoke(new OnCamToggle
		{
			enabled = false,
			camMode = CamMode.landingMode
		});
		WeaponManager weaponManager = aircraft.weaponManager;
		if ((object)weaponManager != null && weaponManager.GetTargetList().Count > 0)
		{
			this.onCamToggle?.Invoke(new OnCamToggle
			{
				enabled = true,
				camMode = CamMode.targetForward
			});
		}
	}

	private void TargetCam_OnSetGear(Aircraft.OnSetGear g)
	{
		if ((!vtolLandingCam && PlayerSettings.landingCam == 1) || PlayerSettings.landingCam == 0)
		{
			return;
		}
		if (g.gearState == LandingGear.GearState.Extending || g.gearState == LandingGear.GearState.LockedExtended)
		{
			WeaponManager weaponManager = aircraft.weaponManager;
			if ((object)weaponManager != null && weaponManager.GetTargetList().Count > 0)
			{
				cam.enabled = false;
				currentMode = CamMode.landingMode;
				this.onCamToggle?.Invoke(new OnCamToggle
				{
					enabled = false,
					camMode = CamMode.targetForward
				});
			}
			SetLandingCam();
		}
		else if (g.gearState == LandingGear.GearState.Retracting || g.gearState == LandingGear.GearState.LockedRetracted)
		{
			cam.enabled = false;
			currentMode = CamMode.targetForward;
			this.onCamToggle?.Invoke(new OnCamToggle
			{
				enabled = false,
				camMode = CamMode.landingMode
			});
			WeaponManager weaponManager2 = aircraft.weaponManager;
			if ((object)weaponManager2 != null && weaponManager2.GetTargetList().Count > 0)
			{
				this.onCamToggle?.Invoke(new OnCamToggle
				{
					enabled = true,
					camMode = CamMode.targetForward
				});
			}
		}
	}

	private void OnDestroy()
	{
		if (attachedPart.parentUnit != null)
		{
			attachedPart.parentUnit.onDisableUnit -= TargetCam_OnUnitDisable;
		}
		attachedPart.onParentDetached -= TargetCam_OnDetach;
		if (aircraft != null)
		{
			aircraft.OnTouchdown -= TargetCam_OnTouchdown;
			aircraft.onSetGear -= TargetCam_OnSetGear;
		}
		if (targetScreenUI != null)
		{
			UnityEngine.Object.Destroy(targetScreenUI);
		}
		if (landingScreenUI != null)
		{
			UnityEngine.Object.Destroy(landingScreenUI);
		}
		if (canvasObjectTarget != null)
		{
			UnityEngine.Object.Destroy(canvasObjectTarget);
		}
		if (canvasObjectLanding != null)
		{
			UnityEngine.Object.Destroy(canvasObjectLanding);
		}
		RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
	}

	public void SetTargetCam()
	{
		if (currentMode == CamMode.landingMode)
		{
			return;
		}
		screenVolume.enabled = true;
		foreach (Unit target in targetList)
		{
			target.displayDetail = 1f;
		}
		if (canvasObjectLanding != null && canvasObjectLanding.activeSelf)
		{
			canvasObjectLanding.SetActive(value: false);
		}
		if (canvasObjectTarget == null)
		{
			canvasObjectTarget = UnityEngine.Object.Instantiate(GameAssets.i.targetScreenCanvas, null);
			targetScreenUI = canvasObjectTarget.GetComponent<TargetScreenUI>();
			targetScreenUI.SetupCamera(cam, UICam, aircraft);
		}
		else if (canvasObjectTarget != null && !canvasObjectTarget.activeSelf)
		{
			canvasObjectTarget.SetActive(value: true);
		}
		base.enabled = true;
		if (!cam.enabled)
		{
			currentMount = camMountForward;
			cam.transform.parent = camMountForward;
			cam.transform.localEulerAngles = Vector3.zero;
			cam.transform.localPosition = Vector3.zero;
			camMountForward.localEulerAngles = Vector3.zero;
			cam.fieldOfView = 10f;
			cam.nearClipPlane = 2f;
			cam.farClipPlane = 60000f;
			currentMode = CamMode.targetForward;
			cam.enabled = true;
			this.onCamToggle?.Invoke(new OnCamToggle
			{
				enabled = true,
				camMode = CamMode.targetForward
			});
		}
		camTimeout = 3f;
		GetPositionAndSize(targetList, out targetPosition, out var size);
		targetDist = FastMath.Distance(targetPosition, base.transform.GlobalPosition());
		if (NetworkSceneSingleton<LevelInfo>.i.timeOfDay < 6f || NetworkSceneSingleton<LevelInfo>.i.timeOfDay > 18f || targetDist > 10000f || PlayerSettings.tacScreenIR)
		{
			if (!IRMode)
			{
				SwitchIRState(IR: true);
			}
		}
		else if (IRMode)
		{
			SwitchIRState(IR: false);
		}
		targetFOV = Mathf.Clamp(size * 75f / targetDist, 0.25f, 20f);
		timeOnTarget += Time.deltaTime;
		if (FastMath.OutOfRange(targetPosition, targetPositionPrev, 50f))
		{
			timeOnTarget = 0f;
		}
		targetPositionPrev = targetPosition;
		AimCamera();
	}

	public void SetLandingCam()
	{
		if ((!vtolLandingCam && PlayerSettings.landingCam == 1) || PlayerSettings.landingCam == 0)
		{
			return;
		}
		screenVolume.enabled = true;
		if (canvasObjectTarget != null && canvasObjectTarget.activeSelf)
		{
			canvasObjectTarget.SetActive(value: false);
		}
		if (canvasObjectLanding == null)
		{
			canvasObjectLanding = UnityEngine.Object.Instantiate(GameAssets.i.LandingScreenCanvas, null);
			landingScreenUI = canvasObjectLanding.GetComponent<LandingScreenUI>();
			landingScreenUI.SetupCamera(cam, UICam);
		}
		else if (canvasObjectLanding != null)
		{
			canvasObjectLanding.SetActive(value: true);
		}
		base.enabled = true;
		if (!cam.enabled)
		{
			currentMount = camMountLanding;
			camMountLanding.transform.localEulerAngles = landingCamVector;
			cam.transform.parent = camMountLanding;
			cam.transform.localEulerAngles = Vector3.zero;
			cam.transform.localPosition = Vector3.zero;
			cam.fieldOfView = landingCamFoV;
			cam.nearClipPlane = 0.1f;
			cam.farClipPlane = 30000f;
			currentMode = CamMode.landingMode;
			cam.enabled = true;
			this.onCamToggle?.Invoke(new OnCamToggle
			{
				enabled = true,
				camMode = CamMode.landingMode
			});
		}
		landingScreenUI.SetInfo(10f / cam.fieldOfView, IRMode);
		if (NetworkSceneSingleton<LevelInfo>.i.timeOfDay < 6f || NetworkSceneSingleton<LevelInfo>.i.timeOfDay > 18f || PlayerSettings.tacScreenIR)
		{
			if (!IRMode)
			{
				SwitchIRState(IR: true);
			}
		}
		else if (IRMode)
		{
			SwitchIRState(IR: false);
		}
	}

	private void GetPositionAndSize(List<Unit> targets, out GlobalPosition position, out float size)
	{
		if (targets.Count == 1)
		{
			SingleTargetPositionAndSize(targets, out position, out size);
		}
		else
		{
			MultipleTargetPositionAndSize(targets, out position, out size);
		}
	}

	private void SingleTargetPositionAndSize(List<Unit> targets, out GlobalPosition position, out float size)
	{
		size = targets[0].definition.length;
		position = aircraft.NetworkHQ.GetTrackingData(targets[0].persistentID)?.GetPosition() ?? targets[0].GlobalPosition();
	}

	private void MultipleTargetPositionAndSize(List<Unit> targets, out GlobalPosition position, out float size)
	{
		GetMultipleTargetBounds(targets, out var targetsSouthWest, out var targetsNorthEast);
		position = ((targetsSouthWest + targetsNorthEast) * 0.5f).ToGlobalPosition();
		float a = Mathf.Max(targetsNorthEast.x - targetsSouthWest.x, targetsNorthEast.z - targetsSouthWest.z);
		size = Mathf.Max(a, targetsNorthEast.y - targetsSouthWest.y);
		size *= 0.75f;
	}

	private void GetMultipleTargetBounds(List<Unit> targets, out Vector3 targetsSouthWest, out Vector3 targetsNorthEast)
	{
		targetsSouthWest = Vector3.one * float.MaxValue;
		targetsNorthEast = -Vector3.one * float.MaxValue;
		for (int i = 0; i < targets.Count; i++)
		{
			Vector3 vector = aircraft.NetworkHQ.GetTrackingData(targets[i].persistentID)?.GetPosition().ToLocalPosition() ?? targets[i].transform.position;
			if (!targets[i].disabled)
			{
				targetsSouthWest.x = Mathf.Min(vector.x, targetsSouthWest.x);
				targetsSouthWest.z = Mathf.Min(vector.z, targetsSouthWest.z);
				targetsSouthWest.y = Mathf.Min(vector.y, targetsSouthWest.y);
				targetsNorthEast.x = Mathf.Max(vector.x, targetsNorthEast.x);
				targetsNorthEast.z = Mathf.Max(vector.z, targetsNorthEast.z);
				targetsNorthEast.y = Mathf.Max(vector.y, targetsNorthEast.y);
			}
		}
	}

	public float GetMag()
	{
		return 10f / cam.fieldOfView;
	}

	public float GetDist()
	{
		return targetDist;
	}

	public string GetGrid()
	{
		return SceneSingleton<DynamicMap>.i.gridLabels.GetGridPosition(targetPosition);
	}

	public bool UsingIR()
	{
		return IRMode;
	}

	public Transform GetCamMount()
	{
		return currentMount;
	}

	public void CancelTarget()
	{
		if (cam.enabled)
		{
			cam.enabled = false;
			this.onCamToggle?.Invoke(new OnCamToggle
			{
				enabled = false,
				camMode = CamMode.targetForward
			});
		}
		targetPosition = default(GlobalPosition);
	}

	private void AimCamera()
	{
		if (currentMode == CamMode.landingMode)
		{
			return;
		}
		if (camTimeout < 2.5f)
		{
			cam.fieldOfView = Mathf.Min(cam.fieldOfView + 2f * Time.deltaTime, 20f);
			if (targetScreenUI == null)
			{
				return;
			}
		}
		if (camTimeout <= 0f)
		{
			timeOnTarget = 0f;
			if (cam.enabled)
			{
				cam.enabled = false;
				this.onCamToggle?.Invoke(new OnCamToggle
				{
					enabled = false,
					camMode = CamMode.targetForward
				});
			}
			if (Vector3.Angle(base.transform.forward, attachedPart.transform.forward) < 0.01f)
			{
				cam.enabled = false;
				base.enabled = false;
			}
		}
		else if (timeOnTarget < 1f)
		{
			currentMount.transform.rotation = Quaternion.Slerp(currentMount.transform.rotation, Quaternion.LookRotation(targetPosition - currentMount.transform.GlobalPosition(), Vector3.up), timeOnTarget);
		}
		else
		{
			currentMount.transform.rotation = Quaternion.LookRotation(targetPosition - currentMount.transform.GlobalPosition(), Vector3.up);
		}
	}

	private void Update()
	{
		if (aircraft == null || aircraft.Player == null || !aircraft.Player.IsLocalPlayer)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		if (Time.timeSinceLevelLoad - lastExposureUpdate > 1f)
		{
			UpdateExposure();
		}
		if (currentMode != CamMode.landingMode)
		{
			cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime);
			Vector3 to = FastMath.Direction(aircraft.GlobalPosition(), targetPosition);
			float num = Vector3.Angle(aircraft.transform.forward, to);
			if (num < 135f && currentMode == CamMode.targetRear)
			{
				currentMount = camMountForward;
				currentMode = CamMode.targetForward;
				cam.transform.parent = camMountForward;
				cam.transform.localEulerAngles = Vector3.zero;
				cam.transform.localPosition = Vector3.zero;
				camMountForward.localEulerAngles = Vector3.zero;
				camMountRear.localEulerAngles = new Vector3(0f, 180f, 0f);
			}
			else if (num > 135f && currentMode == CamMode.targetForward)
			{
				currentMount = camMountRear;
				currentMode = CamMode.targetRear;
				cam.transform.parent = camMountRear;
				cam.transform.localEulerAngles = Vector3.zero;
				cam.transform.localPosition = Vector3.zero;
				camMountForward.localEulerAngles = Vector3.zero;
				camMountRear.localEulerAngles = new Vector3(0f, 180f, 0f);
			}
			if (camTimeout < 3f)
			{
				AimCamera();
			}
			camTimeout -= Time.deltaTime;
		}
	}
}
