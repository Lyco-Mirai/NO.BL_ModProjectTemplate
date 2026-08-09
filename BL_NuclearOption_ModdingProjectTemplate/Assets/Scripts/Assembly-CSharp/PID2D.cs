using UnityEngine;

public class PID2D
{
	private float pFactor;

	private float iFactor;

	private float dFactor;

	private float pLimit;

	private float iLimit;

	private Vector2 p;

	private Vector2 i;

	private Vector2 d;

	private Vector2 errorPrev;

	public PID2D(PIDFactors factors, float pLimit, float iLimit)
	{
		pFactor = factors.P;
		iFactor = factors.I;
		dFactor = factors.D;
		this.pLimit = pLimit;
		this.iLimit = iLimit;
	}

	public void SetPLimit(float pLimit)
	{
		this.pLimit = pLimit;
	}

	public Vector2 GetOutput(Vector2 currentError, float deltaTime)
	{
		p = currentError;
		i = new Vector2((Mathf.Abs(p.x) < iLimit) ? (i.x + p.x) : 0f, (Mathf.Abs(p.y) < iLimit) ? (i.y + p.y) : 0f);
		d = (currentError - errorPrev) / deltaTime;
		errorPrev = p;
		p.x = Mathf.Clamp(p.x, 0f - pLimit, pLimit);
		p.y = Mathf.Clamp(p.y, 0f - pLimit, pLimit);
		return p * pFactor + i * iFactor + d * dFactor;
	}

	public Vector2 GetOutput(Vector2 currentError, Vector2 measuredD)
	{
		p = currentError;
		i = new Vector2((Mathf.Abs(p.x) < iLimit) ? (i.x + p.x) : 0f, (Mathf.Abs(p.y) < iLimit) ? (i.y + p.y) : 0f);
		d = measuredD;
		p.x = Mathf.Clamp(p.x, 0f - pLimit, pLimit);
		p.y = Mathf.Clamp(p.y, 0f - pLimit, pLimit);
		return p * pFactor + i * iFactor + d * dFactor;
	}
}
