using UnityEngine;

public class PartDamageTracker
{
	private float detachedRatio;

	private float lastCheck;

	private bool needsCheck;

	private readonly Aircraft aircraft;

	private void PartDamageTracker_OnPartDetached(UnitPart part)
	{
		needsCheck = true;
	}

	public PartDamageTracker(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		needsCheck = false;
		int num = 0;
		foreach (UnitPart allPart in aircraft.GetAllParts())
		{
			num++;
			allPart.onPartDetached += PartDamageTracker_OnPartDetached;
		}
	}

	public float GetDetachedRatio()
	{
		if (Time.timeSinceLevelLoad - lastCheck < 1f || !needsCheck)
		{
			return detachedRatio;
		}
		lastCheck = Time.timeSinceLevelLoad;
		float num = 0f;
		float num2 = 0f;
		foreach (UnitPart allPart in aircraft.GetAllParts())
		{
			num += 1f;
			if (allPart.IsDetached())
			{
				num2 += 1f;
			}
		}
		needsCheck = false;
		detachedRatio = num2 / num;
		return detachedRatio;
	}
}
