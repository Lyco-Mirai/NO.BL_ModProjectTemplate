using NuclearOption.MissionEditorScripts;
using NuclearOption.UI;
using Rewired;
using UnityEngine;

public class ExtraUiInput : MonoBehaviour
{
	private Player player;

	private void Awake()
	{
		player = ReInput.players.GetPlayer(0);
	}

	private void Update()
	{
		if (player.GetButtonDown("Map"))
		{
			if (LeaderboardMenu.IsOpen() || InputFieldChecker.InsideInputField)
			{
				return;
			}
			if (GameManager.gameState == GameState.Editor && SceneSingleton<MissionEditor>.i != null && SceneSingleton<MissionEditor>.i.Tabs != null)
			{
				SceneSingleton<MissionEditor>.i.Tabs.ToggleMapTab();
			}
			else if (!DynamicMap.mapMaximized)
			{
				SceneSingleton<DynamicMap>.i.Maximize();
			}
			else
			{
				SceneSingleton<DynamicMap>.i.Minimize();
			}
		}
		CombatHUD i = SceneSingleton<CombatHUD>.i;
		if (!(i != null) || (!i.gameObject.activeSelf && !DynamicMap.mapMaximized) || !i.HasTargets)
		{
			return;
		}
		if (GameManager.playerInput.GetButtonTimedPressUp("Cancel", 0f, PlayerSettings.clickDelay))
		{
			i.DeselectLast();
			if (i.aircraft.targetCam != null)
			{
				i.aircraft.targetCam.CancelTarget();
			}
		}
		if (GameManager.playerInput.GetButtonTimedPressDown("Cancel", PlayerSettings.pressDelay))
		{
			i.DeselectAll(withAudio: true);
			SceneSingleton<DynamicMap>.i.DeselectAllIcons();
			if (i.aircraft.targetCam != null)
			{
				i.aircraft.targetCam.CancelTarget();
			}
		}
	}
}
