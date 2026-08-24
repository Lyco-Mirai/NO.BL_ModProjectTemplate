using System;
using UnityEngine;

[Serializable]
public class AoALimiter
{
	private Aircraft aircraft;

	[SerializeField]
	private float threshold = 7f;

	[SerializeField]
	private float limit = 20f;

	public void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public float GetExcessAlpha()
	{
		Vector3 vector = aircraft.cockpit.transform.InverseTransformDirection(aircraft.rb.velocity);
		return Mathf.Clamp01(((0f - Mathf.Atan2(vector.y, vector.z)) * 57.29578f - threshold) / (limit - threshold)) * limit;
	}
}
