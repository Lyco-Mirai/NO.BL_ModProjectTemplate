using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	[RequireComponent(typeof(Button))]
	public class SpawnPrefabTabButton : HighlightButton
	{
		[Header("Reference")]
		[SerializeField]
		private EditorTabs tabs;

		[Header("Tab")]
		[SerializeField]
		private GameObject tabPrefab;

		[SerializeField]
		private PanelLayout layout;

		protected override void onClick()
		{
			ToggleTab(clearUnit: false);
		}

		public void ToggleTab(bool clearUnit)
		{
			tabs.ToggleTabPrefab(this, tabPrefab, clearUnit, layout);
		}
	}
}
