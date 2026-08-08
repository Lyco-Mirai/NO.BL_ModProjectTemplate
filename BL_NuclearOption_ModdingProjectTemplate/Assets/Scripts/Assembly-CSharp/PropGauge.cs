using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropGauge : HUDApp
{
	[SerializeField]
	private string sourceName;

	[SerializeField]
	private TextMeshProUGUI rpmText;

	[SerializeField]
	private TextMeshProUGUI aoaText;

	[SerializeField]
	private TextMeshProUGUI powerText;

	[SerializeField]
	private Image propCircle;

	[SerializeField]
	private float warningThreshold;

	[SerializeField]
	private float maxRPM;

	private Gradient redToGreenGradient;

	private Aircraft aircraft;

	private ConstantSpeedProp source;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.gameObject.name == sourceName)
			{
				source = item.gameObject.GetComponent<ConstantSpeedProp>();
				break;
			}
		}
		PropGauge_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += PropGauge_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= PropGauge_OnThemeGroupChanged;
	}

	public override void Refresh()
	{
		if (!(source == null))
		{
			float rPMRatio = source.GetRPMRatio();
			rpmText.text = $"{rPMRatio * 100f:F1}%";
			aoaText.text = $"{source.GetAoA():F1}°";
			powerText.text = UnitConverter.PowerReading(source.GetPowerAvailable() * 0.001f);
			Color color = redToGreenGradient.Evaluate((rPMRatio - warningThreshold) / (1f - warningThreshold));
			rpmText.color = color;
			propCircle.fillAmount = rPMRatio;
		}
	}

	private void PropGauge_OnThemeGroupChanged()
	{
		redToGreenGradient = ThemeManager.Active.ColorTheme.Gradient();
	}
}
