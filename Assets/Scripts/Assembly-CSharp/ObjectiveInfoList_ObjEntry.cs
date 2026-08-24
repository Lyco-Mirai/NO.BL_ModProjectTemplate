using System.Collections.Generic;
using NuclearOption.SavedMission;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveInfoList_ObjEntry : MonoBehaviour
{
	public Objective objective;

	public TextMeshProUGUI OBJ_Name;

	public TextMeshProUGUI OBJ_State;

	public TextMeshProUGUI OBJ_Complete;

	public Button toggleButton;

	[SerializeField]
	private Transform container;

	[SerializeField]
	private List<ObjectiveInfoList_Item> listPositions;

	[SerializeField]
	private ObjectiveInfoList_Item itemPrefab;

	private List<MissionPosition.PositionResult> resultCache = new List<MissionPosition.PositionResult>();

	public void SetObjective(Objective obj)
	{
		objective = obj;
		OBJ_Name.text = objective.SavedObjective.DisplayName;
		OBJ_State.text = "Active";
		OBJ_Complete.text = "0%";
		ObjectiveInfoList_ObjEntry_OnThemeGroupChanged();
		MissionManager.onObjectiveComplete += OnComplete;
		MissionManager.onObjectiveStarted += OnStarted;
	}

	private void Start()
	{
		ThemeManager.ThemeGroupChanged += ObjectiveInfoList_ObjEntry_OnThemeGroupChanged;
	}

	public void Refresh(Objective obj)
	{
		if (obj.Status == ObjectiveStatus.Complete)
		{
			OnComplete(obj);
			return;
		}
		OBJ_Complete.text = $"{100f * obj.CompletePercent:F0}%";
		MissionPosition.GetAllPositionsResults(SceneSingleton<DynamicMap>.i.HQ, Datum.originPosition.ToGlobalPosition(), includeHidden: false, resultCache);
		List<MissionPosition.PositionResult> list = new List<MissionPosition.PositionResult>();
		foreach (MissionPosition.PositionResult item in resultCache)
		{
			if (item.Objective == obj && !list.Contains(item))
			{
				list.Add(item);
			}
		}
		while (listPositions.Count < list.Count)
		{
			listPositions.Add(CreateItem());
		}
		for (int i = 0; i < listPositions.Count; i++)
		{
			ObjectiveInfoList_Item objectiveInfoList_Item = listPositions[i];
			if (i < list.Count && SceneSingleton<MapOptions>.i.showObjectives)
			{
				objectiveInfoList_Item.gameObject.SetActive(value: true);
				objectiveInfoList_Item.Refresh(list[i]);
			}
			else
			{
				objectiveInfoList_Item.gameObject.SetActive(value: false);
			}
		}
	}

	private ObjectiveInfoList_Item CreateItem()
	{
		return Object.Instantiate(itemPrefab, container);
	}

	public void ToggleButton()
	{
		container.gameObject.SetActive(!container.gameObject.activeSelf);
		LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
	}

	public void OnComplete(Objective obj)
	{
		if (objective != obj)
		{
			return;
		}
		OBJ_State.text = "Complete";
		OBJ_Complete.text = "100 %";
		ObjectiveInfoList_ObjEntry_OnThemeGroupChanged();
		toggleButton.gameObject.SetActive(value: false);
		if (listPositions.Count > 0)
		{
			foreach (ObjectiveInfoList_Item listPosition in listPositions)
			{
				listPosition.OnObjectiveComplete();
				Object.Destroy(listPosition.gameObject);
			}
			listPositions.Clear();
		}
		base.enabled = false;
	}

	public void OnStarted(Objective obj)
	{
		if (objective == obj)
		{
			OBJ_State.text = objective.Status.ToString();
			OBJ_Complete.text = $"{100f * objective.CompletePercent:F0}%";
		}
	}

	public void OnDestroy()
	{
		MissionManager.onObjectiveComplete -= OnComplete;
		MissionManager.onObjectiveStarted -= OnStarted;
		ThemeManager.ThemeGroupChanged -= ObjectiveInfoList_ObjEntry_OnThemeGroupChanged;
	}

	private void ObjectiveInfoList_ObjEntry_OnThemeGroupChanged()
	{
		if (objective != null && objective.Status == ObjectiveStatus.Complete)
		{
			OBJ_Name.color = ThemeManager.Active.ColorTheme.AllClear;
			OBJ_State.color = ThemeManager.Active.ColorTheme.AllClear;
			OBJ_Complete.color = ThemeManager.Active.ColorTheme.AllClear;
		}
		else
		{
			OBJ_Name.color = Color.white;
			OBJ_State.color = Color.white;
			OBJ_Complete.color = Color.white;
		}
	}
}
