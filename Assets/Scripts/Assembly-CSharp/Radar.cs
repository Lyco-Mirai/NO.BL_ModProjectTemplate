using System.Collections.Generic;
using NuclearOption.Jobs;
using UnityEngine;

public class Radar : TargetDetector
{
	public struct RadarDetectionParameters
	{
		public readonly IRadarReturn targetReturn;

		public readonly Radar radar;

		public readonly Unit detectorUnit;

		public readonly float maxRange;

		public readonly float maxSignal;

		public readonly float minSignal;

		public readonly float dopplerFactor;

		public readonly bool triggerWarning;
	}

	[Range(0f, 1f)]
	[SerializeField]
	private float jamTolerance;

	private float jamAccumulation;

	private Aircraft aircraft;

	private PowerSupply powerSupply;

	[SerializeField]
	private float radarCone;

	[SerializeField]
	private float powerDraw;

	public RadarParams RadarParameters;

	private float powerRatio = 1f;

	private List<Missile> guidedMissiles;

	public bool IsJammed()
	{
		return jamAccumulation > jamTolerance;
	}

	protected override void Awake()
	{
		if (!(attachedUnit == null))
		{
			base.Awake();
			attachedUnit.onJam += Radar_OnJam;
			powerSupply = attachedUnit.GetPowerSupply();
			if (powerSupply != null)
			{
				powerSupply.AddUser();
			}
			base.enabled = true;
			activated = true;
			attachedUnit.radar = this;
			guidedMissiles = new List<Missile>();
			if (GameManager.IsHeadless)
			{
				base.enabled = false;
			}
		}
	}

	public void ResetRotators()
	{
		Rotator[] array = rotators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Reset();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (attachedUnit != null)
		{
			attachedUnit.onJam -= Radar_OnJam;
		}
		if (aircraft != null)
		{
			aircraft.SetRadar(null);
		}
		if (powerSupply != null)
		{
			powerSupply.RemoveUser();
		}
	}

	public void AttachToUnit(Unit unit)
	{
		if (attachedUnit != null)
		{
			attachedUnit.onJam -= Radar_OnJam;
		}
		attachedUnit = unit;
		TargetDetector_OnInitialize();
		attachedUnit.onJam += Radar_OnJam;
		powerSupply = unit.GetPowerSupply();
		if (powerSupply != null)
		{
			base.enabled = true;
			powerSupply.AddUser();
		}
		if (unit is Aircraft aircraft)
		{
			if (aircraft.radar != null)
			{
				aircraft.radar.activated = false;
			}
			this.aircraft = aircraft;
			aircraft.SetRadar(this);
			activated = true;
		}
	}

	protected override void TargetDetector_OnUnitDisabled(Unit unit)
	{
		if (attachedUnit.NetworkHQ != null && shared)
		{
			attachedUnit.NetworkHQ.DeregisterRadar(this);
		}
		base.TargetDetector_OnUnitDisabled(unit);
	}

	public override float GetRadarRange()
	{
		return RadarParameters.maxRange;
	}

	public float GetMinSignal()
	{
		return RadarParameters.minSignal;
	}

	private void Radar_OnJam(Unit.JamEventArgs e)
	{
		base.enabled = true;
		jamAccumulation += e.jamAmount / Mathf.Max(jamTolerance, 0.1f);
		jamAccumulation = Mathf.Clamp01(jamAccumulation);
	}

	protected override void TargetSearch()
	{
		if (RadarParameters.maxRange > 0f)
		{
			RadarCheck();
		}
		if (visualRange > 0f)
		{
			VisualCheck();
		}
	}

	protected override void TargetDetector_OnApplyDamage(UnitPart.OnApplyDamage e)
	{
		if (e.detached || e.hitPoints <= 0f)
		{
			DisableTargetDetector();
		}
	}

	private void RadarCheck()
	{
		GlobalPosition b = scanner.GlobalPosition();
		foreach (FactionHQ allHQ in FactionRegistry.GetAllHQs())
		{
			if (attachedUnit.NetworkHQ == allHQ)
			{
				continue;
			}
			for (int i = 0; i < allHQ.factionRadarReturn.Count; i++)
			{
				if (UnitRegistry.TryGetUnit(allHQ.factionRadarReturn[i], out var unit))
				{
					IRadarReturn radarReturn = unit as IRadarReturn;
					if (FastMath.InRange(unit.GlobalPosition(), b, RadarParameters.maxRange * 2f) && (!(radarCone > 0f) || !(Vector3.Angle(unit.transform.position - scanner.position, scanner.transform.forward) > radarCone)))
					{
						DetectorManager.RequestRadarCheck(this, unit, radarReturn);
					}
				}
			}
		}
	}

	public bool CanSeeRadarReturn(IRadarReturn radarReturn, float dist, float clutterFactor)
	{
		if (radarReturn.GetRadarReturn(scanner.position, this, attachedUnit, dist, clutterFactor, RadarParameters, triggerWarning: true) >= RadarParameters.minSignal)
		{
			return !IsJammed();
		}
		return false;
	}

	private void FixedUpdate()
	{
		if (powerSupply != null && activated)
		{
			float num = powerSupply.DrawPower(powerDraw);
			powerRatio = ((powerDraw != 0f) ? (num / powerDraw) : 1f);
		}
	}

	private void Update()
	{
		for (int i = 0; i < rotators.Length; i++)
		{
			rotators[i].transform.localEulerAngles += rotators[i].axis * Time.deltaTime;
		}
		jamAccumulation -= Mathf.Max(jamAccumulation, 0.2f) * Time.deltaTime;
		if (jamAccumulation <= 0f)
		{
			jamAccumulation = 0f;
			if (rotators.Length == 0 && powerSupply == null)
			{
				base.enabled = false;
			}
		}
	}

	public void AddGuidedMissile(Missile missile)
	{
		if (!(missile == null) && !guidedMissiles.Contains(missile))
		{
			guidedMissiles.Add(missile);
		}
	}

	public void RemoveGuidedMissile(Missile missile)
	{
		if (!(missile == null) && guidedMissiles.Contains(missile))
		{
			guidedMissiles.Remove(missile);
		}
	}

	public bool CheckIsTarget(Unit unit)
	{
		if (guidedMissiles.Count == 0)
		{
			return false;
		}
		foreach (Missile guidedMissile in guidedMissiles)
		{
			if (guidedMissile != null && guidedMissile.targetID == unit.persistentID)
			{
				return true;
			}
		}
		return false;
	}
}
