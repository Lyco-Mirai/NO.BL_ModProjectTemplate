using TMPro;
using UnityEngine;

public class ObjectiveInfoList_Item : MonoBehaviour
{
	public TextMeshProUGUI OBJ_Name;

	public TextMeshProUGUI OBJ_Pos;

	public TextMeshProUGUI OBJ_Dist;

	private GlobalPosition currentPos;

	public void Refresh(MissionPosition.PositionResult position)
	{
		currentPos = position.Position;
		OBJ_Name.text = position.Objective.SavedObjective.ObjectiveTypeEnum.ToString();
		OBJ_Pos.text = SceneSingleton<DynamicMap>.i.gridLabels.GetGridPosition(currentPos);
		if (SceneSingleton<CombatHUD>.i.aircraft == null)
		{
			OBJ_Dist.text = "-";
			return;
		}
		float distance = FastMath.Distance(SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition(), currentPos);
		OBJ_Dist.text = UnitConverter.DistanceReading(distance);
	}

	public void OnObjectiveComplete()
	{
		OBJ_Pos.text = "-";
		OBJ_Dist.text = "-";
		base.enabled = false;
	}

	public void OnButtonClick()
	{
		SceneSingleton<DynamicMap>.i.SetMapTarget(currentPos);
	}
}
