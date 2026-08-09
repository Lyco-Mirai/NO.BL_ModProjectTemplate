using UnityEngine;

public class TrailEmitter : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem trailSystem;

	[SerializeField]
	private float emitFrequency;

	[SerializeField]
	private float opacityVariation = 1f;

	[SerializeField]
	private float scaleVariation = 0.5f;

	[SerializeField]
	private float segmentLength = 30f;

	public Rigidbody rb;

	[SerializeField]
	private Transform emitTransform;

	private ParticleSystem.EmitParams emitParams;

	private float emitCounter;

	private float baseSize;

	[SerializeField]
	private float emitDelay;

	public float opacity = 1f;

	[SerializeField]
	private float emitLifetime = 10f;

	private ParticleSystem.MainModule main;

	private void OnEnable()
	{
		main = trailSystem.main;
		baseSize = main.startSizeMultiplier;
		main.simulationSpace = ParticleSystemSimulationSpace.Custom;
		main.customSimulationSpace = Datum.origin;
	}

	public void StartTrail()
	{
		base.enabled = true;
	}

	public void StopTrail()
	{
		base.enabled = false;
	}

	private void FixedUpdate()
	{
		if (!(rb != null))
		{
			return;
		}
		if (emitDelay > 0f)
		{
			emitDelay -= Time.deltaTime;
			return;
		}
		float magnitude = rb.velocity.magnitude;
		emitCounter += Time.deltaTime * magnitude / segmentLength;
		emitLifetime -= Time.deltaTime;
		if (emitCounter > 1f)
		{
			float a = opacity * (1f - opacityVariation * Random.value);
			main.startColor = new Color(1f, 1f, 1f, a);
			main.startSizeMultiplier = baseSize * (1f - Random.value * scaleVariation);
			emitParams.position = emitTransform.position.ToGlobalPosition().AsVector3();
			emitParams.velocity = rb.velocity + main.startSpeed.constant * new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
			trailSystem.Emit(emitParams, 1);
			emitCounter = 0f;
		}
		if (emitLifetime <= 0f)
		{
			base.enabled = false;
		}
	}
}
