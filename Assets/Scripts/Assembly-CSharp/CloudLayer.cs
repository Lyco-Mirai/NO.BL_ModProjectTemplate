using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CloudLayer : MonoBehaviour
{
	[SerializeField]
	private WeatherSet[] weatherSets;

	private WeatherSet currentWeatherSet;

	[SerializeField]
	private ParticleSystem cloudSystem;

	[SerializeField]
	private ParticleSystem distantCloudSystem;

	[SerializeField]
	private ParticleSystem flyThroughSystem;

	[SerializeField]
	private MeshRenderer cloudRenderer;

	[SerializeField]
	private float densityMapScale;

	[SerializeField]
	private float layerHeight;

	[SerializeField]
	private float layerThickness;

	[SerializeField]
	private float cloudSizeMin;

	[SerializeField]
	private float cloudSizeMax;

	[SerializeField]
	private float cloudSizeVariation;

	[SerializeField]
	private float cloudLifeMin;

	[SerializeField]
	private float cloudLifeMax;

	[SerializeField]
	private float cloudDrawDist;

	[SerializeField]
	private int generationRate;

	[SerializeField]
	private int maxParticles;

	[SerializeField]
	private Lightning lightning;

	private Material layerMaterial;

	private Material cloudMaterial;

	private Material distantCloudMaterial;

	private Material flyThroughMaterial;

	private Vector3 samplePoint;

	private ParticleSystem.EmitParams emitParams;

	private ParticleSystem.MainModule main;

	private ParticleSystem.MainModule distantMain;

	private ParticleSystem.EmissionModule flyThroughEmit;

	private ParticleSystem.MainModule flyThroughMain;

	private ParticleSystem.ShapeModule flyThroughShape;

	private float spawnOpacity;

	private float cloudDetail = -1f;

	private float sizeFactor = 1f;

	private Vector2 windDisplacement;

	private Vector3 camVelPrev;

	private static int id_cloudPattern = Shader.PropertyToID("_cloudPattern");

	private static int id_ScatterColor = Shader.PropertyToID("_ScatterColor");

	private static int id_SunDirection = Shader.PropertyToID("_SunDirection");

	private static int id_CloudsOriginOffset = Shader.PropertyToID("_CloudsOriginOffset");

	private void Start()
	{
		main = cloudSystem.main;
		main.simulationSpace = ParticleSystemSimulationSpace.Custom;
		main.customSimulationSpace = Datum.origin;
		flyThroughMain = flyThroughSystem.main;
		flyThroughEmit = flyThroughSystem.emission;
		flyThroughShape = flyThroughSystem.shape;
		flyThroughMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
		flyThroughMain.customSimulationSpace = Datum.origin;
		distantMain = distantCloudSystem.main;
		distantMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
		distantMain.customSimulationSpace = Datum.origin;
		if (GameManager.gameState == GameState.Editor)
		{
			main.useUnscaledTime = true;
			distantMain.useUnscaledTime = true;
			flyThroughMain.useUnscaledTime = true;
		}
		else
		{
			main.useUnscaledTime = false;
			distantMain.useUnscaledTime = false;
			flyThroughMain.useUnscaledTime = false;
		}
		layerMaterial = MaterialHelper.CloneMaterial(cloudRenderer);
		cloudMaterial = cloudSystem.GetComponent<ParticleSystemRenderer>().material;
		distantCloudMaterial = distantCloudSystem.GetComponent<ParticleSystemRenderer>().material;
		flyThroughMaterial = flyThroughSystem.GetComponent<ParticleSystemRenderer>().material;
		UpdateWeatherSets();
		if (!GameManager.IsHeadless)
		{
			WeatherSlowUpdate().Forget();
		}
	}

	public float GetCloudOcclusion()
	{
		if (SceneSingleton<CameraStateManager>.i.transform.position.y > base.transform.position.y)
		{
			return 0f;
		}
		return currentWeatherSet.coverage;
	}

	private async UniTask WeatherSlowUpdate()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(1000, ignoreTimeScale: true);
		while (!cancel.IsCancellationRequested)
		{
			UpdateWeatherSets();
			await UniTask.Delay(1000, ignoreTimeScale: true);
		}
	}

	private void UpdateWeatherSets()
	{
		int num = Mathf.Clamp(Mathf.FloorToInt(NetworkSceneSingleton<LevelInfo>.i.conditions * (float)weatherSets.Length), 0, weatherSets.Length - 1);
		currentWeatherSet = weatherSets[num];
		if (currentWeatherSet != null)
		{
			Transform transform;
			if (GameManager.gameState == GameState.Encyclopedia)
			{
				transform = Camera.main.transform;
			}
			else
			{
				if (!(SceneSingleton<CameraStateManager>.i != null))
				{
					return;
				}
				transform = SceneSingleton<CameraStateManager>.i.transform;
			}
			layerMaterial.SetTexture(id_cloudPattern, currentWeatherSet.mask);
			Color sunColor = NetworkSceneSingleton<LevelInfo>.i.GetSunColor();
			sunColor *= NetworkSceneSingleton<LevelInfo>.i.GetDaylightFactor(transform.position);
			cloudMaterial.SetColor(id_ScatterColor, sunColor);
			distantCloudMaterial.SetColor(id_ScatterColor, sunColor);
			flyThroughMaterial.SetColor(id_ScatterColor, sunColor);
			Vector3 forward = NetworkSceneSingleton<LevelInfo>.i.sun.transform.forward;
			cloudMaterial.SetVector(id_SunDirection, forward);
			distantCloudMaterial.SetVector(id_SunDirection, forward);
			flyThroughMaterial.SetVector(id_SunDirection, forward);
			_ = base.transform.position;
			_ = transform.position;
			distantCloudSystem.gameObject.SetActive(currentWeatherSet.coverage > 0.1f);
			sizeFactor = NetworkSceneSingleton<LevelInfo>.i.conditions * 1.8f;
			distantMain.startSize = new ParticleSystem.MinMaxCurve(1200f * sizeFactor, 1800f * sizeFactor);
		}
		if (currentWeatherSet.lightning && !lightning.gameObject.activeSelf)
		{
			lightning.gameObject.SetActive(value: true);
		}
		if (!currentWeatherSet.lightning && lightning.gameObject.activeSelf)
		{
			lightning.gameObject.SetActive(value: false);
		}
		if (cloudDetail != Mathf.Clamp(PlayerSettings.graphics.CloudDetail, 0.1f, 1f))
		{
			cloudDetail = Mathf.Clamp(PlayerSettings.graphics.CloudDetail, 0.1f, 1f);
			cloudSystem.gameObject.GetComponent<ParticleSystemRenderer>().material.SetFloat("_OpacityBoost", 1f - PlayerSettings.graphics.CloudDetail);
			main.maxParticles = (int)((float)maxParticles * Mathf.Sqrt(cloudDetail));
		}
	}

	private void Update()
	{
		if (GameManager.IsHeadless || SceneSingleton<CameraStateManager>.i == null)
		{
			return;
		}
		Transform transform = SceneSingleton<CameraStateManager>.i.transform;
		layerHeight = NetworkSceneSingleton<LevelInfo>.i.cloudHeight;
		int value = Mathf.FloorToInt((transform.position.y - (base.transform.position.y + 200f)) * (float)currentWeatherSet.cookies.Length / 200f);
		value = Mathf.Clamp(value, 0, currentWeatherSet.cookies.Length - 1);
		Texture2D texture = currentWeatherSet.cookies[value];
		Texture2D texture2D = currentWeatherSet.cookies[0];
		NetworkSceneSingleton<LevelInfo>.i.SetCookie(texture, (float)currentWeatherSet.cookies[0].width * densityMapScale * 0.75f, windDisplacement);
		windDisplacement += new Vector2(NetworkSceneSingleton<LevelInfo>.i.windVelocity.x, NetworkSceneSingleton<LevelInfo>.i.windVelocity.z) * Time.deltaTime;
		layerMaterial.SetVector(id_CloudsOriginOffset, new Vector2(Datum.originPosition.x + windDisplacement.x, Datum.originPosition.z + windDisplacement.y));
		base.transform.position = new Vector3(0f, Datum.LocalSeaY + layerHeight, 0f);
		int i = 0;
		float num = cloudDrawDist;
		samplePoint = transform.position + transform.forward * num * 0.2f + SceneSingleton<CameraStateManager>.i.cameraVelocity * 5f;
		float num2 = base.transform.position.y - transform.position.y;
		float num3 = Mathf.Clamp01(1f + num2 * 0.00025f);
		Vector3 position = Datum.origin.position;
		for (; (float)i < (float)generationRate * num3 * cloudDetail; i++)
		{
			Vector2 insideUnitCircle = Random.insideUnitCircle;
			samplePoint += new Vector3(insideUnitCircle.x * num, 0f, insideUnitCircle.y * num);
			GlobalPosition globalPosition = samplePoint.ToGlobalPosition();
			Color pixel = currentWeatherSet.particleSampler.GetPixel((int)((globalPosition.x - windDisplacement.x) / densityMapScale % (float)texture2D.width), (int)((globalPosition.z - windDisplacement.y) / densityMapScale % (float)texture2D.height));
			if (pixel.r > 0.15f)
			{
				spawnOpacity = 1f - pixel.r;
				main.startColor = new Color(1f, 1f, 1f, pixel.r * 0.5f);
				samplePoint -= position;
				samplePoint.y = layerHeight + Mathf.Sign(Random.value) * spawnOpacity * layerThickness;
				emitParams.position = samplePoint;
				emitParams.velocity = Vector3.right * NetworkSceneSingleton<LevelInfo>.i.windVelocity.x + Vector3.forward * NetworkSceneSingleton<LevelInfo>.i.windVelocity.y;
				float t = (Mathf.Abs(insideUnitCircle.x) + Mathf.Abs(insideUnitCircle.y)) / (num * 1.2f);
				float num4 = Mathf.Lerp(cloudSizeMin, cloudSizeMax, t);
				num4 *= Mathf.Lerp(0.1f, 1.1f, sizeFactor);
				main.startLifetimeMultiplier = Mathf.Lerp(cloudLifeMin, cloudLifeMax, t);
				main.startSizeMultiplier = num4 * Random.Range(1f - cloudSizeVariation, 1f + cloudSizeVariation);
				cloudSystem.Emit(emitParams, 1);
			}
		}
		float num5 = base.transform.position.y + 50f;
		GlobalPosition globalPosition2 = transform.GlobalPosition();
		Vector3 cameraVelocity = SceneSingleton<CameraStateManager>.i.cameraVelocity;
		_ = (cameraVelocity - camVelPrev) / Time.unscaledDeltaTime;
		GlobalPosition position2 = globalPosition2 + SceneSingleton<CameraStateManager>.i.transform.forward * 30f + cameraVelocity * 0.8f;
		camVelPrev = cameraVelocity;
		float num6 = position2.ToLocalPosition().y - num5;
		float num7 = 1f;
		if (num6 < -50f)
		{
			num7 = 1f + (num6 + 50f) * 0.01f;
		}
		if (num6 > 50f)
		{
			num7 = 1f - (num6 - 50f) * 0.03f;
		}
		if (num7 > 0f)
		{
			flyThroughSystem.transform.position = position2.ToLocalPosition();
			Color pixel2 = currentWeatherSet.particleSampler.GetPixel((int)((position2.x - windDisplacement.x) / densityMapScale % (float)texture2D.width), (int)((position2.z - windDisplacement.y) / densityMapScale % (float)texture2D.height));
			num7 *= pixel2.r;
			if (pixel2.r > 0f)
			{
				float magnitude = SceneSingleton<CameraStateManager>.i.cameraVelocity.magnitude;
				flyThroughShape.radius = 50f * (1f + magnitude * 0.005f);
				flyThroughMain.startColor = new Color(1f, 1f, 1f, num7 * num7);
				flyThroughEmit.rateOverTime = Mathf.Lerp(20f, 60f, magnitude * 0.003f * cloudDetail);
				flyThroughSystem.transform.LookAt(SceneSingleton<CameraStateManager>.i.transform.position);
				flyThroughMain.startLifetime = Mathf.Lerp(6f, 1.5f, magnitude * 0.005f);
				flyThroughMain.startSize = Mathf.Lerp(25f, 100f, magnitude * 0.003f);
			}
		}
		if (flyThroughSystem.isPlaying)
		{
			if (num7 <= 0f)
			{
				flyThroughSystem.Stop();
			}
		}
		else if (num7 > 0f)
		{
			flyThroughSystem.Play();
		}
	}
}
