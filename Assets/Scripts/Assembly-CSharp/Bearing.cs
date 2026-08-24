using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Bearing : HUDApp
{
	[SerializeField]
	private TextMeshProUGUI bearing;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private Image border;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		bearing.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			bearing.text = ((type == AppType.HMD) ? $"{SceneSingleton<CameraStateManager>.i.transform.eulerAngles.y:F0}°" : $"{aircraft.transform.eulerAngles.y:F0}°");
		}
	}
}
