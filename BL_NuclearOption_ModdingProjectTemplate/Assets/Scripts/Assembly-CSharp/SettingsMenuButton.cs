using JamesFrowen.ScriptableVariables.UI;
using UnityEngine;

public class SettingsMenuButton : ButtonController
{
	[SerializeField]
	private SettingsMenu menu;

	public GameObject MenuPrefab;

	protected override void onClick()
	{
		menu.OpenMenu(this);
	}
}
