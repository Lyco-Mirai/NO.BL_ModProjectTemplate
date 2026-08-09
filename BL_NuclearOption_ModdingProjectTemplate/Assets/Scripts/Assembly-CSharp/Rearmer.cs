using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;

public class Rearmer : MonoBehaviour
{
	[SerializeField]
	private Transform accessTransform;

	public Unit Unit;

	public float Range;

	public float Capacity = 10000f;

	public bool AvailableForMission;

	private float maxCapacity;

	[SerializeField]
	private bool singleUse;

	private FactionHQ currentHQ;

	[HideInInspector]
	public bool Matched;

	public event Action<RearmerEventArgs> OnRearmProcessed;

	private void Awake()
	{
		maxCapacity = Capacity;
		Unit.onInitialize += Rearmer_OnInitialize;
		Unit.onDisableUnit += Rearmer_OnUnitDisabled;
		Unit.onChangeFaction += Rearmer_OnChangeFaction;
		if (Unit is Building building)
		{
			building.OnRepair += Rearmer_OnRepair;
		}
	}

	public float GetMaxCapacity()
	{
		return maxCapacity;
	}

	public GlobalPosition GetPosition()
	{
		return accessTransform.GlobalPosition();
	}

	private void Rearmer_OnInitialize()
	{
		Unit.onInitialize -= Rearmer_OnInitialize;
		currentHQ = Unit.NetworkHQ;
		if (Unit.NetworkHQ != null && !Unit.disabled)
		{
			Unit.NetworkHQ.RearmMissionController.RegisterRearmer(this);
		}
	}

	public void AssignMission(Unit unitToRearm)
	{
	}

	private void Rearmer_OnRepair()
	{
		if (Unit.NetworkHQ != null)
		{
			Unit.NetworkHQ.RearmMissionController.RegisterRearmer(this);
		}
	}

	private void Rearmer_OnChangeFaction(Unit unit)
	{
		if (currentHQ != null)
		{
			currentHQ.RearmMissionController.DeregisterRearmer(this);
		}
		currentHQ = unit.NetworkHQ;
		if (currentHQ != null)
		{
			currentHQ.RearmMissionController.RegisterRearmer(this);
		}
	}

	private void Rearmer_OnUnitDisabled(Unit unit)
	{
		if (unit.NetworkHQ != null)
		{
			unit.NetworkHQ.RearmMissionController.DeregisterRearmer(this);
		}
	}

	private async UniTask EmptyDisable()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.WaitForSeconds(1);
		if (!cancel.IsCancellationRequested)
		{
			Unit.Networkdisabled = true;
		}
	}

	public void SetCapacity(float capacity)
	{
		Capacity = capacity;
	}

	public void RefillOtherRearmer(Rearmer toRearmer)
	{
		float a = toRearmer.GetMaxCapacity() - toRearmer.Capacity;
		a = Mathf.Min(a, Capacity);
		float num = Capacity - a;
		Unit.RpcUpdateRearmerCapacity(num);
		if (singleUse && num < maxCapacity * 0.01f)
		{
			EmptyDisable().Forget();
			this.OnRearmProcessed?.Invoke(new RearmerEventArgs
			{
				RearmedUnit = toRearmer.Unit,
				Shortfall = 0f,
				CapacityProportion = num / maxCapacity
			});
		}
		toRearmer.Unit.RpcUpdateRearmerCapacity(toRearmer.Capacity + a);
	}

	public bool ProcessRearmRequest(Unit unitToRearm, out int shortfall)
	{
		bool flag = false;
		float num = float.MaxValue;
		Player player = unitToRearm.GetPlayer();
		if (player != null)
		{
			num = player.Allocation;
			if (unitToRearm is Aircraft aircraft && aircraft.NetworkHQ != null)
			{
				aircraft.SuccessfulSortie();
			}
		}
		float num2 = 0f;
		shortfall = 0;
		float num3 = Capacity;
		int[] array = new int[unitToRearm.weaponStations.Count];
		Airbase nearestAirbase = null;
		int num4 = 0;
		for (int i = 0; i < unitToRearm.weaponStations.Count; i++)
		{
			WeaponStation weaponStation = unitToRearm.weaponStations[i];
			float massPerRound = weaponStation.WeaponInfo.massPerRound;
			float costPerRound = weaponStation.WeaponInfo.costPerRound;
			if (weaponStation.WeaponInfo.cargo || massPerRound == 0f)
			{
				continue;
			}
			int num5 = weaponStation.FullAmmo - weaponStation.GetAmmoTotal();
			int num6 = Mathf.FloorToInt(num3 / massPerRound);
			if (num6 < 0)
			{
				num6 = int.MaxValue;
			}
			int num7 = Mathf.FloorToInt(num / costPerRound);
			if (num7 < 0)
			{
				num7 = int.MaxValue;
			}
			int num8 = int.MaxValue;
			if (weaponStation.WeaponInfo.nuclear)
			{
				num8 = ((Unit.NetworkHQ.TryGetNearestAirbase(base.transform.position, out nearestAirbase) && FastMath.InRange(Unit.GlobalPosition(), nearestAirbase.center.GlobalPosition(), nearestAirbase.GetRadius())) ? nearestAirbase.GetWarheads() : 0);
			}
			if (num8 == 0)
			{
				array[i] = -3;
			}
			if (num7 == 0)
			{
				array[i] = -2;
			}
			if (num6 == 0)
			{
				array[i] = -1;
			}
			int num9 = num6;
			shortfall += num5 - num9;
			num9 = Mathf.Min(num9, num5, num8, num7);
			if (num9 > 0)
			{
				array[i] = num9;
				num -= (float)num9 * costPerRound;
				num3 -= (float)num9 * massPerRound;
				num2 += (float)num9 * costPerRound;
				flag = true;
				if (weaponStation.WeaponInfo.nuclear)
				{
					num4 += num9;
				}
			}
		}
		if (flag)
		{
			Player player2 = Unit.GetPlayer();
			if (player2 != null)
			{
				Unit.NetworkHQ.ReportSupplyAction(player2, unitToRearm, num2);
			}
			if (player != null)
			{
				player.AddAllocation(0f - num2);
			}
			if (num4 > 0)
			{
				nearestAirbase.RemoveWarheads(num4);
			}
		}
		unitToRearm.RpcRearm(new RearmEventArgs
		{
			Rearmer = Unit,
			Stations = array
		});
		if (Capacity != num3)
		{
			Unit.RpcUpdateRearmerCapacity(num3);
		}
		float num10 = num3 / maxCapacity;
		if (singleUse && ((shortfall > 0 && num10 < 0.5f) || num10 < 0.01f))
		{
			Debug.Log("Empty Disable " + Unit.unitName);
			EmptyDisable().Forget();
		}
		this.OnRearmProcessed?.Invoke(new RearmerEventArgs
		{
			RearmedUnit = unitToRearm,
			Shortfall = shortfall,
			CapacityProportion = num10
		});
		return flag;
	}
}
