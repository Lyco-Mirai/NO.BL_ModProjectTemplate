using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class ShipOptions : UnitPanelOptions
	{
		[Header("Hold")]
		[SerializeField]
		private Toggle holdPosition;

		[SerializeField]
		private GameObject holdPositionDifferentValue;

		[Header("Skill")]
		[SerializeField]
		private Slider skillSlider;

		[SerializeField]
		private TextMeshProUGUI skillSliderLabel;

		[Header("Waypoints")]
		[SerializeField]
		private WaypointList waypointList;

		protected override void SetupInner()
		{
			targets.SetupToggle(holdPosition, holdPositionDifferentValue, (SavedUnit x) => ref ((SavedShip)x).holdPosition);
			targets.SetupSlider(skillSlider, skillSliderLabel, (SavedUnit x) => ref ((SavedShip)x).skill, (float v) => $"{v:F1}");
		}

		public override void Cleanup()
		{
			targets.RemoveChanged(holdPosition);
			targets.RemoveChanged(skillSlider);
		}

		public override void OnTargetsChanged()
		{
			waypointList.Setup(targets);
		}
	}
}
