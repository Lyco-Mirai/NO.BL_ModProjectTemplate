using TMPro;
using UnityEngine;

public class GameTime : HUDApp
{
	[SerializeField]
	private TextMeshProUGUI gameTimeLabel;

	[SerializeField]
	private TextMeshProUGUI gameTimeValue;

	public override void Initialize(Aircraft aircraft)
	{
	}

	public override void RefreshSettings()
	{
		if (PlayerSettings.hudTime > 0)
		{
			gameTimeLabel.enabled = true;
			gameTimeValue.enabled = true;
		}
		else
		{
			gameTimeLabel.enabled = false;
			gameTimeValue.enabled = false;
		}
		base.RefreshSettings();
		gameTimeLabel.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		gameTimeValue.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (NetworkSceneSingleton<LevelInfo>.i != null)
		{
			gameTimeValue.text = UnitConverter.TimeOfDay(NetworkSceneSingleton<LevelInfo>.i.timeOfDay, includeSeconds: false);
		}
	}
}
