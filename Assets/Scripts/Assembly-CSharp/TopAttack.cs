using System;
using UnityEngine;

[Serializable]
public class TopAttack
{
	public float Amount;

	public float TooCloseRange = 2000f;

	public float probability = 1f;

	[NonSerialized]
	public bool Active;

	[SerializeField]
	private float minRange;

	[SerializeField]
	private float maxRange;

	[SerializeField]
	private bool armorSelective;

	public Vector3 ApplyTopAttack(GlobalPosition missilePos, GlobalPosition targetPos, float speed)
	{
		if (!FastMath.InRange(missilePos, targetPos, maxRange))
		{
			return Vector3.zero;
		}
		Active = true;
		Vector3 vector = targetPos - missilePos;
		vector.y = 0f;
		return Mathf.Clamp01(vector.magnitude * 0.5f / minRange - 0.2f) * Amount * Vector3.up;
	}

	public bool ShouldUseTopAttack(Unit target)
	{
		if (armorSelective)
		{
			if (target.definition.armorTier >= 5f)
			{
				return target.maxRadius < 10f;
			}
			return false;
		}
		return true;
	}
}
