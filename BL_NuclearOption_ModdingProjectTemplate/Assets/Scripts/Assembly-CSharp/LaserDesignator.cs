using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LaserDesignator : MonoBehaviour
{
	[SerializeField]
	private UnitPart unitPart;

	[SerializeField]
	private int maxTargets;

	[SerializeField]
	private float range;

	[SerializeField]
	private Aircraft aircraft;

	private Transform xform;

	private List<Unit> targetList;

	private List<Unit> lasedTargets;

	private void Awake()
	{
		aircraft.SetLaserDesignator(this);
		unitPart.onPartDetached += LaserDesignator_OnPartDetach;
		aircraft.onDisableUnit += LaserDesignator_OnUnitDisabled;
		targetList = aircraft.weaponManager.GetTargetList();
		xform = base.transform;
		lasedTargets = new List<Unit>();
		LaseTargets().Forget();
		base.enabled = false;
	}

	public int GetMaxTargets()
	{
		return maxTargets;
	}

	private void LaserDesignator_OnPartDetach(UnitPart part)
	{
		unitPart.onPartDetached -= LaserDesignator_OnPartDetach;
		aircraft.onDisableUnit -= LaserDesignator_OnUnitDisabled;
		Object.Destroy(this);
	}

	private void LaserDesignator_OnUnitDisabled(Unit unit)
	{
		unitPart.onPartDetached -= LaserDesignator_OnPartDetach;
		aircraft.onDisableUnit -= LaserDesignator_OnUnitDisabled;
		Object.Destroy(this);
	}

	public Unit GetLasedTarget()
	{
		if (lasedTargets.Count == 0)
		{
			return null;
		}
		int index = Mathf.FloorToInt(Random.Range(0f, (float)lasedTargets.Count - 1E-05f));
		return lasedTargets[index];
	}

	private void OnDestroy()
	{
		foreach (Unit lasedTarget in lasedTargets)
		{
			FactionHQ factionHQ = aircraft?.NetworkHQ;
			if (factionHQ != null)
			{
				factionHQ.UpdateLasedState(lasedTarget, lased: false);
			}
		}
	}

	private async UniTask LaseTargets()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		while (!cancel.IsCancellationRequested)
		{
			foreach (Unit lasedTarget in lasedTargets)
			{
				aircraft.NetworkHQ.UpdateLasedState(lasedTarget, lased: false);
			}
			lasedTargets.Clear();
			for (int i = 0; i < targetList.Count && i < maxTargets; i++)
			{
				Unit unit = targetList[i];
				if (unit == null || !aircraft.NetworkHQ.IsTargetPositionAccurate(unit, 100f) || !FastMath.InRange(unit.GlobalPosition(), xform.GlobalPosition(), range) || !unit.LineOfSight(xform.position, 1000f))
				{
					continue;
				}
				lasedTargets.Add(unit);
				aircraft.NetworkHQ.UpdateLasedState(unit, lased: true);
				if (aircraft.LocalSim && unit.NetworkHQ != aircraft.NetworkHQ)
				{
					TrackingInfo trackingData = aircraft.NetworkHQ.GetTrackingData(unit.persistentID);
					if (Time.timeSinceLevelLoad - trackingData.lastSpottedTime > 2f)
					{
						aircraft.NetworkHQ.CmdUpdateTrackingInfo(unit.persistentID);
					}
				}
			}
			await UniTask.Delay(250);
			await UniTask.WaitForFixedUpdate();
		}
	}

	public List<Unit> GetLasedTargets()
	{
		return lasedTargets;
	}

	public int LasedTargetCount()
	{
		return lasedTargets.Count;
	}

	public bool IsLased(Unit unit)
	{
		return lasedTargets.Contains(unit);
	}
}
