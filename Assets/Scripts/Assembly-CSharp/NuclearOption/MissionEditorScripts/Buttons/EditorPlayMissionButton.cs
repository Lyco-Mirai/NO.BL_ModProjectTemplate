using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables.UI;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	public class EditorPlayMissionButton : ButtonController
	{
		protected override void onClick()
		{
			MissionEditor.PlayFromEditor().Forget();
		}
	}
}
