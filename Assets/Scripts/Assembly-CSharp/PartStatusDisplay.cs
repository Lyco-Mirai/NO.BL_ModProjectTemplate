using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PartStatusDisplay
{
	public Image partImage;

	private UnitPart unitPart;

	public float redStatusThreshold;

	[NonSerialized]
	public float displayCondition = 1f;

	private StatusDisplay statusDisplay;

	public void DamageSubscribe(Aircraft aircraft, StatusDisplay statusDisplay)
	{
		unitPart = FindPart(aircraft.partLookup);
		if (unitPart != null)
		{
			unitPart.onApplyDamage += StatusDisplay_OnDamage;
			unitPart.onParentDetached += StatusDisplay_OnDetach;
		}
		this.statusDisplay = statusDisplay;
	}

	private UnitPart FindPart(List<UnitPart> partLookup)
	{
		foreach (UnitPart item in partLookup)
		{
			if (item != null && item.gameObject.name == partImage.gameObject.name)
			{
				return item;
			}
		}
		return null;
	}

	public void DamageUnsubscribe()
	{
		if (unitPart != null)
		{
			unitPart.onApplyDamage -= StatusDisplay_OnDamage;
			unitPart.onParentDetached -= StatusDisplay_OnDetach;
		}
	}

	private void StatusDisplay_OnDamage(UnitPart.OnApplyDamage e)
	{
		displayCondition = Mathf.Max((e.hitPoints - redStatusThreshold) / (100f - redStatusThreshold), 0f);
		if (e.detached)
		{
			displayCondition = 0f;
		}
		Color color = partImage.color;
		color.g = Mathf.Min(displayCondition * 2f, 1f);
		color.a = 1f - displayCondition;
		partImage.color = color;
		statusDisplay.DisplayDamage();
	}

	private void StatusDisplay_OnDetach(UnitPart part)
	{
		DamageUnsubscribe();
		partImage.color = new Color(0.7f, 0f, 0.25f);
		statusDisplay.DisplayDamage();
	}
}
