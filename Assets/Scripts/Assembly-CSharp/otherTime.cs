using System;
using TMPro;
using UnityEngine;

public class otherTime : HUDApp
{
	[SerializeField]
	private TextMeshProUGUI otherTimeLabel;

	[SerializeField]
	private TextMeshProUGUI otherTimeValue;

	public override void Initialize(Aircraft aircraft)
	{
	}

	public override void RefreshSettings()
	{
		if (PlayerSettings.hudTime > 0)
		{
			otherTimeLabel.enabled = true;
			otherTimeValue.enabled = true;
		}
		else
		{
			otherTimeLabel.enabled = false;
			otherTimeValue.enabled = false;
		}
		base.RefreshSettings();
		otherTimeLabel.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		otherTimeValue.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (PlayerSettings.hudTime == 1)
		{
			otherTimeLabel.text = "Mission";
			otherTimeValue.text = UnitConverter.TimeOfDay(NetworkSceneSingleton<MissionManager>.i.MissionTime / 3600f, includeSeconds: true);
		}
		else if (PlayerSettings.hudTime == 2)
		{
			otherTimeLabel.text = "Local";
			DateTime now = DateTime.Now;
			otherTimeValue.text = $"{now.Hour:D2}:{now.Minute:D2}";
		}
	}
}
