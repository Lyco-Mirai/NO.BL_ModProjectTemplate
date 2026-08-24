using System.Collections.Generic;
using NuclearOption.MissionEditorScripts.MultiSelect;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class WaypointEntry : MonoBehaviour
	{
		public const string UNIT_SPAWN_TIMING = "Unit Spawn";

		[SerializeField]
		private Button positionButton;

		[SerializeField]
		private Button removeButton;

		[SerializeField]
		private Text positionLabel;

		[SerializeField]
		private Dropdown objectiveSelect;

		private MultiField<GlobalPosition> positionField;

		private MultiField<string> objectiveField;

		private WaypointList waypointList;

		public int Index { get; private set; }

		private void Awake()
		{
			positionButton.onClick.AddListener(PositionClicked);
			removeButton.onClick.AddListener(RemoveClicked);
			objectiveSelect.onValueChanged.AddListener(ObjectiveSelected);
		}

		public void SetEntry(WaypointList waypointList, List<string> objectives, int index, MultiField<GlobalPosition> positionField, MultiField<string> objectiveField)
		{
			Index = index;
			this.positionField = positionField;
			this.objectiveField = objectiveField;
			this.waypointList = waypointList;
			positionLabel.text = $"Set Position [{positionField.Get()}]";
			objectiveSelect.options.Clear();
			objectiveSelect.AddOptions(objectives);
			int num = objectives.IndexOf(objectiveField.Get());
			if (num == -1)
			{
				Debug.LogError("Waypoint is using objective with name " + objectiveField.Get() + " but no objective with that names exists. Resetting waypoint to use UnitSpawn");
				objectiveField.Set("Unit Spawn");
				num = 0;
			}
			objectiveSelect.SetValueWithoutNotify(num);
		}

		private void PositionClicked()
		{
			GlobalPosition obj = SceneSingleton<CameraStateManager>.i.transform.position.ToGlobalPosition();
			positionField.Set(obj);
			positionLabel.text = "Set Position [" + obj.ToString() + "]";
		}

		private void ObjectiveSelected(int index)
		{
			objectiveField.Set(objectiveSelect.options[objectiveSelect.value].text);
		}

		public void RemoveClicked()
		{
			waypointList.RemoveWayPoint(this);
		}
	}
}
