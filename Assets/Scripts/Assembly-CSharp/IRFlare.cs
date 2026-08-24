using UnityEngine;

public class IRFlare : MonoBehaviour
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

	private IRSource IR;

	private Aircraft aircraft;

	private float checkTime;

	private bool nearAircraft = true;

	private void OnEnable()
	{
		main = smokeParticles.main;
		main.simulationSpace = ParticleSystemSimulationSpace.Custom;
		main.customSimulationSpace = Datum.origin;
		checkTime = Time.timeSinceLevelLoad;
	}

	public void LaunchFlare(Aircraft aircraft, Transform launchPoint, Vector3 launchVelocity)
	{
		IR = new IRSource(base.transform, 1f, flare: true);
		base.transform.position = launchPoint.position - launchVelocity * Time.deltaTime;
		velocity = launchVelocity;
		this.aircraft = aircraft;
		aircraft.AddIRSource(IR);
		emitParams.position = base.transform.GlobalPosition().AsVector3();
		emitParams.velocity = velocity + main.startSpeed.constant * velocity.magnitude * 0.01f * new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
		smokeParticles.Emit(emitParams, 1);
	}

	public void Emit(float airspeed, Vector3 velocity)
	{
		emitCounter += Time.deltaTime * Mathf.Min(emitFrequency, airspeed / minSpeed * emitFrequency);
		if (emitCounter > 1f)
		{
			emitCounter = 0f;
			emitParams.position = base.transform.GlobalPosition().AsVector3();
			emitParams.velocity = velocity + main.startSpeed.constant * airspeed * 0.01f * new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
			smokeParticles.Emit(emitParams, 1);
		}
	}

	private void CheckFlareDistance()
	{
		if (!(aircraft == null) && nearAircraft && !(Time.timeSinceLevelLoad - checkTime < 0.5f))
		{
			checkTime = Time.timeSinceLevelLoad;
			if (FastMath.OutOfRange(base.transform.position, aircraft.transform.position, 100f))
			{
				aircraft.RemoveIRSource(IR);
				nearAircraft = false;
			}
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
			CheckFlareDistance();
			Emit(velocity.magnitude, velocity);
		}
		if (burnTime <= 0f && flareParticles.isPlaying)
		{
			flareLight.enabled = false;
			IR.intensity = 0f;
			if (aircraft != null && nearAircraft)
			{
				aircraft.RemoveIRSource(IR);
			}
			flareParticles.Stop();
			flareLight.enabled = false;
			smokeParticles.gameObject.transform.SetParent(Datum.origin);
			Object.Destroy(smokeParticles.gameObject, 15f);
			Object.Destroy(base.gameObject, 1f);
		}
	}
}
