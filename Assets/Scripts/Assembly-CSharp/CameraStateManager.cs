using System;
using NuclearOption.MissionEditorScripts;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using BL.NO_Patches;

[DefaultExecutionOrder(2)]
public class CameraStateManager : SceneSingleton<CameraStateManager>
{
	public static CameraMode cameraMode;

	public CameraFreeState freeState = new CameraFreeState();

	public CameraOrbitState orbitState = new CameraOrbitState();

	public CameraTVState TVState = new CameraTVState();

	public CameraCockpitState cockpitState = new CameraCockpitState();

	public CameraSelectionState selectionState = new CameraSelectionState();

	public CameraRelativeState relativeState = new CameraRelativeState();

	public CameraEncyclopediaState encyclopediaState = new CameraEncyclopediaState();

	public CameraChaseState chaseState = new CameraChaseState();

	public CameraControlledState controlledState = new CameraControlledState();

	[NonSerialized]
	public Transform cameraPivot;

	public Light nightVisLight;

	public Light Illuminator;

	public Light sunLight;

	public Light moonLight;

	public Vector2 cloudSpeed;

	public float minExposure;

	public float maxExposure;

	public float lightSensitivity;

	private UniversalAdditionalLightData sunLightData;

	private UniversalAdditionalLightData moonLightData;

	public Material atmosphereSky;

	public Material waterSky;

	public GameObject cockpitCam;

	[NonSerialized]
	public Camera mainCamera;

	[NonSerialized]
	public float gForce;

	[NonSerialized]
	public float jerk;

	public string viewStatus = "noTarget";

	public static bool enableMouseLook;

	public float panView;

	public float tiltView;

	private FloatingOrigin floatingOrigin;

	[NonSerialized]
	public float shakeFactor;

	public float shakeThreshold = 5f;

	public Rigidbody followingRB;

	public AudioSource cockpitRattle;

	public AudioSource windNoiseExternal;

	public AudioSource soundEffectSource;

	public Camera cockpitCamRender;

	public Camera selectionCam;

	public GameObject externalViewCenter;

	private Vector3 deathVelocity;

	[HideInInspector]
	public Vector3 deathPosition;

	[HideInInspector]
	public float deathTimer;

	public TrackIRComponent trackIRComponent;

	public Texture2D mapTexture;

	public bool freeCam;

	public Color oceanColor;

	private bool underwater;

	[SerializeField]
	private Renderer sunObject;

	[SerializeField]
	private Renderer moonObject;

	[SerializeField]
	private Volume volume;

	[SerializeField]
	private Image blackoutImage;

	public Unit followingUnit;

	public Unit previousFollowingUnit;

	public float desiredFOV;

	public bool allowInputs = true;

	public float desiredTransSpeed = 1f;

	public float desiredRotSpeed = 1f;

	public float fovChangeSpeed = 0.5f;

	public float fovChangeInertia = 0.5f;

	private int detailUnitIndex;

	private float lastFollowCheck;

	public Vector3 cameraVelocity;

	public CameraBaseState currentState { get; private set; }

	public static event Action<Unit> onFollowingUnitSet;

	public event Action onSwitchCamera;

	public void SwitchState(CameraBaseState state)
	{
		lastFollowCheck = Time.timeSinceLevelLoad;
		if (currentState != null)
		{
			currentState.LeaveState(this);
		}
		currentState = state;
		if (currentState == freeState)
		{
			freeState.DontResetRotationFlag = true;
		}
		desiredFOV = mainCamera.fieldOfView;
		currentState.EnterState(this);
		if (GameManager.GetLocalAircraft(out var localAircraft) && localAircraft == followingUnit)
		{
			SceneSingleton<DynamicMap>.i.Minimize();
		}
		cockpitRattle.Stop();
		this.onSwitchCamera?.Invoke();
		if (currentState == cockpitState)
		{
			AudioMixerVolume.SetEffectsAudioFilterStrength(8000f, 0.7f);
		}
		else
		{
			AudioMixerVolume.SetEffectsAudioFilterStrength(22000f, 1f);
		}
	}

	public Volume GetPostProcessVolume()
	{
		return volume;
	}

	public Image GetBlackoutImage()
	{
		return blackoutImage;
	}

