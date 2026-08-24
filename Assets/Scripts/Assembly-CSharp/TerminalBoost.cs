using System;
using UnityEngine;

[Serializable]
public class TerminalBoost
{
	public float Amount;

	[NonSerialized]
	public bool Active;

	[SerializeField]
	private float minRange;

	[SerializeField]
	private float maxRange;

	public void ApplyTerminalBoost(Missile missile, GlobalPosition missilePos, GlobalPosition targetPos)
	{
		if (FastMath.InRange(missilePos, targetPos, maxRange) && !Active)
		{
			Active = true;
			missile.ApplyTerminalBoost(Amount);
		}
	}
}
