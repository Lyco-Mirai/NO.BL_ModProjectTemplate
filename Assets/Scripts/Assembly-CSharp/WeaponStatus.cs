using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponStatus : MonoBehaviour
{
	[SerializeField]
	private Image weaponImage;

	[SerializeField]
	private Image reloadProgressImage;

	[SerializeField]
	private Image safetyImage;

	[SerializeField]
	private GameObject weaponPanel;

	[SerializeField]
	private TextMeshProUGUI nameText;

	[SerializeField]
	private TextMeshProUGUI ammoText;

	[SerializeField]
	private RectTransform reloadProgress;

	[SerializeField]
	private RectTransform panel;

	private WeaponStation weaponStation;

	private Aircraft aircraft;

	private Color weaponBaseColor;

	private Color weaponFlashColor;

	private float flashAmount;

	private void Awake()
	{
		base.enabled = false;
		weaponFlashColor = weaponBaseColor;
	}

	public void SetCurrentStation(Aircraft aircraft, WeaponStation weaponStation)
	{
		if (this.weaponStation != null)
		{
			this.weaponStation.OnUpdated -= WeaponStatus_OnStationUpdated;
			ThemeManager.ThemeGroupChanged -= UpdateDisplay;
		}
		if (this.aircraft != null)
		{
			this.aircraft.onSetGear -= WeaponStatus_OnSetGear;
		}
		if (weaponStation != null)
		{
			this.aircraft = aircraft;
			this.weaponStation = weaponStation;
			aircraft.onSetGear += WeaponStatus_OnSetGear;
			weaponStation.OnUpdated += WeaponStatus_OnStationUpdated;
			ThemeManager.ThemeGroupChanged += UpdateDisplay;
			UpdateDisplay(weaponStation);
			UpdateSafety();
		}
	}

	private void WeaponStatus_OnStationUpdated()
	{
		flashAmount = 1f;
		UpdateDisplay(weaponStation);
		base.enabled = true;
	}

	private void WeaponStatus_OnSetGear(Aircraft.OnSetGear e)
	{
		UpdateSafety();
	}

	private void UpdateSafety()
	{
		if (weaponStation.SafetyIsOn(aircraft))
		{
			if (!safetyImage.enabled)
			{
				safetyImage.enabled = true;
				SceneSingleton<AircraftActionsReport>.i.ReportText("Weapon safety lock <b>Enabled</b>", 5f);
			}
		}
		else if (safetyImage.enabled)
		{
			safetyImage.enabled = false;
			SceneSingleton<AircraftActionsReport>.i.ReportText("Weapon safety lock <b>Disabled</b>", 5f);
		}
	}

	public void SetVisible(bool visibility)
	{
		weaponPanel.SetActive(visibility);
	}

	public void SetSafety(bool safety)
	{
		safetyImage.enabled = safety;
	}

	public void UpdateDisplay()
	{
		UpdateDisplay(weaponStation);
	}

	public void UpdateDisplay(WeaponStation weaponStation)
	{
		if (weaponStation != null)
		{
			weaponImage.sprite = weaponStation.WeaponInfo.weaponIcon;
			ammoText.text = weaponStation.GetAmmoReadout();
			nameText.text = (weaponStation.Cargo ? ("Cargo (" + weaponStation.WeaponInfo.weaponName + ")") : weaponStation.WeaponInfo.weaponName);
			weaponBaseColor = ((weaponStation.Ammo > 0) ? ThemeManager.Active.ColorTheme.AllClear : ThemeManager.Active.ColorTheme.Alert);
			weaponImage.color = Color.Lerp(weaponBaseColor, ThemeManager.Active.ColorTheme.AllClear * 0.5f + ThemeManager.Active.ColorTheme.Alert, flashAmount);
			ammoText.color = weaponImage.color;
			nameText.color = weaponImage.color;
			if (weaponStation.Reloading)
			{
				base.enabled = true;
				Vector2 vector = new Vector2(panel.sizeDelta.x - 10f, reloadProgress.sizeDelta.y);
				reloadProgressImage.enabled = true;
				weaponImage.color = ThemeManager.Active.ColorTheme.AllClear * 0.5f + ThemeManager.Active.ColorTheme.Alert;
				reloadProgress.sizeDelta = new Vector2(vector.x, vector.y);
			}
			else
			{
				reloadProgressImage.enabled = false;
			}
		}
	}

	private void Update()
	{
		if (flashAmount > 0f)
		{
			flashAmount -= 5f * Time.deltaTime;
		}
		UpdateDisplay(weaponStation);
		if (!weaponStation.Reloading)
		{
			if (flashAmount <= 0f)
			{
				base.enabled = false;
			}
			reloadProgressImage.enabled = false;
		}
		else
		{
			float reloadStatusMax = weaponStation.GetReloadStatusMax();
			Vector2 vector = new Vector2(panel.sizeDelta.x - 10f, reloadProgress.sizeDelta.y);
			reloadProgressImage.enabled = true;
			reloadProgress.sizeDelta = new Vector2(vector.x * reloadStatusMax, vector.y);
		}
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= UpdateDisplay;
	}
}
