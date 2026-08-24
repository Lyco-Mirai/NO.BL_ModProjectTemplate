using System;
using UnityEngine;

[Serializable]
public class IRSource
{
	public Transform transform;

	public float intensity;

	public bool flare;

	public IRSource(Transform transform, float intensity, bool flare)
	{
		this.transform = transform;
		this.intensity = intensity;
		this.flare = flare;
	}
}
