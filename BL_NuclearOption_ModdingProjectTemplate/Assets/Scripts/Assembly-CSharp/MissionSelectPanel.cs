using System;
using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using Unity.Profiling;
using UnityEngine;

public class MissionSelectPanel : MonoBehaviour
{
	private static readonly ProfilerMarker setActiveGroupMarker = new ProfilerMarker("MissionSelectPanel.SetActiveGroup");

	private static readonly ProfilerMarker refreshListsMarker = new ProfilerMarker("MissionSelectPanel.RefreshLists");

	private static readonly ProfilerMarker filterByTagsMarker = new ProfilerMarker("MissionSelectPanel.FilterByTags");

	private static readonly ProfilerMarker countMissionsMarker = new ProfilerMarker("MissionSelectPanel.CountMissions");

	private static readonly ProfilerMarker setPickerFilterMarker = new ProfilerMarker("MissionSelectPanel.SetPickerFilter");

	private static readonly ProfilerMarker setSelectedMissionMarker = new ProfilerMarker("MissionSelectPanel.SetSelectedMission");

	[SerializeField]
	private MissionSelectList missionSelectList;

	[SerializeField]
	private TagFilterList tagFilterList;

	private MissionGroup activeGroup;

	private readonly HashSet<MissionTag> enabledFilters = new HashSet<MissionTag>();

	private readonly List<MissionTag> allTags = new List<MissionTag>();

	private readonly List<(MissionKey key, MissionQuickLoad mission)> allMissions = new List<(MissionKey, MissionQuickLoad)>();

	private readonly List<(MissionKey key, MissionQuickLoad mission)> filteredMissions = new List<(MissionKey, MissionQuickLoad)>();

	private readonly List<MissionGroup> disallowedGroups = new List<MissionGroup>();

	private readonly HashSet<MissionTag> requiredTags = new HashSet<MissionTag>();

	public MissionKey SelectedMission { get; private set; }

	public event Action<MissionKey> OnMissionSelecteed;

	public void SetSelectedMission(MissionKey item)
	{
		using (setSelectedMissionMarker.Auto())
		{
			SelectedMission = item;
			this.OnMissionSelecteed?.Invoke(item);
			missionSelectList.Redraw();
		}
	}

	public void SetActiveGroup(MissionGroup group)
	{
		using (setActiveGroupMarker.Auto())
		{
			activeGroup = group;
			allMissions.Clear();
			IEnumerable<MissionKey> items = from x in @group.GetMissions()
				where !disallowedGroups.Contains(x.Group)
				select x;
			allMissions.AddRange(MissionSaveLoad.QuickLoadMany(items));
			allTags.Clear();
			allTags.AddRange(from x in allMissions.SelectMany(((MissionKey key, MissionQuickLoad mission) x) => x.mission.missionSettings.Tags).Concat(MissionTag.InternalTags).Distinct()
				orderby x.SortOrder
				select x);
			RefreshLists();
		}
	}

	private void RefreshLists()
	{
		using (refreshListsMarker.Auto())
		{
			enabledFilters.UnionWith(requiredTags);
			filteredMissions.Clear();
			filteredMissions.AddRange(allMissions.Where(FilterByTags));
			missionSelectList.UpdateList(filteredMissions.Select(((MissionKey key, MissionQuickLoad mission) x) => new MissionSelectListItem.Item(this, x.key, x.mission)));
			tagFilterList.UpdateList(from x in allTags
				select (tag: x, enabled: enabledFilters.Contains(x), count: CountMissions(x)) into x
				select new TagFilterListItem.Item(x.tag, TagFilterClicked, x.enabled, x.count));
		}
	}

	private bool FilterByTags((MissionKey key, MissionQuickLoad mission) item)
	{
		using (filterByTagsMarker.Auto())
		{
			List<MissionTag> tags = item.mission.missionSettings.Tags;
			foreach (MissionTag filter in enabledFilters)
			{
				if (!tags.Any((MissionTag x) => x.Equals(filter)))
				{
					return false;
				}
			}
			return true;
		}
	}

	private int CountMissions(MissionTag tag)
	{
		using (countMissionsMarker.Auto())
		{
			return filteredMissions.Count(((MissionKey key, MissionQuickLoad mission) x) => x.mission.missionSettings.Tags.Any((MissionTag missionTag) => missionTag.Equals(tag)));
		}
	}

	private void TagFilterClicked(MissionTag tag)
	{
		if (!requiredTags.Contains(tag))
		{
			EnableTag(tag, !enabledFilters.Contains(tag));
		}
	}

	private void EnableTag(MissionTag tag, bool enable)
	{
		if (enable)
		{
			enabledFilters.Add(tag);
		}
		else
		{
			enabledFilters.Remove(tag);
		}
		RefreshLists();
	}

	public void SetPickerFilter(MissionsPicker.Filter filter)
	{
		using (setPickerFilterMarker.Auto())
		{
			if (filter.DisallowedGroups != null)
			{
				disallowedGroups.AddRange(filter.DisallowedGroups);
			}
			if (filter.RequiredTags != null)
			{
				requiredTags.UnionWith(filter.RequiredTags);
			}
			if (activeGroup != null)
			{
				SetActiveGroup(activeGroup);
			}
		}
	}
}
