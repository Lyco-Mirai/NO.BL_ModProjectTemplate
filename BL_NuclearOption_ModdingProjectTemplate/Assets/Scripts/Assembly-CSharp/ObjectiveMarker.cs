using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveMarker : MapMarker
{
	private ObjectiveMarkerManager manager;

	[SerializeField]
	private Text objName;

	[SerializeField]
	private Sprite destroyObjective;

	[SerializeField]
	private Sprite waypointObjective;

	[SerializeField]
	private Sprite captureObjective;

	[SerializeField]
	private Sprite reconObjective;

	public bool shown;

	public bool masked;

	private MissionPosition.PositionResult posResult;

	public void SetObjective(ObjectiveMarkerManager objManager, MissionPosition.PositionResult positionResult)
	{
		manager = objManager;
		posResult = positionResult;
		Objective objective = positionResult.Objective;
		string displayName = objective.SavedObjective.DisplayName;
		objName.text = displayName;
		base.gameObject.name = $"OBJ_{objective.SavedObjective.ObjectiveTypeEnum}";
		SetSprite(positionResult);
		Show(value: true);
	}

	public void UpdateMarker(MissionPosition.PositionResult pos)
	{
		base.transform.eulerAngles = Vector3.zero;
		float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		base.transform.localScale = Vector3.one * num * 1f;
		Vector3 vector = pos.Position.AsVector3() * SceneSingleton<DynamicMap>.i.mapDisplayFactor;
		base.transform.localPosition = new Vector3(vector.x, vector.z, 0f);
		if (pos.Objective != posResult.Objective || !pos.Position.Equals(posResult.Position))
		{
			posResult = pos;
			string displayName = pos.Objective.SavedObjective.DisplayName;
			objName.text = displayName;
			SetSprite(pos);
		}
	}

	public override void Show(bool value)
	{
		markerImg.color = Color.white;
		markerImg.enabled = value;
		objName.enabled = value;
		shown = value;
		masked = false;
	}

	public override void Mask()
	{
		Color color = 0.5f * markerImg.color;
		color.a = 0.5f;
		markerImg.color = color;
		objName.enabled = false;
		masked = true;
	}

	public void SetSprite(MissionPosition.PositionResult positionResult)
	{
		Objective objective = positionResult.Objective;
		float num = 20f;
		if ((objective.SavedObjective.ObjectiveTypeEnum == ObjectiveType.ReachWaypoints || objective.SavedObjective.ObjectiveTypeEnum == ObjectiveType.ReachUnits) && markerImg.sprite != waypointObjective)
		{
			markerImg.sprite = waypointObjective;
		}
		else if (objective.SavedObjective.ObjectiveTypeEnum == ObjectiveType.DestroyUnits && markerImg.sprite != destroyObjective)
		{
			markerImg.sprite = destroyObjective;
		}
		else if (objective.SavedObjective.ObjectiveTypeEnum == ObjectiveType.SpotUnit && markerImg.sprite != reconObjective)
		{
			markerImg.sprite = reconObjective;
			num = 40f;
		}
		else if (objective.SavedObjective.ObjectiveTypeEnum == ObjectiveType.CaptureAirbase && markerImg.sprite != captureObjective)
		{
			markerImg.sprite = captureObjective;
			num = 40f;
		}
		markerImg.rectTransform.sizeDelta = num * Vector2.one;
		objName.rectTransform.localPosition = new Vector3(0f, num, 0f);
	}
}
