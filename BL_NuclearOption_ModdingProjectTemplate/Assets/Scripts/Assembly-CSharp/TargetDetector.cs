using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Jobs;
using NuclearOption.Networking;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
	[Serializable]
	protected class Rotator
	{
		public Transform transform;

		public Vector3 axis;

		public void Reset()
		{
			transform.localRotation = Quaternion.identity;
		}
	}

	[SerializeField]
	protected Unit attachedUnit;

	[SerializeField]
	protected Transform scanner;

	[SerializeField]
	protected UnitPart part;

	[Tooltip("in seconds")]
	[SerializeField]
	private float checkInterval;

	[Tooltip("in seconds")]
	[SerializeField]
	private float alertCheckInterval;

	[SerializeField]
	protected float visualRange;

	[SerializeField]
	protected float magnification;

	[SerializeField]
	protected float maxSpeed;

	[SerializeField]
	protected Rotator[] rotators;

	[SerializeField]
	protected bool shared;

	[HideInInspector]
	public bool activated;

	protected List<Unit> unitsInRange = new List<Unit>(100);

	public List<Unit> detectedTargets = new List<Unit>();

	protected bool disabled;

	private float rewardAmount;

	private float rewardCount;

	private float rewardThreshold = 1f;

	public event Action onScan;

	public event Action<Unit> onDetectTarget;

	public event Action<TargetDetector> onDisabled;

	protected virtual void OnDestroy()
	{
		this.onDetectTarget = null;
		this.onDisabled = null;
		unitsInRange.Clear();
		detectedTargets.Clear();
	}

	public Transform GetScanPoint()
	{
		return scanner;
	}

	public virtual float GetRadarRange()
	{
		return 0f;
	}

	public float GetVisualRange()
	{
		return visualRange;
	}

	public float GetVisualMagnification()
	{
		return magnification;
	}

	public Unit GetAttachedUnit()
	{
		return attachedUnit;
	}

	public bool IsOperational()
	{
		return !disabled;
	}

	public Vector3 GetVelocity()
	{
		if (part == null)
		{
			return Vector3.zero;
		}
		if (!(part.rb != null))
		{
			return Vector3.zero;
		}
		return part.rb.velocity;
	}

	public void SetAttachedUnit(Unit unit)
	{
		attachedUnit = unit;
	}

	protected virtual void Awake()
	{
		base.enabled = false;
		attachedUnit.onInitialize += TargetDetector_OnInitialize;
		if (part != null)
		{
			part.onApplyDamage += TargetDetector_OnApplyDamage;
		}
		attachedUnit.onDisableUnit += TargetDetector_OnUnitDisabled;
	}

	protected void TargetDetector_OnInitialize()
	{
		activated = true;
		if (attachedUnit.IsServer || GameManager.IsLocalAircraft(attachedUnit))
		{
			RepeatSearch().Forget();
		}
		if (shared && attachedUnit.NetworkHQ != null && this is Radar radar)
		{
			attachedUnit.NetworkHQ.RegisterRadar(radar);
		}
	}

	protected virtual void TargetDetector_OnApplyDamage(UnitPart.OnApplyDamage e)
	{
	}

	protected void DisableTargetDetector()
	{
		disabled = true;
		this.onDisabled?.Invoke(this);
		UnityEngine.Object.Destroy(this);
	}

	protected virtual void TargetDetector_OnUnitDisabled(Unit unit)
	{
		DisableTargetDetector();
		attachedUnit.onDisableUnit -= TargetDetector_OnUnitDisabled;
		UnityEngine.Object.Destroy(this);
	}

	private async UniTask RepeatSearch()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)(UnityEngine.Random.value * checkInterval * 1000f));
		while (!cancel.IsCancellationRequested)
		{
			if (activated && scanner != null && !attachedUnit.disabled && attachedUnit.NetworkHQ != null)
			{
				detectedTargets.Clear();
				TargetSearch();
				this.onScan?.Invoke();
			}
			await UniTask.Delay((int)(((alertCheckInterval == 0f || detectedTargets.Count == 0) ? checkInterval : alertCheckInterval) * 1000f));
		}
	}

	protected virtual void TargetSearch()
	{
		VisualCheck();
	}

	public void DetectTarget(Unit target)
	{
		detectedTargets.Add(target);
		this.onDetectTarget?.Invoke(target);
		if (!NetworkManagerNuclearOption.i.Server.Active)
		{
			return;
		}
		Player player = null;
		if (attachedUnit is Missile missile && missile.owner != null)
		{
			player = missile.owner.GetPlayer();
		}
		if (attachedUnit is Aircraft unit)
		{
			player = unit.GetPlayer();
		}/*
		if (player != null && target.NetworkHQ != null && target.NetworkHQ != player.HQ)
		{
			float num = 0f;
			if (!player.HQ.trackingDatabase.ContainsKey(target.persistentID))
			{
				num = 0.05f;
			}
			else if (!player.HQ.IsTargetPositionAccurate(target, 500f))
			{
				num = 0.01f;
			}
			if (target is Missile missile2 && missile2.definition.value < 25f)
			{
				num = 0f;
			}
			rewardCount += num * Mathf.Sqrt(target.definition.value);
			rewardAmount += num * Mathf.Sqrt(target.definition.value);
			if (rewardCount > rewardThreshold)
			{
				player.HQ.ReportReconAction(player, rewardAmount);
				rewardAmount = 0f;
				rewardCount = 0f;
			}
		}*/
		attachedUnit.NetworkHQ.RpcUpdateTrackingInfo(target.persistentID);
	}

	public bool InVisualRange(Unit target)
	{
		return FastMath.InRange(scanner.GlobalPosition(), target.GlobalPosition(), target.GetVisibility() * magnification);
	}

	protected void VisualCheck()
	{
		BattlefieldGrid.GetUnitsInRangeNonAlloc(scanner.GlobalPosition(), visualRange, unitsInRange);
		foreach (Unit item in unitsInRange)
		{
			if (!(item.NetworkHQ == attachedUnit.NetworkHQ) && !item.disabled && !detectedTargets.Contains(item) && !(item.speed > maxSpeed))
			{
				DetectorManager.RequestLoSCheck(this, item);
			}
		}
	}
}
