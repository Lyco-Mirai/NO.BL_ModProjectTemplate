using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class RestrictionsTab : MonoBehaviour, IMissionTab
	{
		[SerializeField]
		private Dropdown factionDropdown;

		[SerializeField]
		private Dropdown weaponDropdown;

		[SerializeField]
		private Dropdown aircraftDropdown;

		[SerializeField]
		private GameObject restrictedItemPrefab;

		private MissionFaction selectedFaction;

		[SerializeField]
		private Transform restrictedWeaponsTransform;

		[SerializeField]
		private Transform restrictedAircraftTransform;

		private List<RestrictedItem> displayedRestrictions = new List<RestrictedItem>();

		private Dictionary<string, UnitDefinition> aircraftNameLookup = new Dictionary<string, UnitDefinition>();

		public void SetMission(Mission mission)
		{
			factionDropdown.options.Clear();
			foreach (MissionFaction faction in MissionManager.CurrentMission.factions)
			{
				factionDropdown.options.Add(new Dropdown.OptionData(faction.factionName));
			}
			aircraftNameLookup.Clear();
			foreach (AircraftDefinition item in Encyclopedia.i.aircraft)
			{
				aircraftNameLookup.Add(item.unitName, item);
			}
			factionDropdown.SetValueWithoutNotify(0);
			SelectFaction();
		}

		public void SelectFaction()
		{
			string text = factionDropdown.options[factionDropdown.value].text;
			if (MissionManager.CurrentMission.TryGetFaction(text, out var faction))
			{
				selectedFaction = faction;
			}
			else
			{
				Debug.LogWarning("Failed to find faction with name: " + text);
			}
			RefreshDisplay();
		}

		private void RefreshDisplay()
		{
			foreach (RestrictedItem displayedRestriction in displayedRestrictions)
			{
				if (displayedRestriction != null)
				{
					Object.Destroy(displayedRestriction.gameObject);
				}
			}
			displayedRestrictions.Clear();
			weaponDropdown.ClearOptions();
			aircraftDropdown.ClearOptions();
			foreach (WeaponMount weaponMount in Encyclopedia.i.weaponMounts)
			{
				if (selectedFaction.restrictions.weapons.Contains(weaponMount.name))
				{
					RestrictedItem component = Object.Instantiate(restrictedItemPrefab, restrictedWeaponsTransform).GetComponent<RestrictedItem>();
					component.SetItem(weaponMount, this);
					displayedRestrictions.Add(component);
				}
				else if (weaponMount.IsAllowed(MissionManager.AllowEventContent))
				{
					weaponDropdown.options.Add(new Dropdown.OptionData(weaponMount.name));
				}
			}
			foreach (AircraftDefinition item in Encyclopedia.i.aircraft)
			{
				if (selectedFaction.restrictions.aircraft.Contains(item.jsonKey))
				{
					RestrictedItem component2 = Object.Instantiate(restrictedItemPrefab, restrictedAircraftTransform).GetComponent<RestrictedItem>();
					component2.SetItem(item, this);
					displayedRestrictions.Add(component2);
				}
				else if (item.IsAllowed(MissionManager.AllowEventContent))
				{
					aircraftDropdown.options.Add(new Dropdown.OptionData(item.unitName));
				}
			}
			weaponDropdown.SetValueWithoutNotify(0);
			weaponDropdown.RefreshShownValue();
			aircraftDropdown.SetValueWithoutNotify(0);
			aircraftDropdown.RefreshShownValue();
		}

		public void RestrictWeapon()
		{
			if (weaponDropdown.options.Count > 0)
			{
				selectedFaction.restrictions.weapons.Add(weaponDropdown.options[weaponDropdown.value].text);
			}
			RefreshDisplay();
		}

		public void RestrictAircraft()
		{
			if (aircraftDropdown.options.Count > 0)
			{
				string text = aircraftDropdown.options[aircraftDropdown.value].text;
				UnitDefinition unitDefinition = aircraftNameLookup[text];
				selectedFaction.restrictions.aircraft.Add(unitDefinition.jsonKey);
			}
			RefreshDisplay();
		}

		public void UnrestrictWeapon(WeaponMount weaponMount, RestrictedItem restrictedItem)
		{
			Object.Destroy(restrictedItem.gameObject);
			selectedFaction.restrictions.weapons.Remove(weaponMount.name);
			RefreshDisplay();
		}

		public void UnrestrictAircraft(UnitDefinition unitDefinition, RestrictedItem restrictedItem)
		{
			Object.Destroy(restrictedItem.gameObject);
			selectedFaction.restrictions.aircraft.Remove(unitDefinition.jsonKey);
			RefreshDisplay();
		}
	}
}
