using JamesFrowen.ScriptableVariables.UI;
using TMPro;
using UnityEngine;

public class CopyTextButton : ButtonController
{
	[SerializeField]
	private TextMeshProUGUI target;

	protected override void onClick()
	{
		GUIUtility.systemCopyBuffer = target.text;
	}
}
