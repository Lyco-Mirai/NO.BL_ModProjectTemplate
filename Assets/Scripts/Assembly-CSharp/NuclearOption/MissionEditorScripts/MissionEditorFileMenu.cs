using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorFileMenu : MonoBehaviour
	{
		[Header("References")]
		[SerializeField]
		private GameObject root;

		[SerializeField]
		private NavbarTabs navbar;

		[Header("Tab Indices")]
		[SerializeField]
		private int newMissionTabIndex;

		[SerializeField]
		private int loadMissionTabIndex = 1;

		[SerializeField]
		private int saveMissionTabIndex = 2;

		[SerializeField]
		private int settingsTabIndex = 3;

		public void OpenNewMission()
		{
			root.SetActive(value: true);
			navbar.SelectTab(newMissionTabIndex);
		}

		public void OpenLoadMenu()
		{
			root.SetActive(value: true);
			navbar.SelectTab(loadMissionTabIndex);
		}

		public void OpenSaveMenu()
		{
			root.SetActive(value: true);
			navbar.SelectTab(saveMissionTabIndex);
		}

		public void OpenSettings()
		{
			root.SetActive(value: true);
			navbar.SelectTab(settingsTabIndex);
		}
	}
}
