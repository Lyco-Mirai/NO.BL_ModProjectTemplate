using JamesFrowen.ScriptableVariables.UI;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	public class ClosePanelButton : ButtonController
	{
		[SerializeField]
		private GameObject panel;

		protected override void onClick()
		{
			panel.SetActive(value: false);
		}
	}
}
