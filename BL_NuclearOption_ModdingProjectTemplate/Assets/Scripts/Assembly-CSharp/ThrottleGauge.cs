using System;
using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThrottleGauge : HUDApp
{
	[Serializable]
	private class ThrottleRegion
	{
		[SerializeField]
		private string name;

		[SerializeField]
		private bool showName;

		[SerializeField]
		private bool showPercent;

		[SerializeField]
		private float start;

		[SerializeField]
		private float end;

		private float percent;

		public bool IsActive(float input)
		{
			if (input >= start)
			{
				return input <= end;
			}
			return false;
		}

		public float GetPercent(float input)
		{
			percent = ((end - start > 0f) ? ((input - start) / (end - start)) : 1f);
			return percent;
		}

		public string GetText()
		{
			string text = string.Empty;
			if (showName)
			{
				text = name;
			}
			if (showPercent)
			{
				text += $"{percent * 100f:0}%";
			}
			return text;
		}

		public float GetStart()
		{
			return start;
		}

		public float GetEnd()
		{
			return end;
		}
	}

	[SerializeField]
	private Image throttleBar;

	[SerializeField]
	private Image throttleArc;

	[SerializeField]
	private Image throttlePointer;

	[SerializeField]
	private TextMeshProUGUI throttleReading;

	[SerializeField]
	private TextMeshProUGUI throttleLabel;

	[SerializeField]
	private Transform throttleReadingPivot;

	[SerializeField]
	private Transform throttleBoundaryPivot;

	private ControlInputs inputs;

	[SerializeField]
	private bool airbrake;

	[SerializeField]
	private bool afterburner;

	[SerializeField]
	private ThrottleRegion[] throttleRegions;

	private ThrottleRegion currentRegion;

	private Aircraft aircraft;

	private float throttlePrev = -1f;

	private Gradient greenToOrangeGradient;

	public override void Initialize(Aircraft aircraft)
	{
		inputs = aircraft.GetInputs();
		this.aircraft = aircraft;
		throttlePrev = -1f;
		if (throttleBoundaryPivot != null && throttleRegions.Length != 0 && afterburner)
		{
			throttleBoundaryPivot.localEulerAngles = new Vector3(0f, 0f, (throttleRegions[^1].GetStart() + 0.01f) * 26f - 13f);
		}
		UpdateGradient();
		Show(PlayerSettings.gauges);
		ThemeManager.ThemeGroupChanged += ThrottleGauge_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= ThrottleGauge_OnThemeGroupChanged;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		throttleReading.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		throttleLabel.fontSize = (int)((float)(fontSize - 4) * fontSizeMultiplier);
		Show(PlayerSettings.gauges);
	}

	public override void Refresh()
	{
		if (!(aircraft == null) && !Mathf.Approximately(throttlePrev, inputs.throttle))
		{
			UpdateThrottleDisplay();
		}
	}

	private void UpdateThrottleDisplay()
	{
		throttlePrev = inputs.throttle;
		throttleReadingPivot.localEulerAngles = new Vector3(0f, 0f, inputs.throttle * 26f - 13f);
		float z = SceneSingleton<CameraStateManager>.i.mainCamera.transform.eulerAngles.z;
		float z2 = aircraft.cockpit.transform.eulerAngles.z;
		throttleReading.transform.eulerAngles = new Vector3(0f, 0f, 0f - (z - z2));
		throttleBar.fillAmount = inputs.throttle;
		if (throttleRegions.Length != 0)
		{
			ThrottleRegion[] array = throttleRegions;
			foreach (ThrottleRegion throttleRegion in array)
			{
				if (throttleRegion.IsActive(inputs.throttle))
				{
					currentRegion = throttleRegion;
				}
			}
		}
		if (currentRegion != null)
		{
			currentRegion.GetPercent(inputs.throttle);
			Color color = greenToOrangeGradient.Evaluate(inputs.throttle);
			throttleBar.color = color;
			throttleReading.color = color;
			throttleReading.text = currentRegion.GetText();
			return;
		}
		throttleReading.text = $"{inputs.throttle * 100f:F0}%";
		throttleBar.color = greenToOrangeGradient.Evaluate(inputs.throttle);
		throttleReading.color = throttleBar.color;
		if (afterburner && inputs.throttle == 1f)
		{
			throttleReading.text = "AFTERBURNER";
			throttleReading.color = greenToOrangeGradient.Evaluate(1f);
			throttleBar.color = greenToOrangeGradient.Evaluate(1f);
		}
		if (airbrake && inputs.throttle == 0f)
		{
			throttleReading.text = "AIRBRAKE";
		}
	}

	private void UpdateGradient()
	{
		if (throttleRegions.Length != 0)
		{
			greenToOrangeGradient = AfterburnerGradient();
			return;
		}
		greenToOrangeGradient = ThemeManager.Active.ColorTheme.Gradient(new List<float> { 1f, 0.5f }, new List<float> { 0f, 1f });
		Gradient AfterburnerGradient()
		{
			List<float> list = new List<float> { throttleRegions[0].GetStart() };
			ThrottleRegion[] array = throttleRegions;
			foreach (ThrottleRegion throttleRegion in array)
			{
				list.Add(throttleRegion.GetEnd());
			}
			return ThemeManager.Active.ColorTheme.Gradient(new List<float> { 1f, 1f, 0.5f, 0.5f, 0.25f }, new List<float>(list));
		}
	}

	public void Show(bool arg)
	{
		throttleArc.enabled = arg;
		throttleBar.enabled = arg;
		throttlePointer.enabled = arg;
		throttleLabel.enabled = arg;
		throttleReading.enabled = arg;
	}

	private void ThrottleGauge_OnThemeGroupChanged()
	{
		UpdateGradient();
		UpdateThrottleDisplay();
	}
}
