using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	[RequireComponent(typeof(Button))]
	public class OpenMenuTabButton : HighlightButton
	{
		[Header("Reference")]
		[SerializeField]
		private EditorTabs tabs;

		[Header("Tab")]
		[SerializeField]
		private GameObject menu;

		protected override void onClick()
		{
			tabs.ToggleTabOpen(this, menu);
		}
	}
}
