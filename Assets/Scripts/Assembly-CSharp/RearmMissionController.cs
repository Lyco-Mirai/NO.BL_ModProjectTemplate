using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Collections;
using UnityEngine;

[Serializable]
public class RearmMissionController : NetworkBehaviour
{
	private readonly struct RearmerMissionMatchup
	{
		public readonly RearmMission mission;

		public readonly Rearmer rearmer;

		public readonly GlobalPosition pos;

		public readonly float dist;

		public RearmerMissionMatchup(RearmMission mission, Rearmer rearmer)
		{
			this.mission = mission;
			pos = mission.Requester.GlobalPosition();
			dist = FastMath.SquareDistance(mission.Requester.GlobalPosition(), rearmer.Unit.GlobalPosition());
			this.rearmer = rearmer;
		}
	}

	[Serializable]
	public readonly struct RearmerWithMission
	{
		public readonly Unit RearmerUnit;

		public readonly Unit RequesterUnit;

		public RearmerWithMission(Unit rearmerUnit, Unit requesterUnit)
		{
			RearmerUnit = rearmerUnit;
			RequesterUnit = requesterUnit;
		}
	}

	[Serializable]
	public class RearmMission
	{
		public Unit Requester;

		public Rearmer Rearmer;

		public float Value;

		public float MaxRange;

		public bool Matched;

		public RearmMission(Unit requester, Rearmer rearmer)
		{
			Requester = requester;
			Rearmer = rearmer;
			Value = 0f;
			foreach (WeaponStation weaponStation in Requester.weaponStations)
			{
				int num = weaponStation.FullAmmo - weaponStation.Ammo;
				Value += weaponStation.WeaponInfo.costPerRound * (float)num;
			}
			MaxRange = Mathf.Max(Mathf.Sqrt(Value) * 8000f, 5000f);
		}

		public void RearmMission_OnRearmerRequestCancel()
		{
			AssignRearmer(null);
		}

		public void AssignRearmer(Rearmer newRearmer)
		{
			if (!(newRearmer == Rearmer))
			{
				if (Rearmer != null && Rearmer.gameObject.TryGetComponent<RearmVehicleAI>(out var component))
				{
					component.OnRequestCancelMission -= RearmMission_OnRearmerRequestCancel;
					component.AssignMission(null);
				}
				if (newRearmer != null && newRearmer.gameObject.TryGetComponent<RearmVehicleAI>(out var component2))
				{
					component2.OnRequestCancelMission += RearmMission_OnRearmerRequestCancel;
					component2.AssignMission(Requester);
				}
				Rearmer = newRearmer;
			}
		}
	}

	private static List<RearmerMissionMatchup> rearmerMissionMatchups = new List<RearmerMissionMatchup>();

	private static List<RearmMission> filteredMissions = new List<RearmMission>();

	private static List<Rearmer> availableRearmersCache = new List<Rearmer>();

	public readonly List<RearmMission> Missions = new List<RearmMission>();

	public readonly SyncList<RearmerWithMission> RearmersWithMissions = new SyncList<RearmerWithMission>();

	public List<Rearmer> Rearmers = new List<Rearmer>();

	public readonly SyncList<Unit> UnitsNeedingRearm = new SyncList<Unit>();

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 0;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	public event Action OnChange;

	private void OnStartClient()
	{
		if (base.IsServer)
		{
			this.StartSlowUpdateDelayed(10f, ManageRearmMissions);
		}
	}

	private void Awake()
	{
		base.Identity.OnStartClient.AddListener(OnStartClient);
		UnitsNeedingRearm.OnChange += OnUnitsNeedingRearmChanged;
		RearmersWithMissions.OnChange += OnRearmersWithMissionsChanged;
	}

	private void OnRearmersWithMissionsChanged()
	{
		this.OnChange?.Invoke();
	}

	private void OnUnitsNeedingRearmChanged()
	{
		this.OnChange?.Invoke();
	}

	public void RegisterRearmer(Rearmer rearmer)
	{
		if (!Rearmers.Contains(rearmer))
		{
			Rearmers.Add(rearmer);
		}
	}

	public void DeregisterRearmer(Rearmer rearmer)
	{
		Rearmers.Remove(rearmer);
	}

	public void DeregisterNeedsRearm(Unit requester)
	{
		if (base.IsServer)
		{
			UnitsNeedingRearm.Remove(requester);
		}
		for (int num = Missions.Count - 1; num >= 0; num--)
		{
			if (Missions[num].Requester == requester)
			{
				Missions[num].AssignRearmer(null);
				Missions.RemoveAt(num);
			}
		}
	}

	public void RegisterNeedsRearm(Unit requester)
	{
		if (base.IsServer)
		{
			UnitsNeedingRearm.Add(requester);
		}
		if (!(requester is Ship) && !(requester is Aircraft))
		{
			CreateRearmMission(requester).Forget();
		}
	}

