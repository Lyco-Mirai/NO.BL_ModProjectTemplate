using JamesFrowen.ScriptableVariables.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	[RequireComponent(typeof(Button))]
	public class ExitEditorButton : ButtonController
	{
		[SerializeField]
		private ExitEditorModal exitModal;

		protected override void onClick()
		{
			exitModal.Show();
		}
	}
}
