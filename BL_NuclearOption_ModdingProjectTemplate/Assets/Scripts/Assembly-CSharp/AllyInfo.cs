using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;

public class AllyInfo : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI hoveredAllyInfo;

	private bool hoverIconExists;

	private Aircraft nearestAlly;

	private Aircraft hoveredAlly;

	private HUDUnitMarker nearestAllyMarker;

	private HUDUnitMarker hoveredAllyMarker;

	private void OnEnable()
	{
		hoveredAllyInfo.text = "";
		hoveredAllyInfo.enabled = false;
		hoverIconExists = false;
		hoveredAlly = null;
		nearestAlly = null;
		nearestAllyMarker = null;
		this.StartSlowUpdateDelayed(1f, UpdateNearestAlly);
		this.StartSlowUpdateDelayed(0.5f, UpdateAllyInfoOnHover);
		RefreshSettings();
		PlayerSettings.OnApplyOptions += RefreshSettings;
	}

	private void OnDisable()
	{
		hoveredAllyInfo.enabled = false;
		PlayerSettings.OnApplyOptions -= RefreshSettings;
	}

	private void UpdateNearestAlly()
	{
		if (SceneSingleton<CombatHUD>.i?.aircraft == null)
		{
			return;
		}
		bool playersOnly = false;
		Aircraft aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		FactionHQ networkHQ = aircraft.NetworkHQ;
		UnitRegistry.TryGetNearestAircraft(aircraft, playersOnly, networkHQ, out nearestAlly, out var _);
		if (nearestAlly != null)
		{
			SceneSingleton<CombatHUD>.i.TryGetMarker(nearestAlly, out var marker);
			if (marker != null && marker != nearestAllyMarker)
			{
				nearestAllyMarker?.SetNearestAlly(nearest: false);
				marker.SetNearestAlly(nearest: true);
				nearestAllyMarker = marker;
			}
		}
	}

	private void UpdateAllyInfoOnHover()
	{
		if (SceneSingleton<CombatHUD>.i?.aircraft == null)
		{
			return;
		}
		bool playersOnly = false;
		Aircraft aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		Vector3 forward = SceneSingleton<CameraStateManager>.i.transform.forward;
		GlobalPosition globalPosition = SceneSingleton<CameraStateManager>.i.transform.GlobalPosition();
		float num = 0f;
		Aircraft aircraft2 = hoveredAlly;
		hoveredAlly = null;
		foreach (Aircraft item in SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ.GetActiveAircraft(playersOnly))
		{
			if (!(item == aircraft))
			{
				Vector3 rhs = FastMath.NormalizedDirection(globalPosition, item.GlobalPosition());
				float num2 = Vector3.Dot(forward, rhs);
				if (num2 > num)
				{
					num = num2;
					hoveredAlly = item;
				}
			}
		}
		hoverIconExists = hoveredAlly != null && SceneSingleton<CombatHUD>.i.TryGetMarker(hoveredAlly, out hoveredAllyMarker);
		hoveredAllyInfo.enabled = hoverIconExists && hoveredAllyMarker.GetLocalPosition().sqrMagnitude < 40000f;
		if (hoverIconExists && aircraft2 != hoveredAlly)
		{
			hoveredAllyInfo.text = "";
			if (hoveredAlly.Player != null)
			{
				TextMeshProUGUI textMeshProUGUI = hoveredAllyInfo;
				textMeshProUGUI.text = textMeshProUGUI.text + hoveredAlly.Player.GetDisplayName(PlayerNameContext.Other) + "\n";
			}
			TextMeshProUGUI textMeshProUGUI2 = hoveredAllyInfo;
			textMeshProUGUI2.text = textMeshProUGUI2.text + hoveredAlly.definition.code + "\n\n\n ";
			Color hudUnitFriendly = ThemeManager.Active.ColorTheme.HudUnitFriendly;
			hoveredAllyInfo.color = hudUnitFriendly.WithAlpha(1f);
		}
	}

	private void RefreshSettings()
	{
		hoveredAllyInfo.fontSize = (int)PlayerSettings.hmdTextSize;
	}

	private void LateUpdate()
	{
		if (hoverIconExists && hoveredAllyMarker != null && hoveredAllyMarker.image != null && !SceneSingleton<CombatHUD>.i.GetTargetList().Contains(hoveredAllyMarker.unit) && SceneSingleton<CombatHUD>.i.jamAccumulation == 0f && hoveredAllyMarker.image != null)
		{
			hoveredAllyInfo.transform.localPosition = hoveredAllyMarker.image.transform.localPosition;
			return;
		}
		hoveredAllyInfo.enabled = false;
		hoverIconExists = false;
		hoveredAllyMarker = null;
	}
}
