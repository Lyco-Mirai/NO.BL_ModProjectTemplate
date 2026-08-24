using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mirage;
using Mirage.Serialization;
using NuclearOption.Jobs;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using RoadPathfinding;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LevelInfo : NetworkSceneSingleton<LevelInfo>
{
	[Serializable]
	public class SkyAnimData
	{
		public AnimationCurve ambientIntensity;

		public AnimationCurve directSunlightIntensity;

		public AnimationCurve scattering;

		public Gradient sunColor;

		public Gradient fogColorGround;

		public Gradient fogColorAltitude;

		public float hazeAmount;
	}

	private static readonly int id_EmissionColor = Shader.PropertyToID("_EmissionColor");

	private static readonly int id_macro_basecolor = Shader.PropertyToID("_macro_basecolor");

	private static readonly int id_macro_depth = Shader.PropertyToID("_macro_depth");

	private static readonly int id_size = Shader.PropertyToID("_size");

	private static readonly int id_AmbientIntensity = Shader.PropertyToID("_AmbientIntensity");

	private static readonly int id_SunColor = Shader.PropertyToID("_SunColor");

	private static readonly int id_FogColor = Shader.PropertyToID("_FogColor");

	private static readonly int id_ScatterIntensity = Shader.PropertyToID("_ScatterIntensity");

	private static readonly int id_SunDirection = Shader.PropertyToID("_SunDirection");

	private static readonly int id_CloudOcclusion = Shader.PropertyToID("_CloudOcclusion");

	private static readonly int id_Altitude = Shader.PropertyToID("_Altitude");

	private static readonly int id_CameraPosition = Shader.PropertyToID("_CameraPosition");

	private static readonly int id_OriginOffset = Shader.PropertyToID("_OriginOffset");

	private static readonly int id_Conditions = Shader.PropertyToID("_Conditions");

	public float mapSize;

	public float hashGridSize;

	public GameObject worldAxis;

	public GameObject starfield;

	public GameObject moonAxis;

	public GameObject ocean;

	public GameObject sunObject;

	public GameObject moonObject;

	public Light sun;

	public Light moon;

	public Light nightVisionIlluminator;

	[SyncVar]
	public float timeOfDay;

	[SyncVar]
	public float conditions;

	[SyncVar]
	public float cloudHeight = 1800f;

	[SyncVar]
	public Vector3 windVelocity;

	[SyncVar]
	public float windTurbulence;

	[SyncVar]
	public float windSpeed;

	[SyncVar]
	public float? moonPhase;

	private float ambientIntensity;

	[SerializeField]
	private WindZone windZone;

	[SerializeField]
	private CloudLayer cloudLayer;

	[SerializeField]
	private ReflectionProbe reflectionProbe;

	[SerializeField]
	private SkyAnimData skyAnimData;

	[SerializeField]
	private Material[] lightScatteringParticles;

	[SerializeField]
	private Material waterMaterial;

	private Color sunColor;

	[NonSerialized]
	private Color[] lightScatteringParticlesStartingColor;

	private Material sunMat;

	private Material moonMat;

	public Volume PostProcessing;

	public Bloom bloom;

	[SerializeField]
	private Transform waterPlane;

	private Vector3 windOffset;

	public static NativeArray<float> airDensityChart;

	private float reflectionLastUpdated;

	private float skyLastUpdated;

	private float lightingLastUpdated;

	[SerializeField]
	private GameObject pathfindingSegmentVis;

	[SerializeField]
	private List<GameObject> pathfindingVisualizations;

	public UniversalAdditionalLightData SunURPLightData;

	public UniversalAdditionalLightData MoonURPLightData;

	private float timeFactor;

	private float windMainHeading;

	private float windRandomArc;

	private float lastWindChange;

	private float windChangeDelay = 120f;

	public bool isDayLight;

	private float daylightLastUpdate;

	private float daylightUpdateRate = 1f;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 7;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	public MapSettings LoadedMapSettings { get; private set; }

	public RoadNetwork roadNetwork { get; private set; }

	public RoadNetwork seaLanes { get; private set; }

	public float NetworktimeOfDay
	{
		get
		{
			return timeOfDay;
		}
		set
		{
			if (!SyncVarEqual(value, timeOfDay))
			{
				float num = timeOfDay;
				timeOfDay = value;
				SetDirtyBit(1uL);
			}
		}
	}

	public float Networkconditions
	{
		get
		{
			return conditions;
		}
		set
		{
			if (!SyncVarEqual(value, conditions))
			{
				float num = conditions;
				conditions = value;
				SetDirtyBit(2uL);
			}
		}
	}

	public float NetworkcloudHeight
	{
		get
		{
			return cloudHeight;
		}
		set
		{
			if (!SyncVarEqual(value, cloudHeight))
			{
				float num = cloudHeight;
				cloudHeight = value;
				SetDirtyBit(4uL);
			}
		}
	}

	public Vector3 NetworkwindVelocity
	{
		get
		{
			return windVelocity;
		}
		set
		{
			if (!SyncVarEqual(value, windVelocity))
			{
				Vector3 vector = windVelocity;
				windVelocity = value;
				SetDirtyBit(8uL);
			}
		}
	}

	public float NetworkwindTurbulence
	{
		get
		{
			return windTurbulence;
		}
		set
		{
			if (!SyncVarEqual(value, windTurbulence))
			{
				float num = windTurbulence;
				windTurbulence = value;
				SetDirtyBit(16uL);
			}
		}
	}

	public float NetworkwindSpeed
	{
		get
		{
			return windSpeed;
		}
		set
		{
			if (!SyncVarEqual(value, windSpeed))
			{
				float num = windSpeed;
				windSpeed = value;
				SetDirtyBit(32uL);
			}
		}
	}

	public float? NetworkmoonPhase
	{
		get
		{
			return moonPhase;
		}
		set
		{
			if (!SyncVarEqual(value, moonPhase))
			{
				float? num = moonPhase;
				moonPhase = value;
				SetDirtyBit(64uL);
			}
		}
	}

	public event Action onDaylightChange;

	protected override void Awake()
	{
		base.Awake();
		BattlefieldGrid.GenerateGrid(mapSize, hashGridSize);
		GenerateAirDensityChart();
		if (PostProcessing.profile.TryGet<ColorAdjustments>(out var component))
		{
			ExposureController.SetColorAdjustments(component);
		}
		PostProcessing.profile.TryGet<Bloom>(out bloom);
		MeshRenderer component2 = sunObject.GetComponent<MeshRenderer>();
		sunMat = MaterialHelper.CloneMaterial(component2);
		MeshRenderer component3 = moonObject.GetComponent<MeshRenderer>();
		moonMat = MaterialHelper.CloneMaterial(component3);
		lightScatteringParticlesStartingColor = lightScatteringParticles.Select((Material x) => x.GetColor(id_EmissionColor)).ToArray();
		ExposureController.RegisterBrightLight(sun);
	}

	public void ApplyMapSettings(MapSettings mapSettings)
	{
		ColorLog<LevelInfo>.Info("ApplyMapSettings: " + mapSettings.name);
		LoadedMapSettings = mapSettings;
		mapSize = mapSettings.MapSize.x;
		worldAxis.transform.localEulerAngles = new Vector3(0f - mapSettings.Latitude, worldAxis.transform.localEulerAngles.y, worldAxis.transform.localEulerAngles.z);
		roadNetwork = mapSettings.CreateRoadNetwork();
		seaLanes = mapSettings.CreateSeaLanes();
		roadNetwork.RegenerateNetwork();
		seaLanes.RegenerateNetwork();
		waterMaterial.SetTexture(id_macro_basecolor, mapSettings.OceanBasecolor);
		waterMaterial.SetTexture(id_macro_depth, mapSettings.OceanDepthmap);
		waterMaterial.SetVector(id_size, new Vector2(mapSettings.MapSize.x, mapSettings.MapSize.y));
		if (mapSettings.ReflectionProbePoint != null)
		{
			reflectionProbe.transform.SetParent(mapSettings.ReflectionProbePoint);
			reflectionProbe.transform.localPosition = Vector3.zero;
			mapSettings.BeforeDestroy += delegate
			{
				if (reflectionProbe.transform.IsChildOf(mapSettings.transform))
				{
					reflectionProbe.transform.SetParent(null);
					reflectionProbe.transform.localPosition = Vector3.zero;
				}
			};
		}
		DynamicMap.LoadMapImage(mapSettings);
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			LoadFromMission(MissionManager.CurrentMission);
		}
	}

	private Camera GetCurrentCam()
	{
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			return SceneSingleton<CameraStateManager>.i.mainCamera;
		}
		return Camera.main;
	}

	private static void GenerateAirDensityChart()
	{
		ColorLog<LevelInfo>.Info("GenerateAirDensityChart");
		if (airDensityChart.IsCreated)
		{
			airDensityChart.Dispose();
		}
		airDensityChart = new NativeArray<float>(64, Allocator.Persistent);
		for (int i = 0; i < 64; i++)
		{
			airDensityChart[i] = GameAssets.i.airDensityAltitude.Evaluate((float)i * 0.47619f);
		}
	}

	public static float GetAirDensity(float altitude)
	{
		return ChartHelper.SafeRead(altitude * 0.0021f, airDensityChart.AsReadOnlySpan());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetSpeedOfSound(float altitude)
	{
		return Mathf.Max(-0.005f * altitude + 340f, 290f);
	}

	private void OnDestroy()
	{
		BattlefieldGrid.Clear();
		for (int i = 0; i < lightScatteringParticles.Length; i++)
		{
			lightScatteringParticles[i].SetColor(value: lightScatteringParticlesStartingColor[i], nameID: id_EmissionColor);
		}
	}

	public float GetAmbientLight()
	{
		return (ambientIntensity + moon.intensity * 0.1f) * (1f - conditions * 0.5f);
	}

	public Vector3 GetWind()
	{
		return windVelocity;
	}

	public float GetTurbulence()
	{
		return windTurbulence;
	}

	public Vector3 GetWind(GlobalPosition globalPosition)
	{
		globalPosition += windOffset;
		Vector3 vector = (-0.75f + Mathf.PerlinNoise1D(globalPosition.z * 0.02f) + 0.5f * Mathf.PerlinNoise1D(globalPosition.z * 0.1f)) * windZone.transform.forward + 0.5f * (-0.75f + Mathf.PerlinNoise1D(globalPosition.x * 0.02f) + 0.5f * Mathf.PerlinNoise1D(globalPosition.x * 0.1f)) * windZone.transform.right + 0.3f * (-0.75f + Mathf.PerlinNoise1D(globalPosition.y * 0.02f) + 0.5f * Mathf.PerlinNoise1D(globalPosition.y * 0.1f)) * Vector3.up;
		return windVelocity + windTurbulence * Mathf.Max(windSpeed, 10f) * vector;
	}

	private void UpdateTimeOfDayLighting(bool forceImmediate)
	{
		if (!forceImmediate && Time.realtimeSinceStartup - lightingLastUpdated < 1f)
		{
			return;
		}
		Camera currentCam = GetCurrentCam();
		if (currentCam == null)
		{
			return;
		}
		float num = currentCam.transform.position.GlobalY();
		if (!(num < 0f))
		{
			lightingLastUpdated = Time.realtimeSinceStartup;
			worldAxis.transform.eulerAngles = new Vector3(worldAxis.transform.eulerAngles.x, worldAxis.transform.eulerAngles.y, timeOfDay * 15f + 180f);
			float num2 = GameAssets.i.airDensityAltitude.Evaluate(num * 0.001f);
			float num3 = 0f;
			ambientIntensity = skyAnimData.ambientIntensity.Evaluate(timeOfDay);
			Color color = Color.Lerp(skyAnimData.fogColorGround.Evaluate(timeOfDay / 24f), skyAnimData.fogColorAltitude.Evaluate(timeOfDay / 24f), (currentCam.transform.position.y - cloudLayer.transform.position.y) * 0.0002f);
			color *= Mathf.Lerp(1.2f, 1.6f, num2 * 4f);
			Color color2 = GameAssets.i.skyColor.Evaluate(timeOfDay / 24f);
			RenderSettings.reflectionIntensity = Mathf.Clamp01(ambientIntensity);
			float num4 = 0f;
			if (conditions > 0.6f)
			{
				num3 = Mathf.Clamp((cloudLayer.transform.position.y + 200f - currentCam.transform.position.y) * 0.005f, 0f, 1f);
				num4 = num3 * Mathf.InverseLerp(0.8f, 1f, conditions);
				color *= 1f - num3 * 0.25f;
				float num5 = Mathf.Max(color.r, 0.07f);
				color = Color.Lerp(color, new Color(num5, num5, num5), num3 * conditions + num4);
				color2 = Color.Lerp(color2, new Color(color2.r * 0.75f, color2.r * 0.75f, color2.r * 0.75f), num3 * conditions + num4);
			}
			RenderSettings.ambientEquatorColor = GameAssets.i.horizonColor.Evaluate(timeOfDay / 24f) * (1f + moon.intensity);
			RenderSettings.ambientGroundColor = GameAssets.i.groundColor.Evaluate(timeOfDay / 24f) * (1f + moon.intensity);
			RenderSettings.ambientSkyColor = color2 * 1.3f + moon.color * 1.5f * moon.intensity * Mathf.Clamp01((0.05f - ambientIntensity) * 40f);
			RenderSettings.fogColor = color;
			RenderSettings.ambientIntensity = ambientIntensity + 0.5f * moon.intensity;
			RenderSettings.skybox.SetFloat(id_AmbientIntensity, ambientIntensity * num2);
			sunColor = skyAnimData.sunColor.Evaluate(timeOfDay / 24f) * 300f;
			RenderSettings.skybox.SetColor(id_SunColor, sunColor);
			RenderSettings.fogDensity = Mathf.Max(num2 * 0.00014f * skyAnimData.hazeAmount + num4 * 0.001f, 3E-05f);
			color *= Mathf.Lerp(0.92f, 1f, num2);
			RenderSettings.skybox.SetFloat(id_Conditions, conditions);
			RenderSettings.skybox.SetColor(id_FogColor, color);
			RenderSettings.skybox.SetFloat(id_ScatterIntensity, skyAnimData.scattering.Evaluate(timeOfDay) * num2 * (1f + moon.intensity));
			RenderSettings.skybox.SetVector(id_SunDirection, new Vector4(sun.transform.forward.x, sun.transform.forward.y, sun.transform.forward.z, 0f));
			RenderSettings.skybox.SetFloat(id_CloudOcclusion, num3 * num3);
			RenderSettings.skybox.SetFloat(id_Altitude, num * 7E-06f);
			sun.color = skyAnimData.sunColor.Evaluate(timeOfDay / 24f) * skyAnimData.directSunlightIntensity.Evaluate(timeOfDay / 24f);
			sun.gameObject.SetActive(sun.color.r > 0f && num3 < 0.9f);
			sunObject.SetActive(sun.gameObject.activeSelf);
			float num6 = 1f - Mathf.Abs(Vector3.Dot(sun.transform.forward, Vector3.up));
			sunMat.SetColor(id_EmissionColor, sun.color * 1000000f * Mathf.Clamp01(1f - num3 * 2f - num6));
			if (ambientIntensity > 0.03f)
			{
				bloom.threshold.value = 1.3f;
				bloom.intensity.value = 0.5f;
			}
			else
			{
				bloom.threshold.value = 0.3f;
				bloom.intensity.value = 3f;
			}
			if (moonPhase.HasValue)
			{
				bool flag = Vector3.Dot(moon.transform.position - currentCam.transform.position, Vector3.up) > 0f;
				float num7 = Mathf.Clamp01(1f - num3);
				float num8 = 1f - Mathf.Abs(moonPhase.Value - 14f) / 14f;
				float num9 = 0.5f * num7 * num8 * (float)(flag ? 1 : 0);
				moon.intensity = (LightVisible(sun) ? 0f : num9);
				moonMat.SetVector("_SunVector", -sun.transform.forward);
			}
		}
	}

	public void UpdateFogDensity(float cloudFog)
	{
	}

	public bool TryGetTerrainColorAtCoordinate(GlobalPosition globalPosition, out Color sampledColor)
	{
		sampledColor = Color.clear;
		if (LoadedMapSettings == null)
		{
			return false;
		}
		sampledColor = LoadedMapSettings.GetTerrainColorAtCoordinate(globalPosition);
		return true;
	}

	public Color GetSunColor()
	{
		return sunColor;
	}

	public float GetDaylightFactor(Vector3 position)
	{
		float num = 1f;
		if (timeOfDay > 18f)
		{
			num -= (timeOfDay - 18f) * 2f;
		}
		else if (timeOfDay < 6f)
		{
			num -= (6f - timeOfDay) * 2f;
		}
		float num2 = Mathf.Clamp01(1f - GetCloudOcclusion(position));
		return Mathf.Clamp01(num) * num2;
	}

	public float GetCloudOcclusion(Vector3 position)
	{
		return Mathf.InverseLerp(0.5f, 0.7f, conditions) * Mathf.Clamp01((cloudLayer.transform.position.y + 200f - position.y) * 0.005f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool LightVisible(Light light)
	{
		if (light.isActiveAndEnabled)
		{
			return light.intensity > 0f;
		}
		return false;
	}

	public void SetCookie(Texture2D texture, float scale, Vector2 windDisplacement)
	{
		sun.transform.rotation = Quaternion.LookRotation(sun.transform.forward, Vector3.up);
		bool flag = LightVisible(sun);
		sun.cookie = (flag ? texture : null);
		moon.cookie = ((!flag && LightVisible(moon)) ? texture : null);
		SunURPLightData.lightCookieOffset = windDisplacement;
		SunURPLightData.lightCookieSize = new Vector2(scale, scale);
		MoonURPLightData.lightCookieOffset = windDisplacement;
		MoonURPLightData.lightCookieSize = new Vector2(scale, scale);
	}

	public void VisualizeWaypoints(List<GlobalPosition> waypoints, Unit unit)
	{
		foreach (GameObject pathfindingVisualization in pathfindingVisualizations)
		{
			UnityEngine.Object.Destroy(pathfindingVisualization);
		}
		pathfindingVisualizations.Clear();
		float num = 0f;
		for (int i = 0; i < waypoints.Count - 1; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(pathfindingSegmentVis, Datum.origin);
			gameObject.transform.localPosition = waypoints[i].AsVector3();
			gameObject.transform.rotation = Quaternion.LookRotation(waypoints[i + 1] - waypoints[i]);
			float num2 = FastMath.Distance(waypoints[i + 1], waypoints[i]);
			num += num2;
			gameObject.transform.localScale = new Vector3(unit.maxRadius * 2f, 1f, num2 - 1f);
			pathfindingVisualizations.Add(gameObject);
			if (num2 < 0.1f)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(GameAssets.i.debugArrow, Datum.origin);
				gameObject2.transform.position = gameObject.transform.position;
				gameObject2.transform.localScale = new Vector3(50f, 50f, 100f);
				gameObject2.transform.rotation = Quaternion.LookRotation(Vector3.up);
				pathfindingVisualizations.Add(gameObject2);
			}
		}
		Debug.Log("Road Distance: " + UnitConverter.DistanceReading(num));
	}

	public void SetTimeOfDay(float timeOfDay)
	{
		NetworktimeOfDay = timeOfDay;
		UpdateTimeOfDayLighting(forceImmediate: true);
		UpdateSkybox(forceImmediate: true);
		reflectionProbe.RenderProbe();
	}

	public void LoadFromMission(Mission mission)
	{
		LoadEnvironment(mission?.environment ?? new MissionEnvironment());
		SetStartingCamera(mission);
		if (mission != null && GameManager.gameState != GameState.Editor)
		{
			roadNetwork.Merge(mission.missionSettings.missionRoads);
			seaLanes.Merge(mission.missionSettings.missionSeaLanes);
		}
	}

	public void SetStartingCamera(Mission mission)
	{
		if (!GameManager.IsPlayingFromEditor && SceneSingleton<CameraStateManager>.i.currentState is CameraFreeState)
		{
			PositionRotation cameraPosition = ((mission == null || !mission.missionSettings.cameraStartPosition.IsOverride) ? LoadedMapSettings.CameraPositionRotation : mission.missionSettings.cameraStartPosition.Value);
			SceneSingleton<CameraStateManager>.i.SetCameraPosition(cameraPosition);
			SceneSingleton<CameraStateManager>.i.CheckOriginShift();
		}
	}

	public void LoadEnvironment(MissionEnvironment environment)
	{
		SetTimeOfDay(environment.timeOfDay);
		Networkconditions = environment.weatherIntensity;
		NetworkcloudHeight = environment.cloudAltitude;
		SetWindSpeed(environment.windSpeed);
		SetWindTurbulence(environment.windTurbulence);
		SetWindHeading(environment.windHeading);
		float num = environment.timeFactor;
		if (num <= 0f)
		{
			if (num != -2f)
			{
				if (num != -1f)
				{
					if (num != 0f)
					{
						goto IL_00dc;
					}
					timeFactor = 1f;
				}
				else
				{
					timeFactor = 0.5f;
				}
			}
			else
			{
				timeFactor = 0f;
			}
		}
		else if (num != 1f)
		{
			if (num != 2f)
			{
				if (num != 3f)
				{
					goto IL_00dc;
				}
				timeFactor = 60f;
			}
			else
			{
				timeFactor = 30f;
			}
		}
		else
		{
			timeFactor = 10f;
		}
		goto IL_00e7;
		IL_00dc:
		timeFactor = 1f;
		goto IL_00e7;
		IL_00e7:
		windRandomArc = environment.windRandomHeading;
		NetworkmoonPhase = environment.moonPhaseNullable;
		if (moonPhase.HasValue)
		{
			SetMoonPhase(moonPhase.Value);
		}
	}

	public void SetMoonPhase(float value)
	{
		moonAxis.transform.localEulerAngles = new Vector3(-5f, 0f, 180f - value * 12.86f);
		if (GameManager.gameState == GameState.Editor)
		{
			UpdateTimeOfDayLighting(forceImmediate: true);
		}
	}

	public void SetWindSpeed(float speed)
	{
		NetworkwindSpeed = speed;
		if (windSpeed == 0f)
		{
			NetworkwindSpeed = 0.1f;
		}
		UpdateWindVelocity();
	}

	public void SetWindTurbulence(float turbulence)
	{
		NetworkwindTurbulence = turbulence;
		windZone.windPulseMagnitude = turbulence;
		windZone.windPulseFrequency = 0.2f;
	}

	public void SetWindHeading(float heading)
	{
		windMainHeading = heading;
		UpdateWindHeading(heading);
	}

	public float GetWindHeading()
	{
		return windZone.transform.localEulerAngles.y;
	}

	private void UpdateWindHeading(float heading)
	{
		windZone.transform.localEulerAngles = new Vector3(0f, heading, 0f);
		UpdateWindVelocity();
	}

	private void UpdateWindVelocity()
	{
		NetworkwindVelocity = windZone.transform.forward * windSpeed;
		windZone.windMain = windVelocity.magnitude;
	}

	public void UpdateSkybox(bool forceImmediate)
	{
		if (forceImmediate || !(Time.realtimeSinceStartup - skyLastUpdated < 1f))
		{
			skyLastUpdated = Time.realtimeSinceStartup;
		}
	}

	private void UpdateWind()
	{
		float num = UnityEngine.Random.Range(0f - windRandomArc, windRandomArc);
		windMainHeading += num;
	}

	private void UpdateWaterPlane(Camera mainCamera)
	{
		if (!(waterPlane == null))
		{
			Vector3 vector = Datum.origin.position - mainCamera.transform.position;
			waterMaterial.SetVector(id_OriginOffset, new Vector2(vector.x, vector.z));
			waterPlane.transform.position = new Vector3(mainCamera.transform.position.x, Datum.LocalSeaY, mainCamera.transform.position.z);
		}
	}

	public void UpdateReflectionProbe(bool forceImmediate)
	{
		if (forceImmediate || !(Time.realtimeSinceStartup - reflectionLastUpdated < 5f))
		{
			reflectionLastUpdated = Time.realtimeSinceStartup;
			if (GetCurrentCam().transform.position.y > Datum.LocalSeaY && !ExposureController.BrightLightsExist())
			{
				reflectionProbe.RenderProbe();
			}
			Material[] array = lightScatteringParticles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetColor(id_EmissionColor, Color.white * 2f * GetAmbientLight());
			}
		}
	}

	private void DaylightCheck()
	{
		if (Time.realtimeSinceStartup - daylightLastUpdate > daylightUpdateRate)
		{
			daylightLastUpdate = Time.realtimeSinceStartup;
			bool flag = ((timeOfDay < 12f) ? (timeOfDay - 6f) : (18f - timeOfDay)) - conditions * 3f > 0f;
			if (flag != isDayLight)
			{
				isDayLight = flag;
				this.onDaylightChange?.Invoke();
			}
		}
	}

	private void Update()
	{
		windOffset = 10f * Time.timeSinceLevelLoad * Vector3.one;
		if (GameManager.ShowEffects)
		{
			UpdateVisuals();
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			UpdateSimulation();
		}
	}

	private void UpdateVisuals()
	{
		Camera main = Camera.main;
		bool num = main != null;
		Vector3 position = (num ? main.transform.position : new Vector3(0f, Datum.LocalSeaY, 0f));
		GlobalPosition globalPosition = position.ToGlobalPosition();
		Material skybox = RenderSettings.skybox;
		if (skybox != null)
		{
			skybox.SetVector(id_CameraPosition, new Vector4(globalPosition.x, globalPosition.y, globalPosition.z, 0f));
		}
		UpdateSkybox(forceImmediate: false);
		if (num)
		{
			UpdateWaterPlane(main);
		}
		starfield.transform.position = position;
		if (moonPhase.HasValue)
		{
			SetMoonPhase(moonPhase.Value);
		}
		UpdateTimeOfDayLighting(forceImmediate: false);
		UpdateReflectionProbe(forceImmediate: false);
		DaylightCheck();
	}

	private void UpdateSimulation()
	{
		NetworktimeOfDay = timeOfDay + timeFactor * Time.deltaTime * 0.00027777778f;
		if (timeOfDay > 24f)
		{
			NetworktimeOfDay = timeOfDay - 24f;
		}
		if (moonPhase.HasValue)
		{
			float value = moonPhase.Value;
			value += timeFactor * Time.deltaTime * 1.1574074E-05f;
			if (value > 28f)
			{
				value -= 28f;
			}
			NetworkmoonPhase = value;
		}
		if (windRandomArc > 0f)
		{
			if (Time.realtimeSinceStartup > lastWindChange + windChangeDelay)
			{
				lastWindChange = Time.realtimeSinceStartup;
				UpdateWind();
			}
			float windHeading = GetWindHeading();
			windHeading = Mathf.LerpAngle(windHeading, windMainHeading, Time.deltaTime);
			UpdateWindHeading(windHeading);
		}
	}

	private void MirageProcessed()
	{
	}

	public override bool SerializeSyncVars(NetworkWriter writer, bool initialize)
	{
		ulong syncVarDirtyBits = base.SyncVarDirtyBits;
		bool result = base.SerializeSyncVars(writer, initialize);
		if (initialize)
		{
			writer.WriteSingleConverter(timeOfDay);
			writer.WriteSingleConverter(conditions);
			writer.WriteSingleConverter(cloudHeight);
			writer.WriteVector3(windVelocity);
			writer.WriteSingleConverter(windTurbulence);
			writer.WriteSingleConverter(windSpeed);
			GeneratedNetworkCode._Write_System_002ENullable_00601_003CSystem_002ESingle_003E(writer, moonPhase);
			return true;
		}
		writer.Write(syncVarDirtyBits, 7);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteSingleConverter(timeOfDay);
			result = true;
		}
		if ((syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteSingleConverter(conditions);
			result = true;
		}
		if ((syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteSingleConverter(cloudHeight);
			result = true;
		}
		if ((syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteVector3(windVelocity);
			result = true;
		}
		if ((syncVarDirtyBits & 0x10L) != 0L)
		{
			writer.WriteSingleConverter(windTurbulence);
			result = true;
		}
		if ((syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteSingleConverter(windSpeed);
			result = true;
		}
		if ((syncVarDirtyBits & 0x40L) != 0L)
		{
			GeneratedNetworkCode._Write_System_002ENullable_00601_003CSystem_002ESingle_003E(writer, moonPhase);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			timeOfDay = reader.ReadSingleConverter();
			conditions = reader.ReadSingleConverter();
			cloudHeight = reader.ReadSingleConverter();
			windVelocity = reader.ReadVector3();
			windTurbulence = reader.ReadSingleConverter();
			windSpeed = reader.ReadSingleConverter();
			moonPhase = GeneratedNetworkCode._Read_System_002ENullable_00601_003CSystem_002ESingle_003E(reader);
			return;
		}
		ulong num = reader.Read(7);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			timeOfDay = reader.ReadSingleConverter();
		}
		if ((num & 2L) != 0L)
		{
			conditions = reader.ReadSingleConverter();
		}
		if ((num & 4L) != 0L)
		{
			cloudHeight = reader.ReadSingleConverter();
		}
		if ((num & 8L) != 0L)
		{
			windVelocity = reader.ReadVector3();
		}
		if ((num & 0x10L) != 0L)
		{
			windTurbulence = reader.ReadSingleConverter();
		}
		if ((num & 0x20L) != 0L)
		{
			windSpeed = reader.ReadSingleConverter();
		}
		if ((num & 0x40L) != 0L)
		{
			moonPhase = GeneratedNetworkCode._Read_System_002ENullable_00601_003CSystem_002ESingle_003E(reader);
		}
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
