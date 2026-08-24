using System.Collections.Generic;
using UnityEngine;

public class ObjectiveMarkerManager : MonoBehaviour
{
	[SerializeField]
	private ObjectiveMarker markerPrefab;

	private readonly List<ObjectiveMarker> objectiveMarkers = new List<ObjectiveMarker>();

	private Transform iconLayer;

	private List<MissionPosition.PositionResult> resultCache = new List<MissionPosition.PositionResult>();

	public void Initialize(Transform layer)
	{
		iconLayer = layer;
		this.StartSlowUpdateDelayed(1f, UpdateObjectiveMarkers);
	}

	private void Update()
	{
		if (!(SceneSingleton<DynamicMap>.i.HQ == null) && MissionManager.Runner != null)
		{
			UpdateObjectiveMarkers();
		}
	}

	private ObjectiveMarker CreateItem(MissionPosition.PositionResult positionResult)
	{
		ObjectiveMarker objectiveMarker = Object.Instantiate(markerPrefab, iconLayer);
		objectiveMarker.SetObjective(this, positionResult);
		return objectiveMarker;
	}

	private void UpdateObjectiveMarkers()
	{
		List<MissionPosition.PositionResult> allPositions = GetAllPositions();
		while (objectiveMarkers.Count < allPositions.Count)
		{
			objectiveMarkers.Add(CreateItem(allPositions[objectiveMarkers.Count]));
		}
		for (int i = 0; i < objectiveMarkers.Count; i++)
		{
			ObjectiveMarker objectiveMarker = objectiveMarkers[i];
			if (i < allPositions.Count && SceneSingleton<MapOptions>.i.showObjectives)
			{
				bool flag = false;
				for (int j = 0; j < allPositions.Count; j++)
				{
					if (i != j && allPositions[i].Objective == allPositions[j].Objective)
					{
						float num = Vector3.Distance(objectiveMarker.transform.localPosition, objectiveMarkers[j].transform.localPosition) * SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
						if (objectiveMarkers[j].shown && !objectiveMarkers[j].masked && num < 40f)
						{
							flag = true;
						}
					}
				}
				objectiveMarker.Show(value: true);
				if (flag)
				{
					objectiveMarker.Mask();
				}
				objectiveMarker.UpdateMarker(allPositions[i]);
			}
			else
			{
				objectiveMarker.Show(value: false);
			}
		}
	}

	private List<MissionPosition.PositionResult> GetAllPositions()
	{
		MissionPosition.GetAllPositionsResults(SceneSingleton<DynamicMap>.i.HQ, Datum.originPosition.ToGlobalPosition(), includeHidden: false, resultCache);
		return resultCache;
	}
}
