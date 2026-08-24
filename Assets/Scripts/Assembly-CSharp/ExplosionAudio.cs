using System;
using UnityEngine;

public class ExplosionAudio : MonoBehaviour
{
	[Serializable]
	public class ExplosionSound
	{
		public AudioSource source;

		public AudioClip[] clips;

		public float yield;
	}

	[SerializeField]
	private ExplosionSound[] explosionSounds;

	private AudioLowPassFilter lowPassFilter;

	private void Start()
	{
		lowPassFilter = base.gameObject.AddComponent<AudioLowPassFilter>();
		float num = FastMath.SquareDistance(SceneSingleton<CameraStateManager>.i.transform.position, base.transform.position);
		ExplosionSound[] array = explosionSounds;
		foreach (ExplosionSound explosionSound in array)
		{
			float maxDistance = explosionSound.source.maxDistance;
			if (num < maxDistance * maxDistance)
			{
				explosionSound.source.clip = explosionSound.clips[Mathf.FloorToInt(UnityEngine.Random.Range(0, explosionSound.clips.Length))];
				explosionSound.source.pitch += UnityEngine.Random.Range(-0.2f, 0.2f);
				explosionSound.source.dopplerLevel = 0f;
				explosionSound.source.spatialBlend = 1f;
				SceneSingleton<ExplosionAudioManager>.i.AddExplosionAudio(explosionSound.source, lowPassFilter, explosionSound.yield);
			}
		}
		UnityEngine.Object.Destroy(this);
	}
}