	public void SetFollowingUnit(Unit unit)
	{
		CameraStateManager.onFollowingUnitSet?.Invoke(unit);
		currentState.LeaveState(this);
		if (followingUnit != null)
		{
			followingUnit.SetDoppler(enabled: true);
			previousFollowingUnit = followingUnit;
			followingUnit.onDisableUnit -= Cam_OnFollowingUnitDisabled;
		}
		if (unit != null)
		{
			followingUnit = unit;
			followingUnit.onDisableUnit += Cam_OnFollowingUnitDisabled;
			followingUnit.displayDetail = 1000f;
			if (unit.rb != null && (!(unit is Aircraft aircraft) || !aircraft.HasEjected()))
			{
				followingRB = unit.rb;
				SwitchState(orbitState);
			}
			else
			{
				base.transform.position = unit.transform.position - unit.transform.forward * (unit.maxRadius * 2f) + Vector3.up * unit.definition.height * 0.8f;
				base.transform.LookAt(unit.transform);
				SwitchState(freeState);
			}
		}
		else
		{
			followingUnit = null;
			SwitchState(freeState);
		}
		NetworkSceneSingleton<LevelInfo>.i.UpdateReflectionProbe(forceImmediate: true);
	}

	public void FocusPosition(Vector3 position, Quaternion? rotation, float distance)
	{
		SetFollowingUnit(null);
		previousFollowingUnit = null;
		if (rotation.HasValue)
		{
			Vector3 vector = rotation.Value * Vector3.forward;
			Vector3 position2 = position - vector * distance;
			base.transform.SetPositionAndRotation(position2, rotation.Value);
		}
		else
		{
			base.transform.position = position - base.transform.forward * distance;
		}
		freeState.DontResetRotationFlag = true;
		SwitchState(freeState);
	}

	public void FocusAirbase(Airbase airbase, bool allowMoveToDropFocus, float viewDistance = 20f, float upDistance = 1.75f)
	{
		selectionState.FocusAirbase(this, airbase, allowMoveToDropFocus, viewDistance, upDistance);
	}

	public void SetCameraPosition(PositionRotation positionRotation)
	{
		SetCameraPosition(positionRotation.Position, positionRotation.Rotation);
	}

	public void SetCameraPosition(GlobalPosition position, Quaternion rotation)
	{
		SetFollowingUnit(null);
		previousFollowingUnit = null;
		base.transform.SetPositionAndRotation(position.ToLocalPosition(), rotation);
		freeState.DontResetRotationFlag = true;
		SwitchState(freeState);
	}

	public void GetCameraPosition(out PositionRotation positionRotation)
	{
		GetCameraPosition(out positionRotation.Position, out positionRotation.Rotation);
	}

	public void GetCameraPosition(out GlobalPosition position, out Quaternion rotation)
	{
		base.transform.GetPositionAndRotation(out var position2, out rotation);
		position = position2.ToGlobalPosition();
	}

	private void Cam_OnFollowingUnitDisabled(Unit unit)
	{
		if (GameManager.gameState == GameState.Menu)
		{
			return;
		}
		if (GameManager.GetLocalAircraft(out var localAircraft) && localAircraft == followingUnit)
		{
			if (localAircraft.pilots[0].dead)
			{
				SoundManager.PlayInterfaceOneShot(GameAssets.i.deathSound);
				if (localAircraft.Player != null)
				{
					localAircraft.Player.ShowMap(5f);
				}
			}
		}
		else
		{
			if (SceneSingleton<GameplayUI>.i.hurt != null)
			{
				SceneSingleton<GameplayUI>.i.hurt.color = new Color(0f, 0f, 0f, 0f);
				SceneSingleton<GameplayUI>.i.hurt.gameObject.SetActive(value: false);
			}
			SetFollowingUnit(null);
		}
	}

	private void Start()
	{
		atmosphereSky = UnityEngine.Object.Instantiate(atmosphereSky);
		waterSky = UnityEngine.Object.Instantiate(waterSky);
		RenderSettings.skybox = atmosphereSky;
		SetDesiredSpeed(1f, 1f);
		SetDesiredFoV(PlayerSettings.defaultFoV, PlayerSettings.defaultFoV);
		allowInputs = true;
	}

