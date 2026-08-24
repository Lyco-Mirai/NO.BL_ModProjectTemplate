using UnityEngine;

public class RadarChaff : MonoBehaviour
{
	private Vector3 velocity;

	[SerializeField]
	private float chaffTime;

	[SerializeField]
	private float drag;

	[SerializeField]
	private ParticleSystem chaffParticles;

	[SerializeField]
	private float emitFrequency;

	[SerializeField]
	private float minSpeed = 20f;

	private Vector3 gravityVector = new Vector3(0f, 9.81f, 0f);

	private RaycastHit hit;

	private int layermask = PhysicsLayers.StaticsMask;

	private Aircraft aircraft;

	private float checkTime;

	private bool nearAircraft = true;

	private void OnEnable()
	{
		checkTime = Time.timeSinceLevelLoad;
	}

	public void LaunchChaff(Aircraft aircraft, Transform launchPoint, Vector3 launchVelocity)
	{
		base.transform.position = launchPoint.position - launchVelocity * Time.deltaTime;
		velocity = launchVelocity;
		this.aircraft = aircraft;
		aircraft.AddRadarChaff(this);
	}

	private void CheckChaffDistance()
	{
		if (!(aircraft == null) && nearAircraft && !(Time.timeSinceLevelLoad - checkTime < 0.5f))
		{
			checkTime = Time.timeSinceLevelLoad;
			if (FastMath.OutOfRange(base.transform.position, aircraft.transform.position, 100f))
			{
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
		chaffTime -= Time.deltaTime;
		if (base.transform.position.GlobalY() <= 0f)
		{
			chaffTime = 0f;
			velocity = Vector3.zero;
		}
		if (chaffTime > 0f)
		{
			CheckChaffDistance();
		}
		if (chaffTime <= 0f && chaffParticles.isPlaying)
		{
			chaffParticles.Stop();
			Object.Destroy(base.gameObject, 1f);
		}
	}
}
