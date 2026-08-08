namespace NuclearOption.SavedMission
{
	public static class EditorMissionGroupHelper
	{
		public static bool IsUserOrEditorGroup(this MissionKey? key)
		{
			if (key.HasValue)
			{
				return key.Value.Group.IsUserOrEditorGroup();
			}
			return false;
		}

		public static bool IsUserOrEditorGroup(this MissionKey key)
		{
			return key.Group.IsUserOrEditorGroup();
		}

		public static bool IsUserOrEditorGroup(this MissionGroup group)
		{
			if (group is MissionGroup.UserGroup || group is EditorMissionGroup)
			{
				return true;
			}
			return false;
		}
	}
}
