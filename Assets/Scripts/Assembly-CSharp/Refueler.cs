using NuclearOption.Networking;
using UnityEngine;

public class Refueler : MonoBehaviour
{
	[SerializeField]
	private Unit attachedUnit;

	[SerializeField]
	private float range = 300f;

	[SerializeField]
	private bool singleUse;

	[Tooltip("in seconds")]
	[SerializeField]
	private float checkInterval = 5f;

	private void Start()
	{
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			attachedUnit.StartSlowUpdateDelayed(checkInterval, SlowUpdate);
		}
	}

	private void SlowUpdate()
	{
		if (attachedUnit.disabled)
		{
			return;
		}
		foreach (Unit item in BattlefieldGrid.GetUnitsInRangeEnumerable(attachedUnit.GlobalPosition(), range))
		{
			if (item.NetworkHQ != attachedUnit.NetworkHQ || !(item is IRefuelable refuelable) || !refuelable.CanRefuel() || !FastMath.InRange(item.transform.position, base.transform.position, range))
			{
				continue;
			}
			bool flag = false;
			float num = 0f;
			Aircraft aircraft = null;
			if (attachedUnit is Container container)
			{
				if (UnitRegistry.TryGetUnit(container.ownerID, out var unit))
				{
					aircraft = unit as Aircraft;
				}
				if (aircraft != null && aircraft.Player != null)
				{
					flag = true;
				}
			}
			refuelable.Refuel(attachedUnit);
			if (singleUse)
			{
				attachedUnit.Networkdisabled = true;
			}
			if (refuelable != aircraft && flag)
			{
				num += 0.15f * Mathf.Sqrt(item.definition.value);
			}
			if (num > 0f)
			{
				aircraft.NetworkHQ.ReportRefuelAction(aircraft.Player, null, num);
			}
		}
	}
}
