using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WingAngleGauge : HUDApp
{
	[SerializeField]
	private Transform wingIcon;

	[SerializeField]
	private Transform inputPivot;

	[SerializeField]
	private TextMeshProUGUI label;

	private IWingAngleGauge wingAngleInterface;

	[SerializeField]
	private Transform HUDAnchor;

	[SerializeField]
	private bool fade;

	[SerializeField]
	private Image[] fadeImages;

	private float angleMin;

	private float angleMax;

	private ControlInputs inputs;

	private float lastAngle;

	private float lastChange;

	private float opacity;

	private float[] imageAlphas;

	public override void Initialize(Aircraft aircraft)
	{
		inputs = aircraft.GetInputs();
		wingAngleInterface = aircraft.gameObject.GetComponent<IWingAngleGauge>();
		angleMin = wingAngleInterface.GetLowerAngleLimit();
		angleMax = wingAngleInterface.GetUpperAngleLimit();
		opacity = 1f;
		imageAlphas = new float[fadeImages.Length];
		for (int i = 0; i < fadeImages.Length; i++)
		{
			imageAlphas[i] = fadeImages[i].color.a;
		}
	}

	private void FadeOut()
	{
		opacity -= Time.deltaTime;
		if (opacity <= 0f)
		{
			Image[] array = fadeImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
			label.enabled = false;
		}
		else
		{
			for (int j = 0; j < fadeImages.Length; j++)
			{
				fadeImages[j].color = new Color(0f, 1f, 0f, imageAlphas[j] * opacity);
			}
			label.color = new Color(0f, 1f, 0f, opacity);
		}
	}

	private void FadeIn()
	{
		if (opacity <= 0f)
		{
			Image[] array = fadeImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			label.enabled = true;
		}
		opacity += 3f * Time.deltaTime;
		opacity = Mathf.Clamp01(opacity);
		for (int j = 0; j < fadeImages.Length; j++)
		{
			fadeImages[j].color = new Color(0f, 1f, 0f, imageAlphas[j] * opacity);
		}
		label.color = new Color(0f, 1f, 0f, opacity);
	}

	public override void Refresh()
	{
		float num = Mathf.Lerp(angleMin, angleMax, inputs.customAxis1);
		float wingAngle = wingAngleInterface.GetWingAngle();
		if (wingAngle != lastAngle)
		{
			lastChange = Time.timeSinceLevelLoad;
		}
		lastAngle = wingAngle;
		if (fade)
		{
			if (Time.timeSinceLevelLoad - lastChange > 5f)
			{
				if (!(opacity > 0f))
				{
					return;
				}
				FadeOut();
			}
			else if (opacity < 1f)
			{
				FadeIn();
			}
		}
		inputPivot.localEulerAngles = new Vector3(0f, 0f, num);
		wingIcon.localEulerAngles = new Vector3(0f, 0f, wingAngle);
		label.text = $"WING {num:0}°";
	}
}
