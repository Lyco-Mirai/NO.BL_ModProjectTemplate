using UnityEngine;

public class HUDApp : MonoBehaviour
{
	protected enum AppType
	{
		HUD = 0,
		HMD = 1,
		MFD = 2
	}

	[SerializeField]
	protected AppType type;

	[SerializeField]
	protected float fontSizeMultiplier = 1f;

	protected int fontSize;

	protected Color fontColor;

	public virtual void Initialize(Aircraft aircraft)
	{
	}

	public virtual void Refresh()
	{
	}

	public virtual void RefreshSettings()
	{
		switch (type)
		{
		case AppType.HUD:
			fontSize = (int)PlayerSettings.hudTextSize;
			fontColor = new Color(PlayerSettings.hudColorR / 255, PlayerSettings.hudColorG / 255, PlayerSettings.hudColorB / 255);
			break;
		case AppType.HMD:
			fontSize = (int)PlayerSettings.hmdTextSize;
			fontColor = new Color(PlayerSettings.hudColorR / 255, PlayerSettings.hudColorG / 255, PlayerSettings.hudColorB / 255);
			break;
		case AppType.MFD:
			fontSize = (int)PlayerSettings.hudTextSize;
			break;
		}
	}
}
