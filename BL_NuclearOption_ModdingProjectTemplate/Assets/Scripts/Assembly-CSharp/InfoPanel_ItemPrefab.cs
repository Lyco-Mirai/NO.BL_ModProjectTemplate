using System.Collections.Generic;
using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel_ItemPrefab : MonoBehaviour
{
	private Player player;

	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private TextMeshProUGUI value;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private List<UnitDefinition> listUnitDefinitions = new List<UnitDefinition>();

	private float numberValue;

	private InfoPanel_Faction factionInfoPanel;

	private Color positiveColor = Color.green;

	private Color zeroColor = Color.grey;

	private Color negativeColor = Color.red;

	private void Start()
	{
		InfoPanel_ItemPrefab_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += InfoPanel_ItemPrefab_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= InfoPanel_ItemPrefab_OnThemeGroupChanged;
	}

	public void SetFaction(InfoPanel_Faction panel)
	{
		factionInfoPanel = panel;
	}

	public void SetTextIcon(string name, Sprite image, bool fit = false)
	{
		string text = name.Replace("_", "\n");
		this.text.text = text;
		this.text.enableAutoSizing = fit;
		if (fit)
		{
			this.text.fontSizeMin = 24f;
			this.text.fontSizeMax = 32f;
			this.text.enableWordWrapping = true;
		}
		if (image != null)
		{
			icon.enabled = true;
			icon.sprite = image;
		}
		else
		{
			icon.enabled = false;
		}
	}

	public void SetListDefinitions(List<UnitDefinition> list)
	{
		listUnitDefinitions.Clear();
		listUnitDefinitions.AddRange(list);
	}

	public void AddDefinition(UnitDefinition item)
	{
		if (!listUnitDefinitions.Contains(item))
		{
			listUnitDefinitions.Add(item);
		}
	}

	public void Refresh(float? number, bool isLoss, bool isValue)
	{
		if (number.HasValue)
		{
			numberValue = number.Value;
			value.text = (isValue ? UnitConverter.ValueReading(number.Value) : number.ToString());
			SetColor(number.Value, isLoss);
		}
		else
		{
			numberValue = 0f;
			value.text = (isValue ? "0" : "-");
		}
	}

	public void RefreshDefinition(InfoPanel_Faction.DisplayType type)
	{
		if (listUnitDefinitions.Count == 0)
		{
			return;
		}
		int num = 0;
		bool isLoss = false;
		for (int i = 0; i < listUnitDefinitions.Count; i++)
		{
			UnitDefinition unitDefinition = listUnitDefinitions[i];
			switch (type)
			{
			case InfoPanel_Faction.DisplayType.Losses:
				num += factionInfoPanel.factionHQ.missionStatsTracker.GetLostUnits(unitDefinition);
				isLoss = true;
				break;
			case InfoPanel_Faction.DisplayType.Reserves:
				num += factionInfoPanel.factionHQ.GetUnitSupply(unitDefinition);
				break;
			case InfoPanel_Faction.DisplayType.Forces:
				num += factionInfoPanel.factionHQ.missionStatsTracker.GetCurrentUnits(unitDefinition);
				break;
			}
		}
		Refresh(num, isLoss, isValue: false);
	}

	public void SetColor(float number, bool isLoss)
	{
		Color color = (isLoss ? ((number > 0f) ? negativeColor : zeroColor) : ((number > 0f) ? positiveColor : zeroColor));
		icon.color = color;
		text.color = color;
		value.color = color;
	}

	public float GetValue()
	{
		return numberValue;
	}

	private void InfoPanel_ItemPrefab_OnThemeGroupChanged()
	{
		positiveColor = ThemeManager.Active.ColorTheme.AllClear;
		negativeColor = ThemeManager.Active.ColorTheme.Alert;
	}
}
