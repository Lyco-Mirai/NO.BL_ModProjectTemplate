using UnityEngine;

public class Parabola
{
	public static Vector3 Coefficients(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		float num = ((p1.y - p2.y) * (p1.x - p3.x) - (p1.y - p3.y) * (p1.x - p2.x)) / ((p1.x * p1.x - p2.x * p2.x) * (p1.x - p3.x) - (p1.x * p1.x - p3.x * p3.x) * (p1.x - p2.x));
		float num2 = (p1.y - p2.y - num * (p1.x * p1.x - p2.x * p2.x)) / (p1.x - p2.x);
		float z = p1.y - num * p1.x * p1.x - num2 * p1.x;
		return new Vector3(num, num2, z);
	}
}
