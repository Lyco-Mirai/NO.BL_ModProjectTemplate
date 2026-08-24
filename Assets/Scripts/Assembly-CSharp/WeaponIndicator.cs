using UnityEngine;
using UnityEngine.UI;

public class WeaponIndicator : HUDApp
{
	[SerializeField]
	private Image weaponImage;

	[SerializeField]
	private Text weaponName;

	[SerializeField]
	private Text weaponAmmo;

	private bool safetyOn;

	private Aircraft aircraft;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		weaponName.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		weaponAmmo.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		Show(PlayerSettings.hudWeapons);
		Refresh();
	}

	public override void Refresh()
	{
		if (aircraft == null || !PlayerSettings.hudWeapons)
		{
			return;
		}
		if (aircraft.weaponManager.currentWeaponStation != null)
		{
			weaponImage.color = Color.green;
			weaponName.color = Color.green;
			weaponAmmo.color = Color.green;
			if (weaponName.text != aircraft.weaponManager.currentWeaponStation.WeaponInfo.shortName)
			{
				if (!weaponImage.enabled)
				{
					weaponImage.enabled = true;
				}
				weaponImage.sprite = aircraft.weaponManager.currentWeaponStation.WeaponInfo.weaponIcon;
				weaponName.text = aircraft.weaponManager.currentWeaponStation.WeaponInfo.shortName;
			}
			if (!aircraft.weaponManager.currentWeaponStation.WeaponInfo.energy)
			{
				int ammo = aircraft.weaponManager.currentWeaponStation.Ammo;
				weaponAmmo.text = $"{aircraft.weaponManager.currentWeaponStation.Ammo}";
				if ((float)ammo == 0f)
				{
					weaponImage.color = Color.red;
					weaponName.color = Color.red;
					weaponAmmo.color = Color.red;
				}
			}
			else
			{
				float charge = aircraft.GetPowerSupply().GetCharge();
				weaponAmmo.text = $"{100f * charge:F0}";
				if (charge == 0f)
				{
					weaponAmmo.color = Color.red;
				}
			}
			if (aircraft.weaponManager.currentWeaponStation.SafetyIsOn(aircraft))
			{
				weaponImage.color = Color.grey;
				weaponName.color = Color.grey;
				weaponAmmo.color = Color.grey;
				if (!safetyOn)
				{
					SceneSingleton<AircraftActionsReport>.i.ReportText("Weapon safety lock <b>Enabled</b>", 5f);
				}
				safetyOn = true;
			}
			else if (safetyOn)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("Weapon safety lock <b>Disabled</b>", 5f);
				safetyOn = false;
			}
		}
		else
		{
			weaponImage.enabled = false;
			weaponName.text = "";
			weaponAmmo.text = "";
		}
	}

	public void Show(bool arg)
	{
		weaponImage.enabled = arg;
		weaponName.enabled = arg;
		weaponAmmo.enabled = arg;
	}
}
