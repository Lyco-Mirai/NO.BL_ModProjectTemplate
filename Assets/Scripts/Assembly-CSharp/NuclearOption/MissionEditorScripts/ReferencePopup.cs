using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class ReferencePopup : MonoBehaviour
	{
		[SerializeField]
		private GameObject holder;

		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private TextMeshProUGUI noOptionsText;

		[SerializeField]
		private Button cancelDropdownButton;

		[SerializeField]
		private TMP_Dropdown allUnitsDropdown;

		[SerializeField]
		public GameObject filterHolder;

		[SerializeField]
		private DropdownDataField factionFilter;

		[SerializeField]
		private DropdownDataField unitTypeFilter;

		[SerializeField]
		private DropdownDataField placementTypeFilter;

		public readonly FilterSet FilterSet = new FilterSet();

		private ISaveableReference startingOption;

		private GetAllOptions getAllOptions;

		private ReferenceToString toDropdownString;

		private PickOrCancel pickOrCancelCallback;

		private bool allowNone;

		private bool filtersSetup;

		private int optionOffset;

		private bool placementFilterIncludeAttached;

		private bool hideUnitTypeFilter;

		private bool hideFactionFilter;

		private bool hidePlacementTypeFilter;

		private readonly List<string> optionNames = new List<string>();

		private readonly List<ISaveableReference> optionValues = new List<ISaveableReference>();

		public GameObject Holder => holder;

		private void CheckFilterSetup()
		{
			if (!filtersSetup)
			{
				FilterSet.SetupFilterUnitType(unitTypeFilter, null, FilterSet);
				FilterSet.SetupFilterFaction(factionFilter, null, FilterSet);
				FilterSet.SetupFilterPlacement(placementTypeFilter, null, FilterSet, placementFilterIncludeAttached);
				filtersSetup = true;
			}
		}

		public void HideFactionFilter(bool hide = true)
		{
			HideFilter(hide, ref hideFactionFilter, factionFilter);
		}

		public void HideUnitTypeFilter(bool hide = true)
		{
			HideFilter(hide, ref hideUnitTypeFilter, unitTypeFilter);
		}

		public void HidePlacementTypeFilter(bool hide = true)
		{
			HideFilter(hide, ref hidePlacementTypeFilter, placementTypeFilter);
		}

		private void HideFilter(bool hide, ref bool hideField, DropdownDataField filter)
		{
			hideField = hide;
			if (hide)
			{
				filter.gameObject.SetActive(value: false);
			}
			else
			{
				ApplyFilter();
			}
		}

		public void SetTitle(string title)
		{
			titleText.text = title;
		}

		private void Awake()
		{
			if (holder == null)
			{
				holder = base.gameObject;
			}
			FilterSet.OnFilterChanged += Refresh;
			cancelDropdownButton.onClick.AddListener(CancelPressed);
			allUnitsDropdown.onValueChanged.AddListener(DropdownValueChanged);
		}

		public void ShowPickOption(ISaveableReference startingOption, bool allowNone, GetAllOptions getAllOptions, ReferenceToString toDropdownString, PickOrCancel pickOrCancelCallback)
		{
			this.startingOption = startingOption;
			this.getAllOptions = getAllOptions;
			this.toDropdownString = toDropdownString;
			this.pickOrCancelCallback = pickOrCancelCallback;
			this.allowNone = allowNone;
			placementFilterIncludeAttached = getAllOptions().All((ISaveableReference x) => x is IHasPlacementType hasPlacementType && hasPlacementType.CanBeAttached);
			Refresh();
			Show();
		}

		private void Show()
		{
			holder.SetActive(value: true);
			unitTypeFilter.Interactable = true;
			factionFilter.Interactable = true;
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)base.transform);
		}

		public void Hide()
		{
			holder.SetActive(value: false);
		}

		private void CancelPressed()
		{
			pickOrCancelCallback(pick: false, null);
			Hide();
		}

		private void DropdownValueChanged(int index)
		{
			int num = index - optionOffset;
			ISaveableReference obj = ((num >= 0) ? optionValues[num] : null);
			pickOrCancelCallback(pick: true, obj);
			Hide();
		}

		private void Refresh()
		{
			if (getAllOptions != null)
			{
				ApplyFilter();
				bool flag = optionNames.Count > 0;
				noOptionsText.gameObject.SetActive(!flag);
				allUnitsDropdown.gameObject.SetActive(flag);
				if (flag)
				{
					optionOffset = SetOptions(startingOption);
				}
			}
		}

		private void ApplyFilter()
		{
			CheckFilterSetup();
			IEnumerable<ISaveableReference> enumerable = getAllOptions();
			int num = enumerable.Count();
			optionNames.Clear();
			optionValues.Clear();
			bool flag = num > 0;
			bool flag2 = num > 0;
			bool flag3 = num > 0;
			foreach (ISaveableReference item in enumerable)
			{
				if (!(item is SavedUnit))
				{
					flag = false;
				}
				if (!(item is IHasFaction))
				{
					flag2 = false;
				}
				if (!(item is IHasPlacementType))
				{
					flag3 = false;
				}
				if (item != null)
				{
					ISaveableReference saveableReference = item;
					if (!saveableReference.CanBeReference)
					{
						continue;
					}
				}
				if (FilterSet.FilterItem(item))
				{
					optionValues.Add(item);
					optionNames.Add(toDropdownString(item));
				}
			}
			factionFilter.gameObject.SetActive(!hideFactionFilter && flag2);
			unitTypeFilter.gameObject.SetActive(!hideUnitTypeFilter && flag);
			placementTypeFilter.gameObject.SetActive(!hidePlacementTypeFilter && flag3);
		}

		private int SetOptions(ISaveableReference startingOption)
		{
			allUnitsDropdown.ClearOptions();
			int num = 0;
			if (allowNone || startingOption == null)
			{
				string text = (allowNone ? "<none>" : "<select>").AddColor(new Color(0.5f, 0.5f, 0.5f));
				allUnitsDropdown.options.Add(new TMP_Dropdown.OptionData(text));
				num++;
			}
			if (startingOption != null && optionValues.IndexOf(startingOption) != -1)
			{
				allUnitsDropdown.options.Add(new TMP_Dropdown.OptionData(toDropdownString(startingOption)));
				num++;
			}
			int valueWithoutNotify = ((startingOption != null) ? (num + optionValues.IndexOf(startingOption)) : 0);
			allUnitsDropdown.AddOptions(optionNames);
			allUnitsDropdown.SetValueWithoutNotify(valueWithoutNotify);
			return num;
		}
	}
}
