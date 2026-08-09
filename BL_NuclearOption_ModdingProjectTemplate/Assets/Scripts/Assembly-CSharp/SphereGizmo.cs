using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SphereGizmo : MonoBehaviour
{
	private readonly Queue<GlobalPosition> positions = new Queue<GlobalPosition>();

	public int MaxPositions;

	public Color[] Colors;

	public float Radius;

	public int Count => positions.Count;

	public void Add(Vector3 position)
	{
		positions.Enqueue(position.ToGlobalPosition());
	}

	public static SphereGizmo Create(int max, Color color, float radius)
	{
		SphereGizmo sphereGizmo = new GameObject("SphereGizmo").AddComponent<SphereGizmo>();
		sphereGizmo.MaxPositions = max;
		sphereGizmo.Radius = radius;
		sphereGizmo.Colors = new Color[5];
		Color.RGBToHSV(color, out var H, out var _, out var _);
		sphereGizmo.Colors[0] = Color.HSVToRGB(H, 1f, 0.3f);
		sphereGizmo.Colors[1] = Color.HSVToRGB(H, 1f, 0.7f);
		sphereGizmo.Colors[2] = Color.HSVToRGB(H, 1f, 1f);
		sphereGizmo.Colors[3] = Color.HSVToRGB(H, 0.8f, 1f);
		sphereGizmo.Colors[4] = Color.HSVToRGB(H, 0.6f, 1f);
		return sphereGizmo;
	}

	private void OnDrawGizmos()
	{
		while (positions.Count > MaxPositions)
		{
			positions.Dequeue();
		}
		if (positions.Count <= 0)
		{
			return;
		}
		base.transform.position = positions.First().ToLocalPosition();
		int num = 0;
		Vector3 start = Vector3.zero;
		bool flag = false;
		foreach (GlobalPosition position in positions)
		{
			Vector3 vector = position.ToLocalPosition();
			Color color = Colors[num];
			num = (num + 1) % Colors.Length;
			Gizmos.color = color;
			Gizmos.DrawSphere(vector, Radius);
			if (flag)
			{
				Debug.DrawLine(start, vector, color, 0.1f);
			}
			start = vector;
			flag = true;
		}
	}
}
