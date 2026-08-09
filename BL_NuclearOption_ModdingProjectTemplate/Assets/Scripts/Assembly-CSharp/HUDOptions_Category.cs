using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDOptions_Category : MonoBehaviour
{
	public enum ButtonContext
	{
		NEUTRAL = 0,
		FRIENDLY = 1,
		HOSTILE = 2
	}

	public HUDOptions_ToggleButton maximizeButton;

	public bool friendlyFaction;

	public List<UnitDefinition> listUnitTypes = new List<UnitDefinition>();

	public bool maximized = true;

	public ButtonContext context;

	public TextMeshProUGUI label;

	private void Start()
	{
		HUDOptions_Category_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += HUDOptions_Category_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= HUDOptions_Category_OnThemeGroupChanged;
	}

	public void Set(bool prio)
	{
		if (prio != maximized)
		{
			maximized = prio;
			maximizeButton.Set(maximized);
		}
	}

	public bool CheckFaction(FactionHQ hq)
	{
		bool result = false;
		if (SceneSingleton<DynamicMap>.i.HQ != null && ((hq == SceneSingleton<DynamicMap>.i.HQ && friendlyFaction) || (hq != SceneSingleton<DynamicMap>.i.HQ && !friendlyFaction) || hq == null))
		{
			result = true;
		}
		return result;
	}

	public bool CheckType(UnitDefinition unitType)
	{
		bool result = false;
		if (listUnitTypes.Count > 0 && unitType.GetType() == listUnitTypes[0].GetType())
		{
			result = true;
		}
		return result;
	}

	private void HUDOptions_Category_OnThemeGroupChanged()
	{
		if (context != ButtonContext.NEUTRAL)
		{
			Color color = context switch
			{
				ButtonContext.FRIENDLY => ThemeManager.Active.ColorTheme.MapIconFriendly, 
				ButtonContext.HOSTILE => ThemeManager.Active.ColorTheme.MapIconHostile, 
				_ => ThemeManager.Active.ColorTheme.AllClear, 
			};
			GetComponent<Image>().color = color;
			label.color = color;
		}
	}
}
