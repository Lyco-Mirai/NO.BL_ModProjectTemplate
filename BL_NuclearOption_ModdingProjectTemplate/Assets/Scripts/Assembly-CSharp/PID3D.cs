using UnityEngine;

public class PID3D
{
	private Vector3 p;

	private Vector3 i;

	private Vector3 d;

	private Vector3 errorPrev;

	public Vector3 GetOutput(Vector3 currentError, float resetILimit, float deltaTime, Vector3 gain)
	{
		p = currentError;
		i = new Vector3((p.x < resetILimit) ? (i.x + p.x) : 0f, (p.y < resetILimit) ? (i.y + p.y) : 0f, (p.z < resetILimit) ? (i.z + p.z) : 0f);
		d = (currentError - errorPrev) / deltaTime;
		errorPrev = p;
		return p * gain.x + i * gain.y + d * gain.z;
	}

	public Vector3 GetOutputSqrt(Vector3 currentError, float resetILimit, float deltaTime, PIDFactors factors)
	{
		float magnitude = currentError.magnitude;
		p = Vector3.ClampMagnitude(currentError, Mathf.Sqrt(magnitude));
		i = ((magnitude > resetILimit) ? Vector3.zero : (i + p * deltaTime));
		d = (currentError - errorPrev) / deltaTime;
		errorPrev = currentError;
		return p * factors.P + i * factors.I + d * factors.D;
	}
}
