using System;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour, IProgress<float>
{
	private static LoadingScreen i;

	[SerializeField]
	private Sprite[] images;

	[SerializeField]
	private Image loadingImage;

	[SerializeField]
	private Slider loadingProgress;

	private int activeCount;

	private float progressStart;

	private float progressEnd;

	public static LoadingScreen GetLoadingScreen()
	{
		if (i == null)
		{
			i = UnityEngine.Object.Instantiate(GameAssets.i.loadingScreenPrefab);
			UnityEngine.Object.DontDestroyOnLoad(i.gameObject);
			i.gameObject.SetActive(value: false);
		}
		return i;
	}

	private void Awake()
	{
		loadingProgress.minValue = 0f;
		loadingProgress.maxValue = 1f;
	}

	public void ShowLoadingScreen(Sprite imageOverride = null)
	{
		if (!GameManager.IsHeadless)
		{
			ColorLog<LoadingScreen>.Info($"ShowLoadingScreen activeCount={activeCount}");
			activeCount++;
			if (activeCount <= 1)
			{
				loadingImage.sprite = ((imageOverride != null) ? imageOverride : images[UnityEngine.Random.Range(0, images.Length)]);
				SetProgressRange(0f, 1f);
				loadingProgress.value = 0f;
				base.gameObject.SetActive(value: true);
			}
		}
	}

	public void HideLoadingScreen()
	{
		if (!GameManager.IsHeadless)
		{
			activeCount--;
			ColorLog<LoadingScreen>.Info($"HideLoadingScreen activeCount={activeCount}");
			if (activeCount < 1)
			{
				loadingProgress.value = 1f;
				base.gameObject.SetActive(value: false);
			}
		}
	}

	public void SetProgressRange(float start, float end)
	{
		progressStart = start;
		progressEnd = end;
	}

	void IProgress<float>.Report(float value)
	{
		if (!GameManager.IsHeadless)
		{
			float value2 = Mathf.Lerp(progressStart, progressEnd, value);
			loadingProgress.value = value2;
		}
	}
}
