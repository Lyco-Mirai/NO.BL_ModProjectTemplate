using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;

namespace NuclearOption.MissionEditorScripts
{
	public static class DataDrawerHelper
	{
		public static ReferenceList DrawList<T>(this DataDrawer drawer, int height, List<T> list, string typeName, Func<IEnumerable<T>> getAllOptions, Action<int> readonlyListEditCallback = null) where T : ISaveableReference
		{
			return drawer.DrawList(height, "Target " + typeName + "s", "Select " + typeName, list, (T x) => x.ToUIString(oneLine: true), (T x) => x.ToUIString(), getAllOptions, readonlyListEditCallback);
		}

		public static ReferenceList DrawList(this DataDrawer drawer, int height, List<Objective> list)
		{
			Action<int> readonlyListEditCallback = ((drawer.Options.Context == PanelDrawContext.GraphEditor) ? ((Action<int>)delegate(int index)
			{
				drawer.Options.OnRequestSelectAndFocus?.Invoke(list[index]);
			}) : null);
			return drawer.DrawList(height, list, "Objective", () => MissionManager.Objectives.AllObjectives, readonlyListEditCallback);
		}

		public static ReferenceList DrawList(this DataDrawer drawer, int height, List<Outcome> list)
		{
			Action<int> readonlyListEditCallback = ((drawer.Options.Context == PanelDrawContext.GraphEditor) ? ((Action<int>)delegate(int index)
			{
				drawer.Options.OnRequestSelectAndFocus?.Invoke(list[index]);
			}) : null);
			return drawer.DrawList(height, list, "Outcome", () => MissionManager.Objectives.AllOutcomes, readonlyListEditCallback);
		}

		public static ReferenceList DrawList(this DataDrawer drawer, int height, List<SavedUnit> list, bool includeBuiltIn)
		{
			return drawer.DrawList(height, list, "Unit", GetAllOptions);
			List<SavedUnit> GetAllOptions()
			{
				List<SavedUnit> list2 = new List<SavedUnit>();
				MissionManager.GetAllSavedUnitsNonAlloc(list2, includeBuiltIn);
				for (int num = list2.Count - 1; num >= 0; num--)
				{
					if (string.IsNullOrEmpty(list2[num].UniqueName))
					{
						list2.RemoveAt(num);
					}
				}
				return list2;
			}
		}

		public static ReferenceList DrawList(this DataDrawer drawer, int height, List<SavedAirbase> list)
		{
			return drawer.DrawList(height, list, "Airbase", delegate
			{
				List<SavedAirbase> list2 = new List<SavedAirbase>();
				MissionManager.GetAllSavedAirbaseNonAlloc(list2);
				return list2;
			});
		}

		public static DropdownDataField DrawFactionDropdown(this DataDrawer drawer, string label, string value, Action<string> setValue)
		{
			List<string> factionsAndNeutral = FactionHelper.GetFactionsAndNeutral();
			return drawer.DrawDropdown(label, factionsAndNeutral, value, setValue);
		}
	}
}
