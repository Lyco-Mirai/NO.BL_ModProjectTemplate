using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
	[SerializeField]
	private AudioSource currentSource;

	[SerializeField]
	private AudioSource fadeSource;

	private bool isFading;

	private float currentClipPriority;

	private List<AudioClip> clipsPlayed = new List<AudioClip>();

	[SerializeField]
	private AudioClip menuMusic;

	public static MusicManager i => SoundManager.i.Music;

	private void Awake()
	{
		currentSource.ignoreListenerPause = true;
		fadeSource.ignoreListenerPause = true;
	}

	public bool IsPlaying()
	{
		return currentSource.isPlaying;
	}

	public void PlayMenuMusic()
	{
		if (!IsPlaying())
		{
			PlayMusic(menuMusic, repeat: false);
		}
	}

	public void PlayMusic(AudioClip audioClip, bool repeat)
	{
		if (!GameManager.IsHeadless)
		{
			currentSource.time = 0f;
			currentSource.clip = audioClip;
			currentSource.loop = repeat;
			currentSource.volume = 1f;
			currentSource.Play();
		}
	}

	public static void ResetPlayedMusic()
	{
		i.clipsPlayed.Clear();
	}

	public void CrossFadeMusic(AudioClip audioClip, float fadeOutTime, float fadeInTime, bool repeat, bool allowReplay, bool replacePlaying, float priority = 0f)
	{
		if (GameManager.IsHeadless || audioClip == null || (!replacePlaying && IsPlaying()) || (IsPlaying() && priority < currentClipPriority))
		{
			return;
		}
		if (isFading)
		{
			Debug.LogWarning("Already CrossFading");
			return;
		}
		if (!allowReplay)
		{
			if (HasPlayedMusic(audioClip))
			{
				return;
			}
			clipsPlayed.Add(audioClip);
		}
		fadeSource.time = 0f;
		fadeSource.clip = audioClip;
		fadeSource.loop = repeat;
		fadeSource.volume = 0f;
		if (audioClip != null)
		{
			fadeSource.Play();
		}
		CrossFade(fadeOutTime, fadeInTime).Forget();
	}

	public bool HasPlayedMusic(AudioClip clip)
	{
		return clipsPlayed.Contains(clip);
	}

	public void QueueMusicClip(AudioClip audioClip, float clipPriority)
	{
		if (!GameManager.IsHeadless)
		{
			Debug.Log("Queuing audioClip " + audioClip.name);
			QueueMusic(audioClip, clipPriority).Forget();
		}
	}

	private async UniTask QueueMusic(AudioClip audioClip, float priority)
	{
		while (IsPlaying())
		{
			await UniTask.Delay(1000);
		}
		if (GameManager.gameResolution == GameResolution.Ongoing)
		{
			currentClipPriority = priority;
			PlayMusic(audioClip, repeat: false);
		}
	}

	private async UniTask CrossFade(float outTime, float inTime)
	{
		isFading = true;
		try
		{
			CancellationToken cancel = base.destroyCancellationToken;
			float outVolume = 1f;
			float inVolume = 0f;
			while (outVolume > 0f || inVolume < 1f)
			{
				await UniTask.Yield();
				if (cancel.IsCancellationRequested)
				{
					return;
				}
				outVolume = ((outTime == 0f) ? 0f : (outVolume - Time.deltaTime / outTime));
				inVolume = ((inTime == 0f) ? 1f : (inVolume + Time.deltaTime / inTime));
				fadeSource.volume = inVolume;
				currentSource.volume = outVolume;
			}
			AudioSource audioSource = currentSource;
			AudioSource audioSource2 = fadeSource;
			fadeSource = audioSource;
			currentSource = audioSource2;
			currentSource.volume = 1f;
			fadeSource.volume = 0f;
			fadeSource.Stop();
		}
		finally
		{
			isFading = false;
		}
	}

	public void FadeOut(float time)
	{
		if (!GameManager.IsHeadless && !(currentSource == null))
		{
			FadeoutSource(currentSource, time).Forget();
		}
	}

	private async UniTask FadeoutSource(AudioSource source, float time)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		while (source.volume > 0f)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			source.volume -= Time.deltaTime / time;
		}
		source.Stop();
	}

	public void StopMusic()
	{
		currentClipPriority = 0f;
		currentSource.Stop();
	}
}
