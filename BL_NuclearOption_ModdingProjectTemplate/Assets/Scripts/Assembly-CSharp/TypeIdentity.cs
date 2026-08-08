using System;
using UnityEngine;

[Serializable]
public struct TypeIdentity
{
	[Range(0f, 1f)]
	public float surface;

	[Range(0f, 1f)]
	public float air;

	[Range(0f, 1f)]
	public float missile;

	[Range(0f, 1f)]
	public float radar;

	[Range(0f, 1f)]
	public float strategic;

	public TypeIdentity(float surface, float air, float missile, float radar, float strategic)
	{
		this.surface = surface;
		this.air = air;
		this.missile = missile;
		this.radar = radar;
		this.strategic = strategic;
	}

	public float ThreatPosedBy(RoleIdentity threat)
	{
		return surface * threat.antiSurface + air * threat.antiAir + missile * threat.antiMissile + radar * threat.antiRadar;
	}
}
