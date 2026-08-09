using System.Collections.Generic;
using UnityEngine;

public abstract class Countermeasure : MonoBehaviour
{
	public string displayName;

	public Sprite displayImage;

	public bool chargeable;

	public int ammo;

	protected List<string> threatTypes;

	public Aircraft aircraft;

	protected virtual void Awake()
	{
		if (aircraft != null)
		{
			aircraft.countermeasureManager.RegisterCountermeasure(this);
		}
	}

	public virtual List<string> GetThreatTypes()
	{
		return new List<string>();
	}

	public virtual void AttachToUnit(Aircraft aircraft)
	{
		if (!(this.aircraft != null))
		{
			this.aircraft = aircraft;
			aircraft.countermeasureManager.RegisterCountermeasure(this);
		}
	}

	public virtual void Fire()
	{
	}

	public virtual void UpdateHUD()
	{
	}

	protected virtual void OnDestroy()
	{
		if (aircraft != null)
		{
			aircraft.countermeasureManager.DeregisterCountermeasure(this);
		}
	}

	public virtual void Rearm(Aircraft aircraft, Unit rearmer)
	{
	}
}
