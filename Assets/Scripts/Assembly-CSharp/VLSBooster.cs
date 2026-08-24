using UnityEngine;

public class VLSBooster : MonoBehaviour
{
	[SerializeField]
	private Missile missile;

	private Rigidbody rb;

	[SerializeField]
	private float thrust;

	[SerializeField]
	private float delayTimer;

	[SerializeField]
	private float burnTime;

	[SerializeField]
	private float fuelMass;

	[SerializeField]
	private float dryMass;

	[SerializeField]
	private float torque;

	[SerializeField]
	private float maxTurnRate;

	private float burnRate;

	private float originalTorque;

	private float originalMaxTurnRate;

	private bool activated;

	private bool separated;

	private bool splashed;

	[SerializeField]
	private ParticleSystem[] particleSystems;

	[SerializeField]
	private TrailEmitter[] trailEmitters;

	[SerializeField]
	private AudioSource[] audioSources;

	[SerializeField]
	private Light[] lights;

	[SerializeField]
	private GameObject splashPrefab;

	private void Awake()
	{
		missile.onInitialize += VLSBooster_OnInitialize;
		burnRate = fuelMass / burnTime;
	}

	private void VLSBooster_OnInitialize()
	{
		missile.onInitialize -= VLSBooster_OnInitialize;
		if (missile.owner == null || missile.owner is Aircraft || GameManager.gameState == GameState.Encyclopedia)
		{
			missile.boosterIsAttached = false;
			Object.Destroy(base.gameObject);
		}
		else
		{
			missile.boosterIsAttached = true;
		}
	}

	private void Activate()
	{
		activated = true;
		ParticleSystem[] array = particleSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
		AudioSource[] array2 = audioSources;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Play();
		}
		Light[] array3 = lights;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].enabled = true;
		}
	}

	public float Thrust()
	{
		fuelMass -= burnRate * Time.fixedDeltaTime;
		missile.rb.mass -= burnRate * Time.fixedDeltaTime;
		if (fuelMass <= 0f || base.transform.position.y < Datum.LocalSeaY)
		{
			Burnout();
		}
		if (missile.LocalSim)
		{
			missile.rb.AddForce(thrust * missile.transform.forward);
		}
		return thrust;
	}

	public void Splash()
	{
		splashed = true;
		if (SceneSingleton<ParticleEffectManager>.i != null)
		{
			SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(splashPrefab).Play(new Vector3(base.transform.position.x, Datum.LocalSeaY, base.transform.position.z), Quaternion.LookRotation(Vector3.up + rb.velocity.normalized));
		}
		base.enabled = false;
	}

	public void Burnout()
	{
		ParticleSystem[] array = particleSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
		AudioSource[] array2 = audioSources;
		foreach (AudioSource audioSource in array2)
		{
			if (audioSource.loop)
			{
				audioSource.Stop();
			}
		}
		Light[] array3 = lights;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].enabled = false;
		}
		separated = true;
		missile.boosterIsAttached = false;
		base.transform.SetParent(null);
		rb = base.gameObject.AddComponent<Rigidbody>();
		rb.mass = dryMass;
		rb.drag = 0.1f;
		rb.angularDrag = 0.01f;
		rb.isKinematic = false;
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.velocity = (missile.rb.isKinematic ? Vector3.zero : missile.rb.velocity);
		Object.Destroy(base.gameObject, 10f);
	}

	private void FixedUpdate()
	{
		if (!activated)
		{
			delayTimer -= Time.deltaTime;
			if (delayTimer <= 0f)
			{
				Activate();
			}
		}
		if (activated && fuelMass > 0f)
		{
			Thrust();
		}
		if (separated && !splashed && base.transform.position.y < Datum.LocalSeaY)
		{
			Splash();
		}
	}
}
