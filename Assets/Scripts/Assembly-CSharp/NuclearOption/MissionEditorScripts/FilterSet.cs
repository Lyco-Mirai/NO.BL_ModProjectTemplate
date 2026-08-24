using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class FilterSet
	{
		public delegate bool Filter(object item);

		public delegate void ApplyFilter(object key, Filter filter);

		public delegate void ClearFilter(object key);

		public delegate Filter FilterFromOption(string option);

		private readonly Dictionary<object, Filter> filters = new Dictionary<object, Filter>();

		public static readonly string noFilter = "<no filter>".AddColor(new Color(0.5f, 0.5f, 0.5f));

		public int Count => filters.Count;

		public event Action OnFilterChanged;

		public void Apply(object key, Filter filter)
		{
			filters[key] = filter;
			this.OnFilterChanged?.Invoke();
		}

		public void Clear(object key)
		{
			filters.Remove(key);
			this.OnFilterChanged?.Invoke();
		}

		public bool FilterItem(object item)
		{
			foreach (Filter value in filters.Values)
			{
				if (!value(item))
				{
					return false;
				}
			}
			return true;
		}

		public static void AddFilterFaction(Transform parent, FilterSet filterSet, DropdownDataField dropdownPrefab)
		{
			DropdownDataField dropdownDataField = UnityEngine.Object.Instantiate(dropdownPrefab, parent);
			dropdownDataField.transform.SetAsFirstSibling();
			SetupFilterFaction(dropdownDataField, "Faction Filter:", filterSet);
		}

		public static void AddFilterPlacement(Transform parent, FilterSet filterSet, DropdownDataField dropdownPrefab, bool includeAttached)
		{
			DropdownDataField dropdownDataField = UnityEngine.Object.Instantiate(dropdownPrefab, parent);
			dropdownDataField.transform.SetAsFirstSibling();
			SetupFilterPlacement(dropdownDataField, "Placement:", filterSet, includeAttached);
		}

		public static void AddFilterUnitType(Transform parent, FilterSet filterSet, DropdownDataField dropdownPrefab)
		{
			DropdownDataField dropdownDataField = UnityEngine.Object.Instantiate(dropdownPrefab, parent);
			dropdownDataField.transform.SetAsFirstSibling();
			SetupFilterUnitType(dropdownDataField, "Type Filter:", filterSet);
		}

		public static void AddFilterMapBuildings(Transform parent, FilterSet filterSet, BoolDataField togglePrefab)
		{
			BoolDataField boolDataField = UnityEngine.Object.Instantiate(togglePrefab, parent);
			boolDataField.transform.SetAsFirstSibling();
			SetupFilterMapObjects(boolDataField, "Include Map Buildings:", startingValue: false, filterSet);
		}

		public static void SetupFilterMapObjects(BoolDataField filter, string label, bool startingValue, FilterSet filterSet)
		{
			filter.Setup<ValueWrapperBool, bool>(label, startingValue, GetFilter);
			GetFilter(startingValue);
			void GetFilter(bool isOn)
			{
				if (isOn)
				{
					filterSet.Clear("ShowMapBuildings");
				}
				else
				{
					filterSet.Apply("ShowMapBuildings", HideMapBuildings);
				}
			}
			static bool HideMapBuildings(object obj)
			{
				return !(obj is SavedBuilding savedBuilding) || savedBuilding.PlacementType != PlacementType.BuiltIn;
			}
		}

		public static void SetupFilterPlacement(DropdownDataField filter, string label, FilterSet filterSet, bool includeAttached)
		{
			List<string> list = new List<string>(5);
			list.Add(noFilter);
			list.Add("BuiltIn");
			list.Add("Override");
			if (includeAttached)
			{
				list.Add("Attached");
			}
			list.Add("Custom");
			SetupFilter(filter, label, "PlacementType", filterSet, list, TypeFilter);
			static Filter TypeFilter(string option)
			{
				return delegate(object obj)
				{
					PlacementType placementType = ((IHasPlacementType)obj).PlacementType;
					return option switch
					{
						"BuiltIn" => placementType == PlacementType.BuiltIn, 
						"Override" => placementType == PlacementType.Override, 
						"Attached" => placementType == PlacementType.Attached, 
						"Custom" => placementType == PlacementType.Custom, 
						_ => false, 
					};
				};
			}
		}

		public static void SetupFilterFaction(DropdownDataField filter, string label, FilterSet filterSet)
		{
			List<string> options = new List<string> { noFilter, "None", "Boscali", "Primeva" };
			SetupFilter(filter, label, "FilterFaction", filterSet, options, FactionFilter);
			static Filter FactionFilter(string filterName)
			{
				return (object obj) => ((IHasFaction)obj).BelongsToFaction(filterName);
			}
		}

		public static void SetupFilterUnitType(DropdownDataField filter, string label, FilterSet filterSet)
		{
			List<string> options = new List<string> { noFilter, "Aircraft", "Buildings", "Ships", "Vehicles", "Scenery", "Containers", "Missiles", "Pilots" };
			SetupFilter(filter, label, "FilterUnitType", filterSet, options, UnitFilter);
			static Filter UnitFilter(string filterName)
			{
				return filterName switch
				{
					"Aircraft" => (object obj) => obj is SavedAircraft, 
					"Buildings" => (object obj) => obj is SavedBuilding, 
					"Ships" => (object obj) => obj is SavedShip, 
					"Vehicles" => (object obj) => obj is SavedVehicle, 
					"Scenery" => (object obj) => obj is SavedScenery, 
					"Containers" => (object obj) => obj is SavedContainer, 
					"Missiles" => (object obj) => obj is SavedMissile, 
					"Pilots" => (object obj) => obj is SavedPilot, 
					_ => throw new KeyNotFoundException("No type with name " + filterName), 
				};
			}
		}

		public static void SetupToggleFilter(BoolDataField field, string filterKey, string label, bool value, FilterSet filterSet, Filter filter)
		{
			ValueWrapperBool wrapper = ValueWrapper.FromCallback<ValueWrapperBool, bool>(value, OnSet);
			field.Setup(label, wrapper);
			OnSet(value);
			void OnSet(bool flag)
			{
				if (flag)
				{
					filterSet.Apply(filterKey, filter);
				}
				else
				{
					filterSet.Clear(filterKey);
				}
			}
		}

		private static void SetupFilter(DropdownDataField filter, string label, string filterKey, FilterSet filterSet, List<string> options, FilterFromOption filterFunc)
		{
			filter.Setup(label, options, null, delegate(string filterName)
			{
				if (filterName == noFilter)
				{
					filterSet.Clear(filterKey);
				}
				else
				{
					filterSet.Apply(filterKey, filterFunc(filterName));
				}
			});
		}
	}
}
