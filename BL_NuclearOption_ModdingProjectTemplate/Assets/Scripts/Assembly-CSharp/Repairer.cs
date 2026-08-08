using NuclearOption.Networking;
using UnityEngine;

public class Repairer : MonoBehaviour
{
	[SerializeField]
	private Unit attachedUnit;

	[SerializeField]
	private float radius = 1000f;

	[SerializeField]
	private float repairRate = 5f;

	[Tooltip("in seconds")]
	[SerializeField]
	private float checkInterval = 30f;

	private float lastRepairCheck;

	private Unit unitToRepair;

	private IRepairable repairInProgress;

	private float repairValue;

	private void Awake()
	{
		attachedUnit.onInitialize += Repairer_OnInitialize;
	}

	private void Repairer_OnInitialize()
	{
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			this.StartSlowUpdateDelayed(5f, Repair);
		}
	}

	private void SearchForRepair()
	{
		if (Time.timeSinceLevelLoad - lastRepairCheck < 30f)
		{
			return;
		}
		if (repairInProgress != null)
		{
			if (repairInProgress.NeedsRepair())
			{
				return;
			}
			repairInProgress = null;
			unitToRepair = null;
		}
		lastRepairCheck = Time.timeSinceLevelLoad;
		Unit unit = unitToRepair;
		attachedUnit.NetworkHQ.TryGetUnitNeedingRepair(attachedUnit, radius, out unitToRepair);
		if (unitToRepair != null && unitToRepair != unit && attachedUnit is ICommandable commandable)
		{
			Vector3 vector = unitToRepair.GlobalPosition() - attachedUnit.GlobalPosition();
			vector.y = 0f;
			GlobalPosition waypoint = unitToRepair.startPosition - unitToRepair.maxRadius * 2f * vector.normalized;
			commandable.UnitCommand.SetDestination(waypoint, playerCommand: false);
		}
	}

	private void Repair()
	{
		if (attachedUnit.disabled || attachedUnit.radarAlt > 2f)
		{
			return;
		}
		SearchForRepair();
		if (unitToRepair == null)
		{
			return;
		}
		repairInProgress = (FastMath.InRange(unitToRepair.startPosition, attachedUnit.GlobalPosition(), radius) ? (unitToRepair as IRepairable) : null);
		if (repairInProgress == null || !repairInProgress.NeedsRepair())
		{
			unitToRepair = null;
			return;
		}
		repairInProgress.Repair(attachedUnit, repairRate);
		repairValue += 0.01f * repairRate * Mathf.Sqrt(unitToRepair.definition.value);
		if (!repairInProgress.NeedsRepair())
		{
			lastRepairCheck = Time.timeSinceLevelLoad - 29f;
			repairInProgress = null;
			/*
			if (attachedUnit is GroundVehicle { Networkowner: var networkowner } groundVehicle && networkowner != null && networkowner.HQ != null)
			{
				repairValue = Mathf.Sqrt(repairValue);
				groundVehicle.Networkowner.HQ.ReportRepairAction(networkowner, repairValue, repairValue);
				repairValue = 0f;
			}*/
		}
	}
}
