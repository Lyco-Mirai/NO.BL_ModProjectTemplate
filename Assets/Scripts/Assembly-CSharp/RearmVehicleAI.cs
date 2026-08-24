using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RearmVehicleAI : MonoBehaviour
{
	private enum AIState
	{
		LeaveDepot = 0,
		Wait = 1,
		DrivingToRestock = 2,
		ReloadingFromRearmer = 3,
		DrivingRearmMission = 4
	}

	[SerializeField]
	private AIState currentState;

	[SerializeField]
	private GroundVehicle vehicle;

	[SerializeField]
	private Rearmer rearmer;

	private float lastRearm;

	private float lastStuck;

	public Unit UnitToRearm;

	private Unit unitToRearmPrev;

	public event Action OnRequestCancelMission;

	private void Awake()
	{
		vehicle.onInitialize += RearmVehicleAI_OnInitialize;
	}

	public void AssignMission(Unit unitToRearm)
	{
		UnitToRearm = unitToRearm;
		if (unitToRearm != null)
		{
			DriveRearmMission().Forget();
		}
	}

	public string GetStateName()
	{
		return currentState switch
		{
			AIState.LeaveDepot => "Leaving Depot", 
			AIState.Wait => "Waiting", 
			AIState.DrivingToRestock => "Driving to Restock", 
			AIState.ReloadingFromRearmer => "Reloading from Rearmer", 
			AIState.DrivingRearmMission => "Driving Rearm Mission", 
			_ => string.Empty, 
		};
	}

	public Unit GetTarget()
	{
		return UnitToRearm;
	}

	private void RearmVehicleAI_OnInitialize()
	{
		if (vehicle.IsServer && !vehicle.GetHoldPosition())
		{
			rearmer.OnRearmProcessed += RearmVehicleAI_OnRearmProcessed;
			LeaveDepot().Forget();
		}
	}

	private void RearmVehicleAI_OnRearmProcessed(RearmerEventArgs e)
	{
		lastRearm = Time.timeSinceLevelLoad;
		if (!vehicle.GetHoldPosition() && ((e.Shortfall > 0f && e.CapacityProportion < 0.5f) || e.CapacityProportion < 0.05f))
		{
			this.OnRequestCancelMission?.Invoke();
			DriveToRestock().Forget();
		}
	}

	private async UniTask LeaveDepot()
	{
		currentState = AIState.LeaveDepot;
		CancellationToken cancel = base.destroyCancellationToken;
		rearmer.AvailableForMission = false;
		while (currentState == AIState.LeaveDepot && !cancel.IsCancellationRequested)
		{
			await UniTask.WaitForSeconds(5);
			if (cancel.IsCancellationRequested)
			{
				break;
			}
			if (vehicle.speed < 1f)
			{
				Wait().Forget();
			}
		}
	}

	private async UniTask Wait()
	{
		if (currentState != AIState.Wait)
		{
			currentState = AIState.Wait;
			vehicle.StopImmediately();
			CancellationToken cancel = base.destroyCancellationToken;
			while (currentState == AIState.Wait && !cancel.IsCancellationRequested)
			{
				rearmer.AvailableForMission = Time.timeSinceLevelLoad - Mathf.Max(lastRearm, lastStuck) > 30f;
				await UniTask.WaitForSeconds(10);
			}
		}
	}

	private void ReloadFromRearmer(Rearmer nearestRearmer)
	{
		currentState = AIState.ReloadingFromRearmer;
		nearestRearmer.RefillOtherRearmer(rearmer);
		rearmer.AvailableForMission = true;
		Wait().Forget();
	}

	private async UniTask DriveRearmMission()
	{
		if (currentState == AIState.DrivingRearmMission)
		{
			return;
		}
		unitToRearmPrev = UnitToRearm;
		currentState = AIState.DrivingRearmMission;
		CancellationToken cancel = base.destroyCancellationToken;
		GlobalPosition destination = vehicle.GlobalPosition();
		GlobalPosition stuckPosition = vehicle.GlobalPosition();
		float stuckTimer = 0f;
		int missionSwitchCounter = 0;
		while (currentState == AIState.DrivingRearmMission && !cancel.IsCancellationRequested)
		{
			GlobalPosition globalPosition = destination;
			if (UnitToRearm == null || UnitToRearm.disabled)
			{
				if (FastMath.OutOfRange(globalPosition, vehicle.GlobalPosition(), 400f))
				{
					Wait().Forget();
					break;
				}
			}
			else
			{
				globalPosition = UnitToRearm.GlobalPosition();
				if (unitToRearmPrev != UnitToRearm)
				{
					unitToRearmPrev = UnitToRearm;
					missionSwitchCounter++;
				}
			}
			rearmer.AvailableForMission = missionSwitchCounter < 2;
			if (FastMath.OutOfRange(destination, globalPosition, 150f))
			{
				vehicle.UnitCommand.SetDestination(globalPosition, playerCommand: false);
				destination = globalPosition;
			}
			if (FastMath.InRange(globalPosition, vehicle.GlobalPosition(), 150f))
			{
				stuckTimer = 0f;
				if (vehicle.speed < 2f || FastMath.InRange(globalPosition, vehicle.GlobalPosition(), 50f))
				{
					Wait().Forget();
					break;
				}
			}
			else if (FastMath.InRange(vehicle.GlobalPosition(), stuckPosition, 20f))
			{
				stuckTimer += 5f;
				if (stuckTimer > 20f)
				{
					this.OnRequestCancelMission?.Invoke();
					lastStuck = Time.timeSinceLevelLoad;
					Wait().Forget();
					break;
				}
			}
			else
			{
				stuckPosition = vehicle.GlobalPosition();
				stuckTimer = 0f;
			}
			await UniTask.WaitForSeconds(5);
		}
	}

	private async UniTask DriveToRestock()
	{
		if (currentState == AIState.DrivingToRestock)
		{
			return;
		}
		rearmer.AvailableForMission = false;
		currentState = AIState.DrivingToRestock;
		CancellationToken cancel = base.destroyCancellationToken;
		if (vehicle.NetworkHQ == null)
		{
			return;
		}
		GlobalPosition destination = vehicle.GlobalPosition();
		float lastTargetCheck = 0f;
		Rearmer nearestRearmer = null;
		while (currentState == AIState.DrivingToRestock && !cancel.IsCancellationRequested)
		{
			if (Time.timeSinceLevelLoad - lastTargetCheck > 30f && vehicle.NetworkHQ.RearmMissionController.TryGetNearestRearmer(vehicle.GlobalPosition(), out nearestRearmer, includeShips: false, rearmer.GetMaxCapacity()))
			{
				lastTargetCheck = Time.timeSinceLevelLoad;
				if (FastMath.OutOfRange(destination, nearestRearmer.transform.GlobalPosition(), 50f))
				{
					destination = nearestRearmer.transform.GlobalPosition();
					vehicle.UnitCommand.SetDestination(destination, playerCommand: false);
				}
			}
			if (nearestRearmer != null && !nearestRearmer.Unit.disabled)
			{
				if (FastMath.InRange(vehicle.GlobalPosition(), destination, 200f))
				{
					vehicle.StopImmediately();
					ReloadFromRearmer(nearestRearmer);
				}
			}
			else
			{
				vehicle.StopImmediately();
			}
			await UniTask.WaitForSeconds(5);
		}
	}
}
