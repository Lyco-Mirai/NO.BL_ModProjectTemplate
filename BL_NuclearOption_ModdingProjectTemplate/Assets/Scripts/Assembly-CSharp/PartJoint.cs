using System;
using UnityEngine;

[Serializable]
public class PartJoint
{
	public UnitPart connectedPart;

	public UnitPart tensor;

	public int solverIterations = 6;

	public float breakForce;

	public float breakTorque;

	public Transform anchor;

	public AudioClip breakSound;

	[HideInInspector]
	public Joint joint;
}
