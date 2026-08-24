using NuclearOption.SavedMission;

namespace NuclearOption.MissionEditorScripts
{
	public interface IMissionEditorFileMenu
	{
		MissionEditorLoadMenuV2ListItem SubItemPrefab { get; }

		void LoadMission(MissionKey key);

		void SelectMission(MissionKey key);
	}
}
