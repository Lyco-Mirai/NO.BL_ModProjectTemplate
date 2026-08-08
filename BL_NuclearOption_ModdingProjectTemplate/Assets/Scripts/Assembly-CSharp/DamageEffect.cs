using System;
using UnityEngine;

[Serializable]
public class DamageEffect
{
	public GameObject prefab;

	public float threshold;

	[NonSerialized]
	public GameObject instance;

	[NonSerialized]
	public bool triggered;
}
