using NuclearOption.MissionEditorScripts;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

public class CustomizeMissionMenu : MonoBehaviour
{
	[SerializeField]
	private MissionsPicker missionsPicker;

	[SerializeField]
	private Button openButton;

	[SerializeField]
	private Button closeButton;

	[SerializeField]
	private GameObject holder;

	[SerializeField]
	private MissionSettingsTab parametersTab;

	[SerializeField]
	private EnvironmentTab environmentTab;

	[SerializeField]
	private FactionSettingsTab factionTab;

	private Mission mission;

	private void Awake()
	{
		openButton.onClick.AddListener(OpenCustomizeMenu);
		closeButton.onClick.AddListener(CloseCustomizeMenu);
		holder.SetActive(value: false);
		missionsPicker.OnMissionSelect += SetMission;
		SetMission(null);
	}

	private void OpenCustomizeMenu()
	{
		holder.SetActive(value: true);
		parametersTab.SetMission(mission);
		environmentTab.SetMission(mission);
		factionTab.SetMission(mission);
	}

	private void CloseCustomizeMenu()
	{
		holder.SetActive(value: false);
		if (MissionSaveLoad.SaveMissionTemp(missionsPicker.Mission, "CurrentMission", runBeforeSave: false, out var missionCopy, out var loadErrors))
		{
			missionsPicker.SetMissionWithoutNotify(missionCopy);
		}
		else
		{
			Debug.LogError("Failed to save copy:\n" + loadErrors);
		}
	}

	public void SetMission(Mission mission)
	{
		openButton.interactable = mission != null;
		this.mission = mission;
	}
}
