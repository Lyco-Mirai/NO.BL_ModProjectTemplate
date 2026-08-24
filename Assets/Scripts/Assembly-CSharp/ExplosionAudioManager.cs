using System;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionAudioManager : SceneSingleton<ExplosionAudioManager>
{
	[Serializable]
	public class ManagedExplosion
	{
		private readonly AudioSource audioSource;

		private readonly Transform xform;

		private readonly AudioLowPassFilter filter;

		private readonly float startTime;

		private readonly float yield;

		private float propagation;

		public ManagedExplosion(AudioSource audioSource, AudioLowPassFilter filter, float yield)
		{
			this.audioSource = audioSource;
			xform = audioSource.transform;
			this.filter = filter;
			startTime = Time.timeSinceLevelLoad;
			this.yield = yield;
			float num = Mathf.Pow(yield, 0.3333f);
			propagation = num * 0.5f;
		}

		public bool AudioSourceNull()
		{
			if (!(audioSource == null))
			{
				return !audioSource.isActiveAndEnabled;
			}
			return true;
		}

		public bool InRange(Vector3 listenerPosition)
		{
			propagation += 340f * Time.deltaTime;
			if (FastMath.InRange(listenerPosition, xform.position, propagation))
			{
				Play();
				return true;
			}
			return false;
		}

		public void Play()
		{
			float num = Time.timeSinceLevelLoad - startTime;
			audioSource.bypassListenerEffects = num > 0.1f;
			filter.cutoffFrequency = Mathf.Clamp(22000f / num, 1000f, 22000f);
			audioSource.Play();
			if (yield > 0f)
			{
				float num2 = Vector3.SqrMagnitude(xform.position - SceneSingleton<CameraStateManager>.i.transform.position);
				float value = yield * 100f / num2;
				SceneSingleton<CameraStateManager>.i.ShakeCamera(Mathf.Clamp01(value), 0f);
			}
		}
	}

	[SerializeField]
	private List<ManagedExplosion> sources = new List<ManagedExplosion>();

	public void AddExplosionAudio(AudioSource audioSource, AudioLowPassFilter filter, float yield)
	{
		sources.Add(new ManagedExplosion(audioSource, filter, yield));
		base.enabled = true;
	}

	private void Update()
	{
		Vector3 position = SceneSingleton<CameraStateManager>.i.transform.position;
		for (int num = sources.Count - 1; num >= 0; num--)
		{
			ManagedExplosion managedExplosion = sources[num];
			if (managedExplosion.AudioSourceNull())
			{
				sources.RemoveAt(num);
			}
			else if (managedExplosion.InRange(position))
			{
				sources.RemoveAt(num);
			}
		}
		if (sources.Count == 0)
		{
			base.enabled = false;
		}
	}
}
