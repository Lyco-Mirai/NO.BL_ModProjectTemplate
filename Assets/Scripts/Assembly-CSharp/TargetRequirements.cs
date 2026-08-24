using System;

[Serializable]
public struct TargetRequirements
{
	public bool lineOfSight;

	public float minAltitude;

	public float maxAltitude;

	public float minRange;

	public float maxRange;

	public float maxSpeed;

	public float minIR;

	public float minRadar;

	public float minAlignment;

	public float minOwnerSpeed;

	public float minValue;
}