	private async UniTask CreateRearmMission(Unit requester)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.WaitForSeconds(5);
		if (!cancel.IsCancellationRequested && requester.HasRequestedRearm)
		{
			Missions.Add(new RearmMission(requester, null));
		}
	}

	public void ManageRearmMissions()
	{
		rearmerMissionMatchups.Clear();
		filteredMissions.Clear();
		RearmersWithMissions.Clear();
		for (int num = Missions.Count - 1; num >= 0; num--)
		{
			RearmMission rearmMission = Missions[num];
			if (rearmMission.Requester == null || rearmMission.Requester.disabled)
			{
				Missions.RemoveAt(num);
			}
			else if (rearmMission.Requester.speed > 1f || rearmMission.Requester.radarAlt > 1f)
			{
				rearmMission.AssignRearmer(null);
			}
			else if (rearmMission.Rearmer == null || rearmMission.Rearmer.AvailableForMission)
			{
				rearmMission.AssignRearmer(null);
				filteredMissions.Add(rearmMission);
			}
		}
		if (filteredMissions.Count == 0)
		{
			return;
		}
		foreach (Rearmer rearmer in Rearmers)
		{
			GlobalPosition b = rearmer.Unit.GlobalPosition();
			if (!rearmer.AvailableForMission)
			{
				continue;
			}
			foreach (RearmMission filteredMission in filteredMissions)
			{
				filteredMission.Matched = false;
				if (FastMath.InRange(filteredMission.Requester.GlobalPosition(), b, filteredMission.MaxRange))
				{
					rearmer.Matched = false;
					rearmerMissionMatchups.Add(new RearmerMissionMatchup(filteredMission, rearmer));
				}
			}
		}
		rearmerMissionMatchups.Sort(delegate(RearmerMissionMatchup a, RearmerMissionMatchup rearmerMissionMatchup2)
		{
			float dist = a.dist;
			return dist.CompareTo(rearmerMissionMatchup2.dist);
		});
		for (int num2 = 0; num2 < rearmerMissionMatchups.Count; num2++)
		{
			RearmerMissionMatchup rearmerMissionMatchup = rearmerMissionMatchups[num2];
			if (!rearmerMissionMatchup.mission.Matched && !rearmerMissionMatchup.rearmer.Matched)
			{
				RearmersWithMissions.Add(new RearmerWithMission(rearmerMissionMatchup.rearmer.Unit, rearmerMissionMatchup.mission.Requester));
				rearmerMissionMatchup.mission.AssignRearmer(rearmerMissionMatchup.rearmer);
				rearmerMissionMatchup.rearmer.Matched = true;
				rearmerMissionMatchup.mission.Matched = true;
			}
		}
	}

	public bool TryGetUnitNeedingRearm(bool ships, bool vehicles, out Unit lowestAmmoUnit)
	{
		lowestAmmoUnit = null;
		float num = 0f;
		for (int num2 = UnitsNeedingRearm.Count - 1; num2 >= 0; num2--)
		{
			Unit unit = UnitsNeedingRearm[num2];
			if (unit == null || unit.disabled || unit.radarAlt > 10f)
			{
				if (base.IsServer)
				{
					UnitsNeedingRearm.RemoveAt(num2);
				}
			}
			else if (!(unit is Aircraft) && (ships || !(unit is Ship)) && (vehicles || !(unit is GroundVehicle)))
			{
				float missing = unit.GetAmmoValue().Missing;
				if (missing > num)
				{
					lowestAmmoUnit = unit;
					num = missing;
				}
			}
		}
		return lowestAmmoUnit != null;
	}

	public bool TryGetNearestRearmer(GlobalPosition fromPosition, out Rearmer nearestRearmer, bool includeShips = false, float minCapacity = 0f)
	{
		nearestRearmer = null;
		float range = float.MaxValue;
		foreach (Rearmer rearmer in Rearmers)
		{
			if ((includeShips || !(rearmer.Unit is Ship)) && rearmer.Capacity >= minCapacity && FastMath.InRange(fromPosition, rearmer.transform.GlobalPosition(), range))
			{
				nearestRearmer = rearmer;
				range = FastMath.Distance(fromPosition, rearmer.transform.GlobalPosition());
			}
		}
		return nearestRearmer != null;
	}

	public bool TryGetRearmer(Unit requestingUnit, out Rearmer bestRearmer)
	{
		bestRearmer = null;
		if (requestingUnit.radarAlt > 1f || (requestingUnit.speed > 1f && !(requestingUnit is Ship)))
		{
			return false;
		}
		GlobalPosition a = requestingUnit.GlobalPosition();
		availableRearmersCache.Clear();
		foreach (Rearmer rearmer in Rearmers)
		{
			if (rearmer.Unit != requestingUnit && rearmer.Capacity > 0f && FastMath.InRange(a, rearmer.GetPosition(), rearmer.Range))
			{
				availableRearmersCache.Add(rearmer);
			}
		}
		if (availableRearmersCache.Count == 0)
		{
			return false;
		}
		availableRearmersCache.Sort((Rearmer rearmer, Rearmer b) => b.Capacity.CompareTo(rearmer.Capacity));
		bestRearmer = availableRearmersCache[0];
		return true;
	}

	public RearmMissionController()
	{
		InitSyncObject(RearmersWithMissions);
		InitSyncObject(UnitsNeedingRearm);
	}

	private void MirageProcessed()
	{
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
