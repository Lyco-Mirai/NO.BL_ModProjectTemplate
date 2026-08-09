using System;
using UnityEngine;

[Serializable]
public struct RadarParams
{
	public float maxRange;

	public float maxSignal;

	public float minSignal;

	public float clutterFactor;

	public float dopplerFactor;

	public RadarParams(float maxRange, float maxSignal, float minSignal, float clutterFactor, float dopplerFactor)
	{
		this.maxRange = maxRange;
		this.maxSignal = maxSignal;
		this.minSignal = minSignal;
		this.clutterFactor = clutterFactor;
		this.dopplerFactor = dopplerFactor;
	}

	public float GetSignalStrength(Vector3 direction, float dist, Rigidbody rb, float RCS, float clutter, float ecm)
	{
		float a = maxRange / dist * Mathf.Pow(RCS, 0.25f);
		a = Mathf.Min(a, maxSignal);
		float num = clutter * clutterFactor;
		a -= num;
		if (a > minSignal)
		{
			float num2 = Mathf.Lerp(a, maxSignal, 0.5f);
			if (rb != null)
			{
				float num3 = Mathf.Min(Mathf.Abs(Vector3.Dot(direction, rb.velocity)), 150f) * dopplerFactor;
				num2 *= 1f + num3;
			}
			return num2 - ecm;
		}
		return a;
	}
}
