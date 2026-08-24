using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	[RequireComponent(typeof(Button))]
	public class MapButton : HighlightButton
	{
		[Header("Reference")]
		[SerializeField]
		private EditorTabs tabs;

		protected override void onClick()
		{
			tabs.ChangeTabMap(this);
		}
	}
}