	private void OnEnable()
	{
		mainCamera = GetComponent<Camera>();
		floatingOrigin = GetComponent<FloatingOrigin>();
		sunLightData = sunLight.gameObject.GetComponent<UniversalAdditionalLightData>();
		moonLightData = moonLight.gameObject.GetComponent<UniversalAdditionalLightData>();
		enableMouseLook = true;
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			currentState = encyclopediaState;
			currentState.EnterState(this);
			return;
		}
		if (currentState == null)
		{
			cameraPivot = new GameObject("cameraPivot").transform;
			currentState = freeState;
			currentState.EnterState(this);
			base.gameObject.AddComponent<ExplosionAudioManager>();
			oceanColor = new Color(0.32f, 0.45f, 0.61f);
		}
		chaseState.Initialize();
	}

	public void SetDesiredFoV(float FOVTarget, float FOVCurrent)
	{
		desiredFOV = FOVTarget;
		if (FOVCurrent != 0f)
		{
			mainCamera.fieldOfView = FOVCurrent;
		}
	}

	public void SetDesiredSpeed(float speed, float rot)
	{
		desiredTransSpeed = speed;
		desiredRotSpeed = rot;
	}

	public void ShakeCamera(float lowFreqShake, float highFreqShake)
	{
		if (currentState == cockpitState)
		{
			cockpitState.AddShake(lowFreqShake, highFreqShake);
		}
	}

	private void FixedUpdate()
	{
		currentState.FixedUpdateState(this);
		SonicBoomManager.ManageSonicBooms(base.transform.GlobalPosition());
	}

	private void Update()
	{
		if (deathTimer > 0f)
		{
			deathTimer -= Time.fixedUnscaledDeltaTime;
			deathPosition += deathVelocity * Time.deltaTime;
		}
		ExposureController.UpdateExposure();
		SetUnitDetail();
		if (!(NetworkSceneSingleton<LevelInfo>.i == null))
		{
			sunLightData.lightCookieOffset += cloudSpeed * Time.deltaTime;
			moonLightData.lightCookieOffset += cloudSpeed * Time.deltaTime;
			Vector3 vector = cameraVelocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition());
			float speedOfSound = LevelInfo.GetSpeedOfSound(base.transform.GlobalPosition().y);
			float airDensity = LevelInfo.GetAirDensity(base.transform.GlobalPosition().y);
			float value = vector.magnitude / speedOfSound;
			value = Mathf.Clamp01(value);
			windNoiseExternal.volume = value * airDensity;
			windNoiseExternal.pitch = value * 0.5f + 0.5f;
			if (GameManager.gameState != GameState.Encyclopedia && !(SceneSingleton<CameraControlUI>.i == null) && SceneSingleton<CameraControlUI>.i.isOpen && !InputFieldChecker.InsideInputField)
			{
				SceneSingleton<CameraControlUI>.i.GetValues();
			}
		}
	}

	private void SetUnitDetail()
	{
		if (UnitRegistry.allUnits.Count != 0)
		{
			detailUnitIndex = ((detailUnitIndex < UnitRegistry.allUnits.Count - 1) ? (detailUnitIndex + 1) : 0);
			Unit unit = UnitRegistry.allUnits[detailUnitIndex];
			if (unit != null && unit != followingUnit)
			{
				unit.displayDetail = 1000f / FastMath.Distance(unit.transform.position, base.transform.position);
			}
		}
	}

	private void FollowCheck()
	{
		if (!(Time.timeSinceLevelLoad - lastFollowCheck < 1f))
		{
			lastFollowCheck = Time.timeSinceLevelLoad;
			if (!GameManager.IsLocalAircraft(followingUnit) && GameManager.GetLocalHQ(out var localHq) && followingUnit.NetworkHQ != null && localHq != followingUnit.NetworkHQ && !localHq.IsTargetPositionAccurate(followingUnit, 100f))
			{
				SetFollowingUnit(null);
			}
		}
	}

	private void LateUpdate()
	{
		if (followingUnit != null)
		{
			FollowCheck();
		}
		currentState.UpdateState(this);
		CheckOriginShift();
		if (NetworkSceneSingleton<LevelInfo>.i != null)
		{
			sunObject.transform.position = base.transform.position - NetworkSceneSingleton<LevelInfo>.i.sun.transform.forward * 40000f;
			moonObject.transform.position = base.transform.position - NetworkSceneSingleton<LevelInfo>.i.moon.transform.forward * 40000f;
		}
		if (!underwater && base.transform.position.y < Datum.LocalSeaY)
		{
			RenderSettings.skybox = waterSky;
			RenderSettings.fogColor = new Color(0f, 0.25f, 0.35f);
			RenderSettings.fogDensity = 0.05f;
			underwater = true;
		}
		if (underwater && base.transform.position.y > Datum.LocalSeaY)
		{
			RenderSettings.skybox = atmosphereSky;
			underwater = false;
		}
	}

	public void CheckOriginShift()
	{
		if (floatingOrigin != null)
		{
			floatingOrigin.OriginShift(base.transform.position);
		}
	}
}
