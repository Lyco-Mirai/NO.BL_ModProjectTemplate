using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitDebug : MonoBehaviour
{
	[Serializable]
	public class WeaponStationDebug
	{
		[SerializeField]
		private GameObject panel;

		[SerializeField]
		private TMP_Text text;

		private List<WeaponStation> weaponStations;

		private WeaponInfo weaponInfo;

		public bool IsReloading()
		{
			bool flag = false;
			if (weaponStations == null)
			{
				return false;
			}
			foreach (WeaponStation weaponStation in weaponStations)
			{
				flag = weaponStation.GetReloadStatusMin() > 0f || flag;
			}
			return flag;
		}

		public void Hide()
		{
			panel.SetActive(value: false);
			if (weaponStations != null)
			{
				foreach (WeaponStation weaponStation in weaponStations)
				{
					weaponStation.OnUpdated -= WeaponStationDebug_OnFired;
				}
				weaponStations.Clear();
			}
			weaponInfo = null;
		}

		public void Show(Unit unit, WeaponInfo weaponInfo)
		{
			this.weaponInfo = weaponInfo;
			if (weaponStations != null)
			{
				foreach (WeaponStation weaponStation in weaponStations)
				{
					weaponStation.OnUpdated -= WeaponStationDebug_OnFired;
				}
			}
			else
			{
				weaponStations = new List<WeaponStation>();
			}
			weaponStations.Clear();
			foreach (WeaponStation weaponStation2 in unit.weaponStations)
			{
				if (weaponStation2.WeaponInfo == weaponInfo)
				{
					weaponStations.Add(weaponStation2);
					weaponStation2.OnUpdated += WeaponStationDebug_OnFired;
				}
			}
			panel.SetActive(value: true);
			UpdateText();
		}

		public void UpdateText()
		{
			int num = 0;
			int num2 = 0;
			float a = 1f;
			int num3 = 0;
			foreach (WeaponStation weaponStation in weaponStations)
			{
				num += weaponStation.Ammo;
				num2 += weaponStation.FullAmmo;
				a = Mathf.Min(a, weaponStation.GetReloadStatusMin());
				num3 += weaponStation.GetAmmoTotal() - weaponStation.Ammo;
			}
			text.text = $"{weaponInfo.weaponName}: {num} / {num2}";
			if (num3 > 0)
			{
				TMP_Text tMP_Text = text;
				tMP_Text.text = tMP_Text.text + " + " + num3;
			}
		}

		private void WeaponStationDebug_OnFired()
		{
			UpdateText();
		}
	}

	private Unit followingUnit;

	private TrackingInfo trackingInfo;

	[SerializeField]
	private TMP_Text unitName;

	[SerializeField]
	private TMP_Text position;

	[SerializeField]
	private TMP_Text speed;

	[SerializeField]
	private TMP_Text radarAlt;

	[SerializeField]
	private TMP_Text target;

	[SerializeField]
	private TMP_Text missileAttacks;

	[SerializeField]
	private TMP_Text state;

	[SerializeField]
	private TMP_Text weapon;

	[SerializeField]
	private TMP_Text gForce;

	[SerializeField]
	private TMP_Text lookAt;

	[SerializeField]
	private GameObject mainPanel;

	[SerializeField]
	private GameObject speedPanel;

	[SerializeField]
	private GameObject targetPanel;

	[SerializeField]
	private GameObject statePanel;

	[SerializeField]
	private GameObject weaponPanel;

	[SerializeField]
	private GameObject gForcePanel;

	[SerializeField]
	private GameObject weaponStationsPanel;

	[SerializeField]
	private GameObject lookAtPanel;

	private Vector3 velPrev;

	private Turret turret;

	private List<Unit> targetList;

	private WeaponManager weaponManager;

	private Pilot pilot;

	private ShipAI shipAI;

	private List<WeaponInfo> weaponInfoToShow = new List<WeaponInfo>();

	[SerializeField]
	private WeaponStationDebug[] weaponStationDisplays;

	[SerializeField]
	private RearmerDisplay rearmerDisplay;

	private void Awake()
	{
		base.enabled = false;
		base.gameObject.SetActive(value: false);
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			CameraStateManager.onFollowingUnitSet += UnitDebug_OnFollowingUnitSet;
		}
	}

	private void OnDestroy()
	{
		CameraStateManager.onFollowingUnitSet -= UnitDebug_OnFollowingUnitSet;
	}

	private void UnitDebug_OnFollowingUnitSet(Unit unit)
	{
		velPrev = Vector3.zero;
		if (unit == null || unit == SceneSingleton<CombatHUD>.i.aircraft)
		{
			followingUnit = null;
			base.enabled = false;
			base.gameObject.SetActive(value: false);
			return;
		}
		rearmerDisplay.Initialize(null, unit, null);
		UpdateWeaponDisplay(unit);
		followingUnit = unit;
		base.gameObject.SetActive(value: true);
		base.enabled = true;
		unitName.text = followingUnit.unitName;
		trackingInfo = null;
		targetList = null;
		turret = null;
		weaponManager = null;
		pilot = null;
		if (unit.NetworkHQ != null)
		{
			foreach (FactionHQ allHQ in FactionRegistry.GetAllHQs())
			{
				if (allHQ != unit.NetworkHQ)
				{
					trackingInfo = allHQ.GetTrackingData(unit.persistentID);
				}
			}
			turret = unit.gameObject.GetComponentInChildren<Turret>();
		}
		if (followingUnit is Aircraft aircraft)
		{
			turret = null;
			speedPanel.SetActive(value: true);
			targetPanel.SetActive(value: true);
			statePanel.SetActive(value: true);
			gForcePanel.SetActive(value: true);
			targetList = aircraft.weaponManager.GetTargetList();
			weaponManager = aircraft.weaponManager;
			if (aircraft.pilots.Length != 0 && aircraft.pilots[0] != null)
			{
				pilot = aircraft.pilots[0];
			}
		}
		if (followingUnit is Building)
		{
			speedPanel.SetActive(value: false);
			targetPanel.SetActive(value: false);
			statePanel.SetActive(value: false);
		}
		if (followingUnit is GroundVehicle)
		{
			speedPanel.SetActive(value: true);
			targetPanel.SetActive(value: true);
			statePanel.SetActive(value: false);
			gForcePanel.SetActive(value: false);
		}
		if (followingUnit is Missile)
		{
			speedPanel.SetActive(value: true);
			targetPanel.SetActive(value: true);
			statePanel.SetActive(value: false);
			gForcePanel.SetActive(value: true);
		}
		if (followingUnit is Ship ship)
		{
			speedPanel.SetActive(value: true);
			targetPanel.SetActive(value: true);
			statePanel.SetActive(value: true);
			gForcePanel.SetActive(value: false);
			shipAI = ship.GetComponent<ShipAI>();
		}
		if (followingUnit is PilotDismounted)
		{
			speedPanel.SetActive(value: true);
			gForcePanel.SetActive(value: false);
		}
	}

	private void FixedUpdate()
	{
		if (followingUnit != null && gForcePanel.activeSelf && !(followingUnit.rb == null))
		{
			if (velPrev != Vector3.zero)
			{
				Vector3 vector = (followingUnit.rb.velocity - velPrev) / Time.fixedDeltaTime;
				vector += Vector3.up * 9.81f;
				gForce.text = $"{vector.magnitude / 9.81f:F1}g";
			}
			velPrev = followingUnit.rb.velocity;
		}
	}

	private void UpdateWeaponDisplay(Unit unit)
	{
		weaponStationsPanel.SetActive(value: false);
		WeaponStationDebug[] array = weaponStationDisplays;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Hide();
		}
		weaponInfoToShow.Clear();
		foreach (WeaponStation weaponStation in unit.weaponStations)
		{
			if (!weaponInfoToShow.Contains(weaponStation.WeaponInfo))
			{
				weaponInfoToShow.Add(weaponStation.WeaponInfo);
			}
		}
		GameManager.GetLocalFaction(out var localFaction);
		bool flag = (weaponInfoToShow.Count > 0 && localFaction == null) || (unit.NetworkHQ != null && localFaction == unit.NetworkHQ.faction);
		weaponStationsPanel.SetActive(flag && !PlayerSettings.cinematicMode);
		weaponInfoToShow.Sort((WeaponInfo a, WeaponInfo b) => a.costPerRound.CompareTo(b.costPerRound));
		for (int num = 0; num < weaponInfoToShow.Count; num++)
		{
			weaponStationDisplays[num].Show(unit, weaponInfoToShow[num]);
		}
	}

	private void Update()
	{
		if (followingUnit == null)
		{
			base.enabled = false;
			return;
		}
		if (mainPanel.activeSelf)
		{
			if (PlayerSettings.cinematicMode)
			{
				mainPanel.SetActive(value: false);
				weaponStationsPanel.SetActive(value: false);
				lookAtPanel.SetActive(value: false);
				return;
			}
		}
		else if (!PlayerSettings.cinematicMode)
		{
			mainPanel.SetActive(value: true);
			weaponStationsPanel.SetActive(followingUnit.weaponStations.Count > 0);
		}
		GlobalPosition globalPosition = followingUnit.GlobalPosition();
		position.text = $"[{globalPosition.x:F0}, {globalPosition.y:F0}, {globalPosition.z:F0}]";
		radarAlt.text = UnitConverter.AltitudeReading(followingUnit.radarAlt);
		speed.text = UnitConverter.SpeedReading(followingUnit.speed);
		float speedOfSound = LevelInfo.GetSpeedOfSound(globalPosition.y);
		if (followingUnit.speed > speedOfSound)
		{
			speed.text = $"M {followingUnit.speed / speedOfSound:F2}";
		}
		if (followingUnit is GroundVehicle)
		{
			speed.text = UnitConverter.SpeedReadingGround(followingUnit.speed);
		}
		missileAttacks.text = ((trackingInfo != null) ? $"{trackingInfo.missileAttacks}" : "0");
		WeaponStationDebug[] array = weaponStationDisplays;
		foreach (WeaponStationDebug weaponStationDebug in array)
		{
			if (weaponStationDebug.IsReloading())
			{
				weaponStationDebug.UpdateText();
			}
		}
		state.text = ((pilot != null) ? pilot.GetCurrentState() : "none");
		if (turret != null)
		{
			target.text = ((turret.GetTarget() != null) ? turret.GetTarget().unitName : "none");
			turret.GetWeaponStation();
		}
		else
		{
			target.text = ((targetList != null && targetList.Count > 0) ? targetList[0].unitName : "none");
		}
		if (followingUnit is Missile missile)
		{
			target.text = (UnitRegistry.TryGetUnit(missile.targetID, out var unit) ? unit.unitName : "none");
		}
		if (followingUnit is Ship)
		{
			state.text = $"{shipAI.state}";
		}
		if (followingUnit.rb != null && SceneSingleton<CameraStateManager>.i.orbitState.TryGetLookAtUnit(out var lookAtUnit))
		{
			lookAt.text = "Looking at " + lookAtUnit.unitName;
			lookAtPanel.SetActive(value: true);
		}
		else
		{
			lookAtPanel.SetActive(value: false);
		}
	}
}
