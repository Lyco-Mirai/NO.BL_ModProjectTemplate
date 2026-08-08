using UnityEngine;

public class Lightning : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem lightningSystem;

	private ParticleSystem.EmitParams emitParams;

	[SerializeField]
	private Light flashLight;

	private ParticleSystem.MainModule main;

	[SerializeField]
	private float flashIntensity;

	[SerializeField]
	private AudioClip[] thunderClips;

	private float strikeTimer = 10f;

	private void OnEnable()
	{
		main = lightningSystem.main;
		main.simulationSpace = ParticleSystemSimulationSpace.Custom;
		main.customSimulationSpace = Datum.origin;
		flashLight.transform.SetParent(Datum.origin);
		base.transform.localPosition = Vector3.zero;
		strikeTimer = Random.Range(2, 20);
		flashLight.enabled = false;
	}

	private void GenerateStrike()
	{
		flashLight.enabled = true;
		Vector3 vector = SceneSingleton<CameraStateManager>.i.transform.position + SceneSingleton<CameraStateManager>.i.transform.forward * 10000f;
		vector += new Vector3(Random.Range(-10000, 10000), 0f, Random.Range(-10000, 10000));
		Vector3 vector2 = vector.ToGlobalPosition().AsVector3();
		vector2.y = NetworkSceneSingleton<LevelInfo>.i.cloudHeight + 50f;
		Vector3 localPosition = vector;
		int i = 0;
		for (int num = 30; i < num; i++)
		{
			emitParams.position = vector2;
			localPosition += vector2;
			lightningSystem.Emit(emitParams, 1);
			vector2 += new Vector3(Random.Range(-40, 40), -60f, Random.Range(-40, 40));
		}
		localPosition /= (float)i;
		flashLight.transform.localPosition = localPosition;
		flashLight.intensity = flashIntensity;
		if (!FastMath.OutOfRange(vector, SceneSingleton<CameraStateManager>.i.transform.position, 8000f))
		{
			GameObject obj = new GameObject();
			obj.name = "Lightning Sound";
			obj.transform.SetParent(Datum.origin);
			obj.transform.position = new Vector3(vector.x, SceneSingleton<CameraStateManager>.i.transform.position.y, vector.z);
			AudioSource audioSource = obj.AddComponent<AudioSource>();
			audioSource.maxDistance = 8000f;
			audioSource.spatialBlend = 1f;
			audioSource.dopplerLevel = 0f;
			audioSource.spread = 20f;
			audioSource.rolloffMode = AudioRolloffMode.Linear;
			audioSource.clip = thunderClips[Mathf.FloorToInt(Random.Range(0, thunderClips.Length))];
			AudioLowPassFilter filter = obj.AddComponent<AudioLowPassFilter>();
			SceneSingleton<ExplosionAudioManager>.i.AddExplosionAudio(audioSource, filter, 0f);
			Object.Destroy(obj, 35f);
		}
	}

	private void Update()
	{
		if (!(NetworkSceneSingleton<LevelInfo>.i == null))
		{
			flashLight.intensity -= flashIntensity * Time.deltaTime;
			if (flashLight.intensity <= 0f)
			{
				flashLight.enabled = false;
			}
			strikeTimer -= Time.deltaTime;
			if (strikeTimer <= 0f)
			{
				GenerateStrike();
				strikeTimer = Random.Range(2, 20);
			}
		}
	}
}
