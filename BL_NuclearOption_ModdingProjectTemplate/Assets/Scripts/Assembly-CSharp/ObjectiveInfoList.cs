using System.Collections.Generic;
using NuclearOption.Networking.Lobbies;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveInfoList : SceneSingleton<ObjectiveInfoList>
{
	public MFDScreen screen;

	private List<Objective> activeObjectives = new List<Objective>();

	[SerializeField]
	private List<ObjectiveInfoList_ObjEntry> listObjectives = new List<ObjectiveInfoList_ObjEntry>();

	[SerializeField]
	private GameObject objectivePrefab;

	[SerializeField]
	private GameObject missionInfo;

	[SerializeField]
	private GameObject objectiveInfo;

	[SerializeField]
	private Transform container;

	[SerializeField]
	private TextMeshProUGUI missionName;

	[SerializeField]
	private TextMeshProUGUI missionTime;

	[SerializeField]
	private TextMeshProUGUI missionEscalation;

	[SerializeField]
	private TextMeshProUGUI missionDescription;

	[SerializeField]
	private Button missionButton;

	[SerializeField]
	private Button objectiveButton;

	[SerializeField]
	private TextMeshProUGUI missionButtonText;

	[SerializeField]
	private TextMeshProUGUI objectiveButtonText;

	private float lastRefresh;

	private float refreshDelay = 1f;

	private bool objectiveInitialized;

	private void Start()
	{
		MissionManager.onObjectiveStarted += AddObjectiveEntry;
		ShowMissionInfo();
	}

	private void Update()
	{
		if (!(Time.timeSinceLevelLoad < lastRefresh + refreshDelay))
		{
			if (!objectiveInitialized)
			{
				InitializeObjectiveList();
				InitializeMission();
			}
			UpdateMissionInfo();
			UpdateObjectiveInfo();
			lastRefresh = Time.timeSinceLevelLoad;
		}
	}

	public void UpdateMissionInfo()
	{
		if (MissionManager.Runner != null)
		{
			float currentEscalation = NetworkSceneSingleton<MissionManager>.i.currentEscalation;
			string arg = "Conventional";
			if (currentEscalation > NetworkSceneSingleton<MissionManager>.i.strategicThreshold)
			{
				arg = "Strategic";
			}
			else if (currentEscalation > NetworkSceneSingleton<MissionManager>.i.tacticalThreshold)
			{
				arg = "Tactical";
			}
			missionEscalation.text = $"Score {currentEscalation:F1}  --  {arg} level";
			missionTime.text = "Time " + UnitConverter.TimeOfDay(NetworkSceneSingleton<LevelInfo>.i.timeOfDay, includeSeconds: true) + "  --  Duration " + UnitConverter.TimeOfDay(NetworkSceneSingleton<MissionManager>.i.MissionTime / 3600f, includeSeconds: true);
		}
	}

	public void UpdateObjectiveInfo()
	{
		if (SceneSingleton<DynamicMap>.i.HQ == null || MissionManager.Runner == null)
		{
			if (objectiveButton.gameObject.activeSelf)
			{
				objectiveButton.gameObject.SetActive(value: false);
			}
		}
		else
		{
			if (listObjectives.Count == 0)
			{
				return;
			}
			MissionPosition.TryGetActiveObjectives(SceneSingleton<DynamicMap>.i.HQ, out activeObjectives);
			if (activeObjectives == null)
			{
				return;
			}
			if (!objectiveButton.gameObject.activeSelf)
			{
				objectiveButton.gameObject.SetActive(value: true);
			}
			foreach (ObjectiveInfoList_ObjEntry listObjective in listObjectives)
			{
				for (int i = 0; i < activeObjectives.Count; i++)
				{
					if (listObjective.objective == activeObjectives[i])
					{
						listObjective.Refresh(activeObjectives[i]);
					}
				}
			}
		}
	}

	public void OnEnable()
	{
	}

	private void InitializeMission()
	{
		if (MissionManager.Runner == null)
		{
			return;
		}
		if (GameManager.gameState == GameState.Multiplayer)
		{
			if (SteamLobby.instance != null)
			{
				missionName.text = SteamLobby.instance.CurrentLobbyName;
			}
		}
		else if (GameManager.gameState == GameState.SinglePlayer)
		{
			missionName.text = MissionManager.CurrentMission.Name;
		}
		missionDescription.text = MissionManager.CurrentMission.missionSettings.description;
	}

	private void InitializeObjectiveList()
	{
		if (SceneSingleton<DynamicMap>.i.HQ == null)
		{
			return;
		}
		MissionPosition.TryGetActiveObjectives(SceneSingleton<DynamicMap>.i.HQ, out activeObjectives);
		if (activeObjectives == null)
		{
			return;
		}
		if (activeObjectives.Count > 0)
		{
			for (int i = 0; i < activeObjectives.Count; i++)
			{
				if (activeObjectives[i] is IObjectiveWithPosition && !activeObjectives[i].SavedObjective.Hidden)
				{
					AddObjectiveEntry(activeObjectives[i]);
				}
			}
		}
		objectiveInitialized = true;
	}

	private void AddObjectiveEntry(Objective obj)
	{
		if (obj is IObjectiveWithPosition && !obj.SavedObjective.Hidden && obj.FactionHQ != null && SceneSingleton<DynamicMap>.i.HQ != null && obj.FactionHQ == SceneSingleton<DynamicMap>.i.HQ)
		{
			ObjectiveInfoList_ObjEntry component = Object.Instantiate(objectivePrefab, container).GetComponent<ObjectiveInfoList_ObjEntry>();
			component.SetObjective(obj);
			listObjectives.Add(component);
		}
	}

	private void OnDestroy()
	{
		if (listObjectives != null && listObjectives.Count > 0 && listObjectives.Count != activeObjectives.Count)
		{
			foreach (ObjectiveInfoList_ObjEntry listObjective in listObjectives)
			{
				Object.Destroy(listObjective.gameObject);
			}
			listObjectives.Clear();
		}
		MissionManager.onObjectiveStarted -= AddObjectiveEntry;
	}

	public void ShowMissionInfo()
	{
		missionInfo.gameObject.SetActive(value: true);
		objectiveInfo.gameObject.SetActive(value: false);
	}

	public void ShowObjectiveList()
	{
		if (!(SceneSingleton<DynamicMap>.i.HQ == null))
		{
			missionInfo.gameObject.SetActive(value: false);
			objectiveInfo.gameObject.SetActive(value: true);
		}
	}
}
