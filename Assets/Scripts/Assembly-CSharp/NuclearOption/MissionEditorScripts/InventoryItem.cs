using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class InventoryItem : MonoBehaviour
	{
		[SerializeField]
		private Text unitName;

		private IReadOnlyList<MissionFaction> selectedFactions;

		private int supplyIndex;

		private UnitDefinition definition;

		private FactionSettingsTab factionSettingsTab;

		private int count;

		public void SetInventoryItem(int supplyIndex, UnitCount factionSupply, IReadOnlyList<MissionFaction> selectedFactions, FactionSettingsTab factionSettingsTab)
		{
			this.selectedFactions = selectedFactions;
			this.factionSettingsTab = factionSettingsTab;
			this.supplyIndex = supplyIndex;
			count = factionSupply.Count;
			definition = Encyclopedia.Lookup[factionSupply.UnitType];
			RefreshDisplay();
		}

		public void RefreshDisplay()
		{
			unitName.text = $"{definition.unitName}[{count}]";
		}

		public void AddOne()
		{
			count++;
			foreach (MissionFaction selectedFaction in selectedFactions)
			{
				selectedFaction.supplies[supplyIndex].Count = count;
			}
			RefreshDisplay();
		}

		public void RemoveOne()
		{
			count--;
			foreach (MissionFaction selectedFaction in selectedFactions)
			{
				selectedFaction.supplies[supplyIndex].Count = count;
			}
			RefreshDisplay();
			if (count > 0)
			{
				return;
			}
			bool flag = false;
			foreach (MissionFaction selectedFaction2 in selectedFactions)
			{
				foreach (UnitCount supply in selectedFaction2.supplies)
				{
					if (supply.UnitType == definition.jsonKey)
					{
						selectedFaction2.supplies.Remove(supply);
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				Object.Destroy(base.gameObject);
				factionSettingsTab.RemoveSupplyEntry(this);
			}
		}
	}
}
