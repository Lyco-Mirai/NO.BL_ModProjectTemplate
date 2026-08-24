using System;
using UnityEngine;

[Serializable]
public class AeroPID
{
	private PIDFactors factors;

	private float referenceAirspeed;

	private float p;

	private float i;

	private float d;

	private float errorPrev;

	public AeroPID(PIDFactors factors, float referenceAirspeed)
	{
		this.factors = factors;
		this.referenceAirspeed = referenceAirspeed;
	}

	public void Reseti()
	{
		i = 0f;
	}

	public float GetOutput(float currentError, float measuredD, float clampI, float airspeed, float deltaTime)
	{
		float num = referenceAirspeed * referenceAirspeed / Mathf.Max(airspeed * airspeed, 1000f);
		p = currentError;
		i += p * deltaTime;
		i = Mathf.Clamp(i, 0f - clampI, clampI);
		return p * factors.P * num + i * factors.I * num + measuredD * factors.D * num;
	}

	public float GetOutput(float currentError, float clampI, float airspeed)
	{
		float num = Mathf.Clamp(referenceAirspeed * referenceAirspeed / Mathf.Max(airspeed * airspeed, 10f), 0f, 4f);
		p = currentError;
		i = ((Mathf.Abs(p) < clampI) ? (i + p * Time.fixedDeltaTime) : 0f);
		d = (currentError - errorPrev) / Time.fixedDeltaTime;
		errorPrev = p;
		return p * factors.P + i * factors.I + d * factors.D * num;
	}

	public float GetOutputClampP(float currentError, float clampP, float clampI, float airspeed)
	{
		float num = Mathf.Clamp(referenceAirspeed * referenceAirspeed / Mathf.Max(airspeed * airspeed, 10f), 0f, 4f);
		p = currentError;
		i = ((Mathf.Abs(p) < clampI) ? (i + p * Time.fixedDeltaTime) : 0f);
		d = (currentError - errorPrev) / Time.fixedDeltaTime;
		errorPrev = p;
		return Mathf.Clamp(p, 0f - clampP, clampP) * factors.P + i * factors.I + d * factors.D * num;
	}
}
