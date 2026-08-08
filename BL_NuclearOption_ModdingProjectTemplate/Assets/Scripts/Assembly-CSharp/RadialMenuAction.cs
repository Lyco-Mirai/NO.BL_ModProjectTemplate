using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Radial Menu Action", menuName = "ScriptableObjects/RadialMenuAction", order = 8)]
public class RadialMenuAction : ScriptableObject
{
	public enum ActionType
	{
		Eject = 0,
		Gear = 1,
		Radar = 2,
		NavLights = 3,
		FlightAssist = 4,
		AutoHover = 5,
		Engine = 6,
		Nightvis = 7,
		TurretAuto = 8,
		SelectWeapon = 9,
		LinkGuns = 10
	}

	[SerializeField]
	public string DisplayName;

	[SerializeField]
	private ActionType actionType;

	[SerializeField]
	private Sprite iconSprite;

	[SerializeField]
	private Sprite backgroundSprite;

	[SerializeField]
	private Text ammoText;

	[SerializeField]
	private Color backgroundColorInactive;

	[SerializeField]
	private Color backgroundColorActive;

	[SerializeField]
	public int weapon_number = -1;

	public bool Caution;

	private Image iconImage;

	private Image backgroundImage;

	private float flashAmount;

	private Color selectedColor = Color.green;

	private Color defaultColor = Color.gray;

	public bool AllowedOnAircraft(Aircraft aircraft)
	{
		return actionType switch
		{
			ActionType.Eject => true, 
			ActionType.Gear => true, 
			ActionType.Radar => aircraft.radar != null, 
			ActionType.FlightAssist => aircraft.GetControlsFilter().HasFlightAssist(), 
			ActionType.AutoHover => aircraft.GetControlsFilter().HasAutoHover(), 
			ActionType.Engine => true, 
			ActionType.Nightvis => true, 
			ActionType.TurretAuto => aircraft.weaponManager.StationsWithTurrets() > 0, 
			ActionType.LinkGuns => aircraft.weaponManager.HasMultipleGuns(), 
			ActionType.SelectWeapon => aircraft.weaponStations.Count > weapon_number, 
			_ => false, 
		};
	}

	public void Setup(Image backgroundImage, Image iconImage, Text ammoText)
	{
		backgroundImage.gameObject.SetActive(value: true);
		this.backgroundImage = backgroundImage;
		backgroundImage.sprite = backgroundSprite;
		selectedColor = (Caution ? (Color.green * 0.9f + Color.red) : Color.green);
		defaultColor = ((actionType == ActionType.SelectWeapon) ? Color.green : Color.gray);
		this.iconImage = iconImage;
		this.iconImage.sprite = iconSprite;
		iconImage.rectTransform.sizeDelta = ((actionType == ActionType.SelectWeapon) ? new Vector2(80f, 40f) : new Vector2(50f, 50f));
		iconImage.sprite = iconSprite;
		this.ammoText = ammoText;
		UnHover();
	}

	public void Hover()
	{
		backgroundImage.color = backgroundColorActive;
		iconImage.color = selectedColor;
		ammoText.color = selectedColor;
	}

	public void UnHover()
	{
		backgroundImage.color = backgroundColorInactive;
		iconImage.color = defaultColor;
		ammoText.color = defaultColor;
	}

	public void Flash()
	{
		iconImage.color = Color.Lerp(Color.gray, Color.yellow, flashAmount);
		flashAmount -= Time.deltaTime;
	}

	public void TriggerAction(Aircraft aircraft)
	{
		flashAmount = 1f;
		switch (actionType)
		{
		case ActionType.Eject:
			aircraft.StartEjectionSequence();
			break;
		case ActionType.Gear:
			if (!(aircraft.radarAlt < 0.2f))
			{
				if (aircraft.gearState == LandingGear.GearState.LockedExtended)
				{
					aircraft.SetGear(deployed: false);
				}
				if (aircraft.gearState == LandingGear.GearState.LockedRetracted)
				{
					aircraft.SetGear(deployed: true);
				}
			}
			break;
		case ActionType.Radar:
			aircraft.CmdToggleRadar();
			break;
		case ActionType.FlightAssist:
			aircraft.TogglePitchLimiter();
			break;
		case ActionType.AutoHover:
			aircraft.GetControlsFilter().ToggleAutoHover();
			break;
		case ActionType.Engine:
			aircraft.CmdToggleIgnition();
			break;
		case ActionType.Nightvis:
			NightVision.Toggle();
			break;
		case ActionType.TurretAuto:
			SceneSingleton<CombatHUD>.i.ToggleAutoControl();
			break;
		case ActionType.LinkGuns:
			aircraft.weaponManager.ToggleGunsLinked();
			break;
		case ActionType.SelectWeapon:
			aircraft.SetActiveStation((byte)weapon_number);
			SceneSingleton<CombatHUD>.i.ShowWeaponStation(aircraft.weaponStations[weapon_number]);
			break;
		case ActionType.NavLights:
			break;
		}
	}

	public void SetWeapon(WeaponInfo weaponInfo, int number)
	{
		weapon_number = number;
		iconSprite = weaponInfo.weaponIcon;
		DisplayName = weaponInfo.weaponName;
		selectedColor = Color.green;
		defaultColor = Color.green;
	}

	public void RefreshWeapon(string ammoReadout, float ammoLevel)
	{
		ammoText.enabled = true;
		ammoText.text = ammoReadout;
		if (ammoLevel == 0f)
		{
			selectedColor = Color.red;
			defaultColor = Color.grey;
		}
		else
		{
			selectedColor = Color.green;
			defaultColor = Color.green;
		}
	}

	public ActionType GetActionType()
	{
		return actionType;
	}
}
