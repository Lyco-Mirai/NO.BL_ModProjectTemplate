using System;
using UnityEngine;

[Serializable]
public class FiringCone
{
	[SerializeField]
	private Transform transform;

	[SerializeField]
	private float coneAngle;

	[SerializeField]
	private bool exclusion;

	public Vector3 GetDirection()
	{
		return transform.forward;
	}

	public float GetAngle()
	{
		return coneAngle;
	}

	public bool VectorPermitted(Vector3 checkVector, out Vector3 allowedVector)
	{
		allowedVector = checkVector;
		float num = Vector3.Angle(checkVector, transform.forward);
		if (!exclusion && num > coneAngle)
		{
			allowedVector = Vector3.RotateTowards(transform.forward, checkVector, coneAngle * (MathF.PI / 180f), 1f);
		}
		if (exclusion && num < coneAngle)
		{
			float num2 = coneAngle - num;
			allowedVector = Vector3.RotateTowards(checkVector, transform.forward, (0f - num2) * (MathF.PI / 180f), 1f);
		}
		return checkVector == allowedVector;
	}
}
