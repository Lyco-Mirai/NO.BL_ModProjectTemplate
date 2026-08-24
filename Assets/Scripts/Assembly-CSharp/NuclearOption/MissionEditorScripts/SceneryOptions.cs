using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class SceneryOptions : UnitPanelOptions
	{
		[SerializeField]
		private Toggle indestructibleToggle;

		[SerializeField]
		private GameObject indestructibleToggleDifferentValue;

		protected override void SetupInner()
		{
			targets.SetupToggle(indestructibleToggle, indestructibleToggleDifferentValue, (SavedUnit x) => ref ((SavedScenery)x).indestructible);
		}

		public override void Cleanup()
		{
			targets.RemoveChanged(indestructibleToggle);
		}

		public override void OnTargetsChanged()
		{
		}
	}
}
