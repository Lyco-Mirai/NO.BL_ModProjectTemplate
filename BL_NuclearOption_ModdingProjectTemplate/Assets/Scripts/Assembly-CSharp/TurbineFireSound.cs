using UnityEngine;

public class TurbineFireSound : MonoBehaviour
{
	public AudioSource source;

	public AudioClip[] clips;

	public float pitchVariation = 0.05f;

	public float volumeVariation = 0.05f;

	public Rigidbody rb;

	private void Start()
	{
		if (clips.Length != 0)
		{
			source.clip = clips[Random.Range(0, clips.Length)];
		}
		source.pitch += Random.Range(0f - pitchVariation, pitchVariation);
		source.volume += Random.Range(0f - volumeVariation, volumeVariation);
		source.Play();
		if (source.loop)
		{
			source.time = Random.Range(0f, source.clip.length);
		}
		rb = base.transform.GetComponentInParent<Rigidbody>();
	}

	private void Update()
	{
		if (rb != null)
		{
			source.volume = Mathf.Lerp(source.volume, Mathf.Min(source.volume, Mathf.Clamp01(rb.velocity.magnitude * 0.1f)), 0.1f);
		}
		if (base.transform.parent == Datum.origin)
		{
			source.volume = Mathf.Lerp(source.volume, 0f, 0.1f);
		}
	}
}
