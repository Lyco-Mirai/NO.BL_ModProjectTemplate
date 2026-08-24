using System;
using UnityEngine;

[Serializable]
public class PIDFactors
{
	[SerializeField]
	private Vector3 PID;

	public float P => PID.x;

	public float I => PID.y;

	public float D => PID.z;

	public PIDFactors(float p, float i, float d)
	{
		PID.x = p;
		PID.y = i;
		PID.z = d;
	}
}
