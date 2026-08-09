using NuclearOption.Networking;
using UnityEngine;

public class ImpactDetector : MonoBehaviour
{
	[SerializeField]
	private float gLimit;

	private Rigidbody rb;

	private IDamageable damageable;

	private Vector3 velocityPrev;

	private Vector3 accel;

	private float timeSinceSpawn;

	private void Awake()
	{
		damageable = base.gameObject.GetComponent<IDamageable>();
		if (!NetworkManagerNuclearOption.i.Server.Active || damageable == null)
		{
			Object.Destroy(this);
		}
		rb = damageable.GetUnit().rb;
		base.gameObject.GetComponent<Unit>().CheckRadarAlt();
		if (base.gameObject.GetComponent<Unit>().radarAlt < 1f)
		{
			Object.Destroy(this);
		}
	}

	private void FixedUpdate()
	{
		timeSinceSpawn += Time.fixedDeltaTime;
		if (velocityPrev != Vector3.zero)
		{
			accel = (rb.velocity - velocityPrev) / Time.fixedDeltaTime;
		}
		velocityPrev = rb.velocity;
		if (accel.sqrMagnitude > gLimit * gLimit * 82.81f)
		{
			damageable.TakeDamage(0f, 0f, 1f, 0f, accel.magnitude, PersistentID.None);
			Object.Destroy(this);
		}
		if (timeSinceSpawn > 5f && Mathf.Abs(rb.velocity.y) < 5f && rb.velocity.sqrMagnitude < 10f)
		{
			Object.Destroy(this);
		}
	}

	public void SetGLimit(float value)
	{
		gLimit = value;
	}
}
