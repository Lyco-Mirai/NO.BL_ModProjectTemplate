using System;
using UnityEngine;

[Serializable]
public class SpecialFlare : MonoBehaviour
{
	private Vector3 velocity;

	[SerializeField]
	private float burnTime;

	[SerializeField]
	private float drag;

	[SerializeField]
	private ParticleSystem flareParticles;

	[SerializeField]
	private ParticleSystem smokeParticles;

	[SerializeField]
	private float emitFrequency;

	[SerializeField]
	private float minSpeed = 20f;

	[SerializeField]
	private Light flareLight;

	private ParticleSystem.MainModule main;

	private float emitCounter;

	private ParticleSystem.EmitParams emitParams;

	private Vector3 gravityVector = new Vector3(0f, 9.81f, 0f);

	private RaycastHit hit;

	private int layermask = PhysicsLayers.StaticsMask;

	private float checkTime;

	public ParticleSystemRenderer flareRenderer;

	public ParticleSystemRenderer smokeRenderer;

	public Color[] listColor = new Color[6]
	{
		Color.blue,
		Color.green,
		Color.magenta,
		Color.magenta,
		Color.red,
		Color.yellow
	};

	public Material[] listSmoke = new Material[0];

	public Material[] listSparks = new Material[0];

	private void OnEnable()
	{
		main = smokeParticles.main;
		main.simulationSpace = ParticleSystemSimulationSpace.Custom;
		main.customSimulationSpace = Datum.origin;
		checkTime = Time.timeSinceLevelLoad;
		int num = UnityEngine.Random.Range(0, listSmoke.Length);
		flareParticles.startColor = listColor[num];
		smokeParticles.startColor = listColor[num];
		ParticleSystem.ColorOverLifetimeModule colorOverLifetime = smokeParticles.colorOverLifetime;
		Gradient gradient = new Gradient();
		gradient.SetKeys(new GradientColorKey[2]
		{
			new GradientColorKey(listColor[num], 0f),
			new GradientColorKey(listColor[num], 1f)
		}, new GradientAlphaKey[2]
		{
			new GradientAlphaKey(1f, 0f),
			new GradientAlphaKey(0f, 1f)
		});
		colorOverLifetime.color = gradient;
		flareLight.color = listColor[num];
		flareRenderer.material = listSparks[num];
		smokeRenderer.material = listSmoke[num];
	}

	public void LaunchFlare(Transform launchPoint, Vector3 launchVelocity)
	{
		base.transform.position = launchPoint.position - launchVelocity * Time.deltaTime;
		velocity = launchVelocity;
		emitParams.position = base.transform.GlobalPosition().AsVector3();
		emitParams.velocity = velocity + main.startSpeed.constant * velocity.magnitude * 0.01f * new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
		smokeParticles.Emit(emitParams, 1);
	}

	public void Emit(float airspeed, Vector3 velocity)
	{
		emitCounter += Time.deltaTime * Mathf.Min(emitFrequency, airspeed / minSpeed * emitFrequency);
		if (emitCounter > 1f)
		{
			emitCounter = 0f;
			emitParams.position = base.transform.GlobalPosition().AsVector3();
			emitParams.velocity = velocity + main.startSpeed.constant * airspeed * 0.01f * new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
			smokeParticles.Emit(emitParams, 1);
		}
	}

	private void Update()
	{
		base.transform.position += velocity * Time.deltaTime;
		Vector3 vector = velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition());
		Vector3 vector2 = vector.normalized * vector.sqrMagnitude * drag;
		velocity -= (vector2 + gravityVector) * Time.deltaTime;
		if (Physics.Linecast(base.transform.position, base.transform.position + velocity * Time.deltaTime * 1.05f, out hit, layermask))
		{
			base.transform.position = hit.point + hit.normal * 0.1f;
			velocity = Vector3.zero;
			gravityVector = Vector3.zero;
		}
		if (velocity != Vector3.zero)
		{
			base.transform.rotation = Quaternion.LookRotation(velocity);
		}
		burnTime -= Time.deltaTime;
		if (base.transform.position.GlobalY() <= 0f)
		{
			burnTime = 0f;
			velocity = Vector3.zero;
		}
		if (burnTime > 0f)
		{
			Emit(velocity.magnitude, velocity);
		}
		if (burnTime <= 0f && flareParticles.isPlaying)
		{
			flareLight.enabled = false;
			flareParticles.Stop();
			flareLight.enabled = false;
			smokeParticles.gameObject.transform.SetParent(Datum.origin);
			UnityEngine.Object.Destroy(smokeParticles.gameObject, 15f);
			UnityEngine.Object.Destroy(base.gameObject, 1f);
		}
	}
}
