using System;
using UnityEngine;

[Serializable]
public class GForceDamage
{
	public bool enabled;

	public float threshold;

	public float overGDamage;

	[NonSerialized]
	public bool initialized;

	[NonSerialized]
	public Vector3 velocityPrev;
}
