using NuclearOption;
using NuclearOption.MissionEditorScripts;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
	[SerializeField]
	private Transform parent;

	[SerializeField]
	private SettingsMenuButton defaultMenu;

	private SettingsMenuButton currentButton;

	private GameObject currentSubmenu;

	public void Start()
	{
		if (SceneSingleton<GameplayUI>.i != null)
		{
			FlightHud.EnableCanvas(enable: false);
			SceneSingleton<GameplayUI>.i.gameplayCanvas.enabled = false;
		}
		PlayerSettings.LoadPrefs();
		OpenMenu(defaultMenu);
	}

	public void OpenMenu(SettingsMenuButton button)
	{
		if (!(currentButton == button))
		{
			if (currentSubmenu != null)
			{
				Object.Destroy(currentSubmenu);
			}
			currentButton = button;
			currentSubmenu = Object.Instantiate(button.MenuPrefab, parent);
			FixLayout.ForceRebuildRecursive(parent.AsRectTransform());
		}
	}

	public void CloseSettingsMenu()
	{
		PlayerSettings.ApplyPrefs();
		Object.Destroy(base.gameObject);
	}
}
