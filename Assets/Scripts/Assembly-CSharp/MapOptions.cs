public class MapOptions : SceneSingleton<MapOptions>
{
	public enum TooltipType
	{
		None = 0,
		Info = 1,
		Ammo = 2,
		Order = 3
	}

	public MFDScreen screen;

	public TooltipType tooltipType = TooltipType.Info;

	public bool showObjectives = true;

	public bool showTargetInfo = true;

	public bool showJamming = true;

	public bool showPilotIcons = true;

	public bool showGridLabels = true;

	public bool showAirbaseIcon = true;

	public float iconSize = 1f;

	public void ToggleShowObjectives()
	{
		showObjectives = !showObjectives;
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowTargetInfo()
	{
		showTargetInfo = !showTargetInfo;
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowJamming()
	{
		showJamming = !showJamming;
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowGridLabels()
	{
		showGridLabels = !showGridLabels;
	}

	public void SetToolTipType(int value)
	{
		tooltipType = (TooltipType)value;
	}

	public void SetIconSize(int value)
	{
		iconSize = 0.6f + 0.2f * (float)value;
	}

	public void ToggleShowPilotIcons()
	{
		showPilotIcons = !showPilotIcons;
		foreach (MapIcon mapIcon in SceneSingleton<DynamicMap>.i.mapIcons)
		{
			if (mapIcon is UnitMapIcon unitMapIcon && unitMapIcon.unit is PilotDismounted)
			{
				mapIcon.gameObject.SetActive(showPilotIcons);
			}
		}
		SceneSingleton<DynamicMap>.i.ShowTypeChanged();
	}

	public void ToggleShowAirbaseIcons()
	{
		showAirbaseIcon = !showAirbaseIcon;
	}
}
