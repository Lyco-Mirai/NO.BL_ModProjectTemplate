using UnityEngine;

public class HUDAppManager : SceneSingleton<HUDAppManager>
{
	private Aircraft aircraft;

	[SerializeField]
	private HUDApp[] apps;

	private void Start()
	{
		aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		aircraft.onDisableUnit += HUDAppManager_OnUnitDisable;
		PlayerSettings.OnApplyOptions += RefreshSettings;
		HUDApp[] array = apps;
		foreach (HUDApp obj in array)
		{
			obj.Initialize(aircraft);
			obj.RefreshSettings();
		}
	}

	private void Update()
	{
		for (int i = 0; i < apps.Length; i++)
		{
			apps[i].Refresh();
		}
	}

	public void RefreshSettings()
	{
		HUDApp[] array = apps;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RefreshSettings();
		}
	}

	private void HUDAppManager_OnUnitDisable(Unit unit)
	{
		aircraft.onDisableUnit -= HUDAppManager_OnUnitDisable;
		PlayerSettings.OnApplyOptions -= RefreshSettings;
		if (this != null)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		PlayerSettings.OnApplyOptions -= RefreshSettings;
	}
}
