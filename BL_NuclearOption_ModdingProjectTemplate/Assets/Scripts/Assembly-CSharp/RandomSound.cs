using UnityEngine;

public class RandomSound : MonoBehaviour
{
	public AudioSource source;

	public AudioClip[] clips;

	public float pitchVariation = 0.05f;

	public float volumeVariation = 0.05f;

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
	}
}
