using System;
using UnityEngine;

[Serializable]
public struct RoleIdentity
{
	[Range(0f, 1f)]
	public float antiSurface;

	[Range(0f, 1f)]
	public float antiAir;

	[Range(0f, 1f)]
	public float antiMissile;

	[Range(0f, 1f)]
	public float antiRadar;

	public float OpportunityAgainst(TypeIdentity target)
	{
		return target.surface * antiSurface + target.air * antiAir + target.missile * antiMissile + target.radar * antiRadar;
	}
}
