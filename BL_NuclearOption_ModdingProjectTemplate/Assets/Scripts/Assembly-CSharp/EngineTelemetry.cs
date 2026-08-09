using System;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EngineTelemetry : MonoBehaviour
{
	private enum EngineState
	{
		AllClear = 0,
		Damage = 1,
		Failure = 2
	}

	[Serializable]
	private class Gauge
	{
		[SerializeField]
		private TextMeshProUGUI title;

		[SerializeField]
		private TextMeshProUGUI reading;

		[SerializeField]
		private Transform needle;

		[SerializeField]
		private float maxValue;

		public void UpdatePower(float power)
		{
			if (!(reading == null))
			{
				reading.text = (power * 0.001f).ToString("F1");
				needle.transform.localEulerAngles = Vector3.forward * (power * 0.001f / maxValue) * -300f;
			}
		}

		public void UpdateNumber(float number)
		{
			if (!(reading == null))
			{
				reading.text = number.ToString("F0");
				needle.transform.localEulerAngles = Vector3.forward * (number / maxValue) * -300f;
			}
		}

		public void UpdateRPM(float rpmRatio)
		{
			if (!(reading == null))
			{
				reading.text = $"{rpmRatio * 100f:F0}%";
				needle.transform.localEulerAngles = Vector3.forward * rpmRatio * -300f;
			}
		}

		public void UpdateThrust(float thrust)
		{
			if (!(reading == null))
			{
				reading.text = (thrust * 0.001f).ToString("F1");
				needle.transform.localEulerAngles = Vector3.forward * (thrust / maxValue) * -300f;
			}
		}
	}

	private Aircraft aircraft;

	[SerializeField]
	private string engineName;

	[SerializeField]
	private Gauge thrustGauge;

	[SerializeField]
	private Gauge rpmGauge;

	[SerializeField]
	private Gauge throttleGauge;

	[SerializeField]
	private Gauge pitchGauge;

	[SerializeField]
	private TextMeshProUGUI statusDisplay;

	[SerializeField]
	private TextMeshProUGUI[] colorableTexts;

	[SerializeField]
	private Image[] colorableImages;

	[SerializeField]
	private bool preferDisplayThrust;

	private IEngine engineInterface;

	private bool displayPower;

	private bool displayPitch;

	private float lastUpdate;

	private ControlInputs controlInputs;

	private EngineState engineState;

	private void Start()
	{
		aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		foreach (DamageablePart damageable in aircraft.damageables)
		{
			Transform transform = damageable.Damageable.GetTransform();
			if (transform.gameObject.name == engineName && transform.gameObject.TryGetComponent<IEngine>(out engineInterface))
			{
				engineInterface.OnEngineDisable += EngineTelemetry_OnEngineFailure;
				engineInterface.OnEngineDamage += EngineTelemetry_OnEngineDamage;
				break;
			}
		}
		if (engineInterface == null)
		{
			Debug.LogWarning("Couldn't find engine interface for part " + engineName);
		}
		if (engineInterface is IPowerSource)
		{
			displayPower = true;
		}
		if (engineInterface is IPitchTelemetry)
		{
			displayPitch = true;
		}
		controlInputs = aircraft.GetInputs();
		EngineTelemetry_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += EngineTelemetry_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		if (engineInterface != null)
		{
			engineInterface.OnEngineDisable -= EngineTelemetry_OnEngineFailure;
			engineInterface.OnEngineDamage -= EngineTelemetry_OnEngineDamage;
		}
		ThemeManager.ThemeGroupChanged -= EngineTelemetry_OnThemeGroupChanged;
	}

	private void EngineTelemetry_OnEngineFailure()
	{
		engineState = EngineState.Failure;
		ApplyColor(ThemeManager.Active.ColorTheme.Alert);
		statusDisplay.text = "INOPERABLE";
	}

	private void EngineTelemetry_OnEngineDamage()
	{
		engineState = EngineState.Damage;
		ApplyColor(ThemeManager.Active.ColorTheme.Warning);
		statusDisplay.text = "FAULT";
	}

	private void EngineTelemetry_OnThemeGroupChanged()
	{
		switch (engineState)
		{
		case EngineState.Failure:
			ApplyColor(ThemeManager.Active.ColorTheme.Alert);
			break;
		case EngineState.Damage:
			ApplyColor(ThemeManager.Active.ColorTheme.Warning);
			break;
		default:
			ApplyColor(ThemeManager.Active.ColorTheme.AllClear);
			break;
		}
	}

	private void ApplyColor(Color color)
	{
		TextMeshProUGUI[] array = colorableTexts;
		foreach (TextMeshProUGUI textMeshProUGUI in array)
		{
			textMeshProUGUI.color = color.WithAlpha(textMeshProUGUI.color.a);
		}
		Image[] array2 = colorableImages;
		foreach (Image image in array2)
		{
			image.color = color.WithAlpha(image.color.a);
		}
		statusDisplay.color = color.WithAlpha(statusDisplay.color.a);
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad - lastUpdate < 0.05f)
		{
			return;
		}
		lastUpdate = Time.timeSinceLevelLoad;
		if (!(engineInterface as UnityEngine.Object))
		{
			base.enabled = false;
			return;
		}
		if (displayPower)
		{
			if (engineInterface is IPowerSource powerSource)
			{
				float power = powerSource.GetPower();
				thrustGauge.UpdatePower(power);
			}
			if (preferDisplayThrust && engineInterface is IThrustSource thrustSource)
			{
				float thrust = thrustSource.GetThrust();
				thrustGauge.UpdateThrust(thrust);
			}
		}
		else if (engineInterface is IThrustSource thrustSource2)
		{
			float thrust2 = thrustSource2.GetThrust();
			thrustGauge.UpdateThrust(thrust2);
		}
		if (displayPitch)
		{
			pitchGauge.UpdateNumber((engineInterface as IPitchTelemetry).GetPitch());
		}
		rpmGauge.UpdateRPM(engineInterface.GetRPMRatio());
		throttleGauge.UpdateNumber(controlInputs.throttle * 100f);
	}
}
