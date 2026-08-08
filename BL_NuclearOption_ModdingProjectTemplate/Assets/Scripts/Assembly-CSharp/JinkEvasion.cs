using System;
using UnityEngine;

[Serializable]
public class JinkEvasion
{
	public float amount;

	[SerializeField]
	private float period;

	[SerializeField]
	private float minRange;

	[SerializeField]
	private float maxRange;

	[SerializeField]
	private float minSpeed = 300f;

	[SerializeField]
	private bool flat;

	private Vector3 jinkOffset;

	private float lastJink;

	public Vector3 ApplyJink(GlobalPosition missilePos, GlobalPosition targetPos, float speed, float targetDist)
	{
		if (targetDist < minRange || targetDist > maxRange || speed < minSpeed)
		{
			return Vector3.zero;
		}
		if (jinkOffset == Vector3.zero || Time.timeSinceLevelLoad - lastJink > period)
		{
			lastJink = Time.timeSinceLevelLoad;
			jinkOffset = Vector3.zero;
			Vector3 planeNormal = targetPos - missilePos;
			jinkOffset = Vector3.ProjectOnPlane(UnityEngine.Random.insideUnitSphere, planeNormal).normalized * amount;
			jinkOffset.y = (flat ? 0f : Mathf.Max(jinkOffset.y, 0f));
		}
		return jinkOffset * targetDist;
	}
}
