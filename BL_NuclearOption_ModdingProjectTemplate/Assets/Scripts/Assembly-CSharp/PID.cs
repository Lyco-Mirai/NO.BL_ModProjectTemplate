using System;
using UnityEngine;

[Serializable]
public class PID
{
	public float pFactor;

	public float iFactor;

	public float dFactor;

	private float p;

	private float i;

	private float d;

	private float previousError;

	public PID(float p, float i, float d)
	{
		pFactor = p;
		iFactor = i;
		dFactor = d;
	}

	public PID(Vector3 pid)
	{
		pFactor = pid.x;
		iFactor = pid.y;
		dFactor = pid.z;
	}

	public PID(PIDFactors factors)
	{
		pFactor = factors.P;
		iFactor = factors.I;
		dFactor = factors.D;
	}

	public void SetValues(float p, float i, float d)
	{
		pFactor = p;
		iFactor = i;
		dFactor = d;
	}

	public void Reseti()
	{
		i = 0f;
	}

	public float GetOutput(float currentError, float deltaTime)
	{
		p = currentError;
		i += p * deltaTime;
		d = (currentError - previousError) / deltaTime;
		previousError = p;
		return p * pFactor + i * iFactor + d * dFactor;
	}

	public float GetOutput(float currentError, float iThreshold, float deltaTime)
	{
		p = currentError;
		i = ((Mathf.Abs(p) < iThreshold) ? (i + p * deltaTime) : 0f);
		d = (currentError - previousError) / deltaTime;
		previousError = p;
		return p * pFactor + i * iFactor + d * dFactor;
	}

	public float GetOutput(float currentError, float iThreshold, float deltaTime, Vector3 gain)
	{
		p = currentError;
		i = ((Mathf.Abs(p) < iThreshold) ? (i + p * deltaTime) : 0f);
		d = (currentError - previousError) / deltaTime;
		previousError = p;
		return p * gain.x + i * gain.y + d * gain.z;
	}

	public float GetOutputSqrt(float currentError, float iThreshold, float deltaTime, Vector3 gain)
	{
		p = ((Mathf.Abs(currentError) > 1f) ? (Mathf.Sign(currentError) * Mathf.Sqrt(Mathf.Abs(currentError))) : currentError);
		i = ((Mathf.Abs(p) < iThreshold) ? (i + p * deltaTime) : 0f);
		d = (currentError - previousError) / deltaTime;
		previousError = p;
		return p * gain.x + i * gain.y + d * gain.z;
	}

	public float GetOutput(float currentError, float measuredD, float iThreshold, float deltaTime)
	{
		p = currentError;
		i = ((Mathf.Abs(p) < iThreshold) ? (i + p * deltaTime) : 0f);
		return p * pFactor + i * iFactor + measuredD * dFactor;
	}

	public float GetOutput(float currentError, float measuredD, float iThreshold, float iClamp, float deltaTime)
	{
		p = currentError;
		i = ((Mathf.Abs(p) < iThreshold) ? (i + p * deltaTime) : i);
		i = Mathf.Clamp(i, 0f - iClamp, iClamp);
		return p * pFactor + i * iFactor + measuredD * dFactor;
	}

	public float GetOutputDirect(float currentError, float measuredD, float iThreshold, float deltaTime, Vector3 gain)
	{
		p = currentError;
		i = ((Mathf.Abs(p) < iThreshold) ? (i + p * deltaTime) : i);
		return p * gain.x + i * gain.y + measuredD * gain.z;
	}
}
