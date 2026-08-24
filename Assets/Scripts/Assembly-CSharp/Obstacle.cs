using UnityEngine;

public readonly struct Obstacle
{
	public readonly Transform Transform;

	public readonly float Radius;

	public readonly float Top;

	public Obstacle(Transform transform, float radius, float top)
	{
		Transform = transform;
		Radius = radius;
		Top = top;
	}
}
