using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
	private static readonly List<WeaponMount> legalWeaponsCache = new List<WeaponMount>();

	public HardpointSet[] hardpointSets;

	public readonly Dictionary<int, List<Weapon>> HardpointsIndexes = new Dictionary<int, List<Weapon>>();

	[SerializeField]
	private Aircraft aircraft;

	[NonSerialized]
	public WeaponStation currentWeaponStation;

	private List<Renderer> colorables = new List<Renderer>();

	private List<Renderer> skinnables = new List<Renderer>();

	private List<Unit> targetList = new List<Unit>();

	private LiveryData liveryData;

	private MaterialCleanup materialCleanup;

	private bool gunsLinked;

	public event Action OnWeaponsLoaded;

	public event Action OnStationFired;

	private void Awake()
	{
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			aircraft.onInitialize += WeaponManager_OnInitialize;
		}
	}

	private void OnDestroy()
	{
		materialCleanup?.CleanupAll();
	}

	public void UpdateColorables(LiveryData liveryData)
	{
		if (liveryData == null)
		{
			return;
		}
		this.liveryData = liveryData;
		if (materialCleanup == null)
		{
			materialCleanup = new MaterialCleanup();
		}
		for (int num = colorables.Count - 1; num >= 0; num--)
		{
			if (!(colorables[num] == null))
			{
				Material material = colorables[num].material;
				materialCleanup.Add(material);
				LiveryData.TextureColor[] colors = liveryData.Colors;
				if (colors != null && colors.Length != 0)
				{
					material.SetColor("_Color", liveryData.Colors[0].Color);
				}
			}
		}
		foreach (Renderer skinnable in skinnables)
		{
			Material material2 = skinnable.material;
			materialCleanup.Add(material2);
			material2.SetTexture("_Livery", liveryData.Texture);
			material2.SetFloat("_Glossiness", liveryData.Glossiness);
		}
	}

	public Loadout SelectAIAircraftWeapons(Airbase parentAirbase)
	{
		Loadout loadout = new Loadout();
		int num = 0;
		if (parentAirbase == null)
		{
			parentAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position);
		}
		if (parentAirbase != null)
		{
			num = parentAirbase.GetWarheads();
			int b = aircraft.NetworkHQ.GetWarheadStockpile() - aircraft.NetworkHQ.warheadsReserve;
			num = Mathf.Min(num, b);
		}
		if (!MissionManager.AllowTactical())
		{
			num = 0;
		}
		HardpointSet[] array = hardpointSets;
		foreach (HardpointSet hardpointSet in array)
		{
			WeaponMount weaponMount = null;
			WeaponChecker.GetAvailableWeaponsNonAlloc(null, hardpointSet, parentAirbase, parentAirbase.CurrentHQ, allowEmpty: false, legalWeaponsCache);
			WeaponChecker.PreferNukesFilter(num, hardpointSet, legalWeaponsCache);
			if (legalWeaponsCache.Count > 0)
			{
				weaponMount = legalWeaponsCache.RandomItem();
			}
			if (weaponMount != null && weaponMount.info != null && weaponMount.info.nuclear)
			{
				num -= weaponMount.ammo * hardpointSet.hardpoints.Count;
			}
			loadout.weapons.Add(weaponMount);
		}
		return loadout;
	}

	public Loadout GetCurrentLoadout()
	{
		return aircraft.loadout;
	}

	public float GetCurrentValue(bool includeCargo)
	{
		float num = 0f;
		HardpointSet[] array = hardpointSets;
		foreach (HardpointSet hardpointSet in array)
		{
			foreach (Hardpoint hardpoint in hardpointSet.hardpoints)
			{
				_ = hardpoint;
				if (!(hardpointSet.weaponMount == null) && (!(hardpointSet.weaponMount.info != null) || !hardpointSet.weaponMount.info.cargo || includeCargo))
				{
					num += hardpointSet.weaponMount.emptyCost;
				}
			}
		}
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (includeCargo || !weaponStation.WeaponInfo.cargo)
			{
				num += (float)weaponStation.Ammo * weaponStation.WeaponInfo.costPerRound;
			}
		}
		return num;
	}

	public int GetCurrentWarheads()
	{
		int num = 0;
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.WeaponInfo.nuclear)
			{
				num += weaponStation.Ammo;
			}
		}
		return num;
	}

	public float GetCurrentMass()
	{
		float num = 0f;
		HardpointSet[] array = hardpointSets;
		foreach (HardpointSet hardpointSet in array)
		{
			foreach (Hardpoint hardpoint in hardpointSet.hardpoints)
			{
				_ = hardpoint;
				num += ((hardpointSet.weaponMount == null) ? 0f : hardpointSet.weaponMount.emptyMass);
			}
		}
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.Cargo)
			{
				foreach (Weapon weapon in weaponStation.Weapons)
				{
					num += weapon.GetMass();
				}
			}
			else
			{
				num += (float)weaponStation.Ammo * weaponStation.WeaponInfo.massPerRound;
			}
		}
		return num;
	}

	public void ClearTargetList()
	{
		targetList.Clear();
		TargetListChanged();
	}

	public void AddTargetList(Unit target)
	{
		if (target != null)
		{
			targetList.Insert(0, target);
			TargetListChanged();
		}
		else
		{
			ColorLog<WeaponManager>.LogError("AddTargetList was given null target");
		}
	}

	public void RemoveTargetList(Unit target)
	{
		targetList.Remove(target);
		TargetListChanged();
	}

	public List<Unit> GetTargetList()
	{
		return targetList;
	}

	public bool CheckIsTarget(Unit candidate)
	{
		if (targetList.Count > 0)
		{
			return targetList.Contains(candidate);
		}
		return false;
	}

	public void TargetListChanged()
	{
		if (aircraft.networked && aircraft.weaponStations.Count != 0)
		{
			int num = targetList.Count;
			if (targetList.Count > 128)
			{
				num = 128;
				ColorLog<WeaponManager>.InfoWarn("SetStationTargets can only have a max of 128 targets");
			}
			Span<PersistentID> span = stackalloc PersistentID[num];
			for (int i = 0; i < num; i++)
			{
				span[i] = targetList[i].persistentID;
			}
			aircraft.SetStationTargets(currentWeaponStation.Number, span);
		}
	}

	public void SetTargetList(ReadOnlySpan<PersistentID> targetIDs)
	{
		targetList.Clear();
		ReadOnlySpan<PersistentID> readOnlySpan = targetIDs;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			if (UnitRegistry.TryGetUnit(readOnlySpan[i], out var unit))
			{
				targetList.Add(unit);
			}
		}
	}

	public void SetActiveStation(byte stationIndex)
	{
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			weaponStation.SetStationActive(aircraft, weaponStation.Number == stationIndex);
		}
		currentWeaponStation = aircraft.weaponStations[stationIndex];
		Span<PersistentID> span = stackalloc PersistentID[targetList.Count];
		for (int i = 0; i < targetList.Count; i++)
		{
			span[i] = targetList[i].persistentID;
		}
		currentWeaponStation.SetStationTargets(span);
	}

	private void WeaponManager_OnInitialize()
	{
		aircraft.onInitialize -= WeaponManager_OnInitialize;
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			InitializeWeaponManager();
		}
	}

	public void WeaponManager_OnLoadoutChanged()
	{
		SpawnWeapons();
	}

	public void InitializeWeaponManager()
	{
		if (aircraft.loadout == null || aircraft.loadout.weapons.Count != hardpointSets.Length)
		{
			aircraft.Networkloadout = aircraft.definition.aircraftParameters.loadouts[1];
		}
		aircraft.onLoadoutChanged += WeaponManager_OnLoadoutChanged;
		SpawnWeapons();
	}

	public void InvokeOnStationFired()
	{
		this.OnStationFired?.Invoke();
	}

	public void RemoveWeapons()
	{
		HardpointSet[] array = hardpointSets;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].RemoveMounts();
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			aircraft.ClearWeaponStates();
		}
		aircraft.ClearWeaponStations();
	}

	public void ReturnWeapons()
	{
		if (aircraft.IsServer && aircraft.networked)
		{
			int currentWarheads = GetCurrentWarheads();
			float currentValue = GetCurrentValue(includeCargo: true);
			if (aircraft.Player != null)
			{
				aircraft.Player.AddAllocation(currentValue);
			}
			if (aircraft.NetworkHQ != null)
			{
				Airbase nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position);
				if (currentWarheads != 0 && nearestAirbase != null)
				{
					nearestAirbase.AddWarheads(currentWarheads);
				}
			}
		}
		RemoveWeapons();
	}

	public void SpawnWeapons()
	{
		float num = 0f;
		int currentWarheads = GetCurrentWarheads();
		if (aircraft.IsServer && aircraft.networked && aircraft.Player != null)
		{
			num = GetCurrentValue(includeCargo: true);
		}
		if (currentWeaponStation != null)
		{
			_ = currentWeaponStation.Number;
		}
		RemoveWeapons();
		colorables.Clear();
		skinnables.Clear();
		for (int i = 0; i < hardpointSets.Length; i++)
		{
			if (i < aircraft.loadout.weapons.Count)
			{
				HardpointSet hardpointSet = hardpointSets[i];
				WeaponMount weaponMount = aircraft.loadout.weapons[i];
				LoadHardpointSet(hardpointSet, weaponMount);
			}
		}
		OrganizeWeaponStations();
		if (aircraft.IsServer && aircraft.networked)
		{
			if (aircraft.Player != null)
			{
				float num2 = GetCurrentValue(includeCargo: true) - num;
				aircraft.Player.AddAllocation(0f - num2);
			}
			if (aircraft.NetworkHQ != null)
			{
				int num3 = GetCurrentWarheads() - currentWarheads;
				Airbase nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position);
				if (num3 != 0 && nearestAirbase != null)
				{
					if (num3 > 0)
					{
						nearestAirbase.RemoveWarheads(num3);
					}
					else
					{
						nearestAirbase.AddWarheads(-num3);
					}
				}
			}
		}
		this.OnWeaponsLoaded?.Invoke();
		if (PlayerSettings.debugVis)
		{
			aircraft.DebugCoM();
		}
	}

	public void RegisterWeapon(Weapon weapon, WeaponMount weaponMount, Hardpoint hardpoint)
	{
		if (!StationExistsForWeaponInfo(weapon.info, weaponMount.Cargo, out var existingStation))
		{
			existingStation = new WeaponStation(aircraft, weaponMount.Cargo, weaponMount.GearSafety, weaponMount.GroundSafety, weaponMount.sortWeapons);
			aircraft.RegisterWeaponStation(existingStation);
		}
		existingStation.RegisterWeapon(weapon, aircraft, weaponMount, hardpoint);
		if (hardpoint.HardpointIndex >= 0)
		{
			if (!HardpointsIndexes.TryGetValue(hardpoint.HardpointIndex, out var value))
			{
				HardpointsIndexes.Add(hardpoint.HardpointIndex, new List<Weapon> { weapon });
			}
			else
			{
				value.Add(weapon);
			}
		}
	}

	private bool StationExistsForWeaponInfo(WeaponInfo weaponInfo, bool cargo, out WeaponStation existingStation)
	{
		existingStation = null;
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (cargo && weaponStation.Cargo)
			{
				existingStation = weaponStation;
				return true;
			}
			if (weaponInfo != null && weaponStation.WeaponInfo == weaponInfo)
			{
				existingStation = weaponStation;
				return true;
			}
		}
		return false;
	}

	private void LoadHardpointSet(HardpointSet hardpointSet, WeaponMount weaponMount)
	{
		if (weaponMount == null || (aircraft.NetworkHQ != null && aircraft.NetworkHQ.restrictedWeapons.Contains(weaponMount.name)))
		{
			hardpointSet.RemoveMounts();
		}
		else
		{
			hardpointSet.SpawnMounts(aircraft, weaponMount);
		}
	}

	private void OrganizeWeaponStations()
	{
		UpdateColorables(liveryData);
		for (int num = aircraft.weaponStations.Count - 1; num >= 0; num--)
		{
			aircraft.weaponStations[num].TypeLookup = new Dictionary<UnitDefinition, OpportunityThreat>();
			aircraft.weaponStations[num].Number = (byte)num;
			aircraft.weaponStations[num].SortWeapons(aircraft);
			aircraft.weaponStations[num].AccountAmmo();
		}
		if (aircraft.weaponStations.Count > 0)
		{
			currentWeaponStation = aircraft.weaponStations[0];
			currentWeaponStation.SetStationActive(aircraft, isActive: true);
			TargetListChanged();
		}
		else
		{
			currentWeaponStation = null;
		}
		if (SceneSingleton<CombatHUD>.i.aircraft == aircraft)
		{
			SceneSingleton<CombatHUD>.i.ShowWeaponStation(currentWeaponStation);
		}
	}

	public void RegisterColorable(Renderer renderer)
	{
		colorables.Add(renderer);
	}

	public void RegisterSkinnable(Renderer renderer)
	{
		skinnables.Add(renderer);
	}

	public int StationsWithTurrets()
	{
		int num = 0;
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.HasTurret())
			{
				num++;
			}
		}
		return num;
	}

	public bool HasTailHook()
	{
		HardpointSet[] array = hardpointSets;
		foreach (HardpointSet hardpointSet in array)
		{
			if (hardpointSet.weaponMount != null && hardpointSet.weaponMount.tailHook)
			{
				return true;
			}
		}
		return false;
	}

	public void NextWeaponStation()
	{
		if (aircraft.weaponStations.Count != 0 && aircraft.weaponStations.Count != 1)
		{
			int num = ((currentWeaponStation.Number < aircraft.weaponStations.Count - 1) ? (currentWeaponStation.Number + 1) : 0);
			Span<PersistentID> span = stackalloc PersistentID[targetList.Count];
			for (int i = 0; i < targetList.Count; i++)
			{
				span[i] = targetList[i].persistentID;
			}
			currentWeaponStation = aircraft.weaponStations[num];
			aircraft.SetActiveStation((byte)num);
			SceneSingleton<CombatHUD>.i.ShowWeaponStation(currentWeaponStation);
		}
	}

	public void PreviousWeaponStation()
	{
		if (aircraft.weaponStations.Count != 0 && aircraft.weaponStations.Count != 1)
		{
			int num = ((currentWeaponStation.Number > 0) ? (currentWeaponStation.Number - 1) : (aircraft.weaponStations.Count - 1));
			Span<PersistentID> span = stackalloc PersistentID[targetList.Count];
			for (int i = 0; i < targetList.Count; i++)
			{
				span[i] = targetList[i].persistentID;
			}
			currentWeaponStation = aircraft.weaponStations[num];
			aircraft.SetActiveStation((byte)num);
			SceneSingleton<CombatHUD>.i.ShowWeaponStation(currentWeaponStation);
		}
	}

	public void FireGuns()
	{
		Unit target = ((targetList.Count == 0) ? null : targetList[0]);
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.WeaponInfo.gun && weaponStation.TurretCount() == 0 && weaponStation.Ready())
			{
				weaponStation.Fire(aircraft, target);
			}
		}
	}

	public void Fire()
	{
		if (currentWeaponStation == null || currentWeaponStation.SafetyIsOn(aircraft) || aircraft.weaponStations.Count == 0)
		{
			return;
		}
		if (currentWeaponStation.WeaponInfo.gun && gunsLinked)
		{
			FireGuns();
			return;
		}
		Unit target = ((targetList.Count == 0) ? null : targetList[0]);
		if (!aircraft.remoteSim && currentWeaponStation.Ready() && !currentWeaponStation.SalvoInProgress)
		{
			if (currentWeaponStation.WeaponInfo.gun || currentWeaponStation.WeaponInfo.fireInterval == 0f || currentWeaponStation.WeaponInfo.sling)
			{
				currentWeaponStation.Fire(aircraft, target);
			}
			else if (targetList.Count > 1)
			{
				currentWeaponStation.SalvoInProgress = true;
				SalvoFire(targetList, currentWeaponStation.WeaponInfo.fireInterval * 1.1f).Forget();
			}
			else
			{
				currentWeaponStation.LaunchMount(aircraft, target, aircraft.GlobalPosition() + aircraft.transform.forward * 50000f);
			}
		}
	}

	private async UniTask SalvoFire(List<Unit> targets, float salvoInterval)
	{
		int i = 0;
		WeaponStation salvoStation = currentWeaponStation;
		CancellationToken cancel = base.destroyCancellationToken;
		while (i < targets.Count)
		{
			Unit unit = targets[i];
			if (unit != null && !unit.disabled)
			{
				salvoStation.LaunchMount(aircraft, targets[i], default(GlobalPosition));
			}
			i++;
			await UniTask.Delay((int)(salvoInterval * 1000f));
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		salvoStation.SalvoInProgress = false;
	}

	public void ToggleGunsLinked()
	{
		gunsLinked = !gunsLinked;
		string report = (gunsLinked ? "All Guns <b>Linked</b>" : "Guns <b>Split</b>");
		SceneSingleton<AircraftActionsReport>.i.ReportText(report, 5f);
	}

	public void SetGunsLinked(bool gunsLinked)
	{
		this.gunsLinked = gunsLinked;
	}

	public bool HasMultipleGuns()
	{
		int num = 0;
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.WeaponInfo.gun && weaponStation.TurretCount() == 0)
			{
				num++;
			}
		}
		return num > 1;
	}
}
