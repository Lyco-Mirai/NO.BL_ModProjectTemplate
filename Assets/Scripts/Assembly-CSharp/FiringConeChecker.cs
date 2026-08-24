using UnityEngine;

public static class FiringConeChecker
{
	public static bool VectorWithinFiringCones(FiringCone[] firingCones, Vector3 targetVector, out Vector3 nearestAllowableVector)
	{
		nearestAllowableVector = targetVector;
		if (firingCones.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < firingCones.Length; i++)
		{
			if (!firingCones[i].VectorPermitted(nearestAllowableVector, out var allowedVector))
			{
				nearestAllowableVector = allowedVector;
			}
		}
		return targetVector == nearestAllowableVector;
	}
}
