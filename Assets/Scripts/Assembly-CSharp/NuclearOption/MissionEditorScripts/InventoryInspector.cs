using System.Collections.Generic;
using System.Linq;
using NuclearOption.MissionEditorScripts.MultiSelect;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class InventoryInspector : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI title;

		[SerializeField]
		private TMP_Dropdown unitDropdown;

		[SerializeField]
		private Button addButton;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private InventoryInspectorItem itemPrefab;

		[SerializeField]
		private GameObject inventoryDifferentOverlay;

		private List<UnitDefinition> unitDropdownOptions = new List<UnitDefinition>();

		private MultiSelect<(SavedUnit saved, UnitStorage storage)> targets = new MultiSelect<(SavedUnit, UnitStorage)>();

		private List<InventoryInspectorItem> entries = new List<InventoryInspectorItem>();

		private void Awake()
		{
			addButton.onClick.AddListener(AddUnit);
			closeButton.onClick.AddListener(delegate
			{
				base.gameObject.SetActive(value: false);
			});
		}

		public void UnitPanelTargetsChanged(List<(SavedUnit saved, UnitStorage storage)> storageTargets)
		{
			if (storageTargets.Count == 0)
			{
				Debug.LogError("InventoryPanel should not be opened if there are no units with UnitStorage");
				return;
			}
			targets.ReplaceTargets(storageTargets);
			DestroyItems();
			bool flag = targets.AllTheSame(((SavedUnit saved, UnitStorage storage) x) => x.storage.GetStoredList(), (UnitCount a, UnitCount b) => a.UnitType == b.UnitType && a.Count == b.Count) && targets.AllTheSame(((SavedUnit saved, UnitStorage storage) x) => CreatePossibleSupplyList(x.storage), (UnitDefinition a, UnitDefinition b) => a == b);
			inventoryDifferentOverlay.SetActive(!flag);
			if (flag)
			{
				title.text = ((targets.Targets.Count == 1) ? ("[" + targets.Targets[0].saved.UniqueName + "] inventory") : $"[{targets.Targets.Count} units] inventory");
				RefreshUI();
			}
			else
			{
				title.text = "no inventory";
				unitDropdown.ClearOptions();
				unitDropdownOptions.Clear();
			}
		}

		private void DestroyItems()
		{
			foreach (InventoryInspectorItem entry in entries)
			{
				if (entry != null)
				{
					entry.gameObject.SetActive(value: false);
					Object.Destroy(entry.gameObject);
				}
			}
			entries.Clear();
		}

		private void RefreshUI()
		{
			UnitStorage item = targets.Targets[0].storage;
			List<UnitCount> storedList = item.GetStoredList();
			List<UnitDefinition> possibleUnits = CreatePossibleSupplyList(item);
			int? num = storedList?.Count;
			PopulateDropdown(storedList, possibleUnits);
			bool flag = false;
			while (entries.Count > num)
			{
				InventoryInspectorItem inventoryInspectorItem = entries.Last();
				if (inventoryInspectorItem != null)
				{
					inventoryInspectorItem.gameObject.SetActive(value: false);
					Object.Destroy(inventoryInspectorItem.gameObject);
				}
				entries.RemoveAt(entries.Count - 1);
				flag = true;
			}
			while (entries.Count < num)
			{
				InventoryInspectorItem item2 = Object.Instantiate(itemPrefab, base.transform);
				entries.Add(item2);
				flag = true;
			}
			if (flag)
			{
				FixLayout.RebuildRoot(base.gameObject);
				FixLayout.RebuildRootEndOfFrame(base.gameObject);
			}
			for (int i = 0; i < storedList?.Count; i++)
			{
				UnitCount supply = storedList[i];
				entries[i].SetEntry(this, supply);
			}
		}

		private static List<UnitDefinition> CreatePossibleSupplyList(UnitStorage storage)
		{
			List<UnitDefinition> list = new List<UnitDefinition>();
			foreach (UnitDefinition value in Encyclopedia.Lookup.Values)
			{
				if (value.IsAllowed(MissionManager.AllowEventContent) && storage.CanFit(value))
				{
					list.Add(value);
				}
			}
			return list;
		}

		private void PopulateDropdown(IReadOnlyList<UnitCount> currentSupply, List<UnitDefinition> possibleUnits)
		{
			unitDropdown.ClearOptions();
			unitDropdownOptions.Clear();
			foreach (UnitDefinition def in possibleUnits)
			{
				if (currentSupply == null || !currentSupply.Any((UnitCount x) => x.UnitType == def.jsonKey))
				{
					unitDropdownOptions.Add(def);
					unitDropdown.options.Add(new TMP_Dropdown.OptionData(def.unitName, def.friendlyIcon));
				}
			}
			addButton.interactable = unitDropdown.options.Count > 0;
			unitDropdown.SetValueWithoutNotify(0);
			unitDropdown.RefreshShownValue();
		}

		public void AddUnit()
		{
			if (unitDropdownOptions.Count == 0)
			{
				Debug.LogError("Add clicked when there are no options in the dropdown");
				return;
			}
			UnitDefinition unitDefinition = unitDropdownOptions[unitDropdown.value];
			foreach (var target in targets.Targets)
			{
				target.storage.AddOrRemoveUnitEditor(target.saved, unitDefinition, 1);
			}
			RefreshUI();
		}

		public void AddOne(InventoryInspectorItem entry)
		{
			foreach (var target in targets.Targets)
			{
				target.storage.AddOrRemoveUnitEditor(target.saved, entry.UnitDefinition, 1);
			}
			RefreshUI();
		}

		public void RemoveOne(InventoryInspectorItem entry)
		{
			foreach (var target in targets.Targets)
			{
				target.storage.AddOrRemoveUnitEditor(target.saved, entry.UnitDefinition, -1);
			}
			RefreshUI();
		}
	}
}
