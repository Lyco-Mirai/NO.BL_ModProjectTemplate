using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionSettingTabManager : MonoBehaviour
	{
		[SerializeField]
		private Button openParametersTab;

		[SerializeField]
		private Button openEnvironmentTab;

		[SerializeField]
		private Button openFactionTab;

		[SerializeField]
		private Button openRestrictionsTab;

		[SerializeField]
		private PanelScrollView panel;

		[SerializeField]
		private Color normalColor;

		[SerializeField]
		private Color openColor;

		[SerializeField]
		private MissionSettingsTab parametersTab;

		[SerializeField]
		private EnvironmentTab environmentTab;

		[SerializeField]
		private FactionSettingsTab factionTab;

		[SerializeField]
		private RestrictionsTab restrictionsTab;

		private Button activeButton;

		private IMissionTab activeTab;

		private Mission mission;

		private void Awake()
		{
			openParametersTab.onClick.AddListener(OpenParameters);
			openEnvironmentTab.onClick.AddListener(OpenEnvironment);
			openFactionTab.onClick.AddListener(OpenFaction);
			openRestrictionsTab.onClick.AddListener(OpenRestrictions);
			MissionManager.OnEditorOrPickerMissionChanged += MissionManager_OnEditorOrPickerMissionChanged;
			SetTabActive(parametersTab, active: false);
			SetTabActive(environmentTab, active: false);
			SetTabActive(restrictionsTab, active: false);
			SetTabActive(factionTab, active: false);
		}

		private void Start()
		{
			OpenParameters();
		}

		private void OnDestroy()
		{
			MissionManager.OnEditorOrPickerMissionChanged -= MissionManager_OnEditorOrPickerMissionChanged;
		}

		private void MissionManager_OnEditorOrPickerMissionChanged(Mission mission)
		{
			this.mission = mission;
			activeTab?.SetMission(mission);
		}

		private void SetActive<T>(Button button, T tab) where T : MonoBehaviour, IMissionTab
		{
			if (activeButton != null)
			{
				activeButton.image.color = normalColor;
			}
			activeButton = button;
			activeButton.image.color = openColor;
			if (activeTab != null)
			{
				SetTabActive((MonoBehaviour)activeTab, active: false);
			}
			activeTab = tab;
			activeTab.SetMission(mission);
			SetTabActive(tab, active: true);
			panel.SetChild(tab.transform.AsRectTransform());
		}

		private void SetTabActive(MonoBehaviour tab, bool active)
		{
			tab.gameObject.SetActive(active);
		}

		private void OpenParameters()
		{
			SetActive(openParametersTab, parametersTab);
		}

		private void OpenEnvironment()
		{
			SetActive(openEnvironmentTab, environmentTab);
		}

		private void OpenFaction()
		{
			SetActive(openFactionTab, factionTab);
		}

		private void OpenRestrictions()
		{
			SetActive(openRestrictionsTab, restrictionsTab);
		}
	}
}
