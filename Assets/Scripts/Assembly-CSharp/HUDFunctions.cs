using UnityEngine;

public static class HUDFunctions
{
	public static bool PinToScreenEdge(Vector3 coords, out Vector3 rayToScreen, out float arrowAngle)
	{
		bool result = false;
		Vector3 to = coords - SceneSingleton<CameraStateManager>.i.mainCamera.transform.position;
		rayToScreen = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(coords);
		Vector3 vector = new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f);
		Vector3 vector2 = vector;
		rayToScreen -= vector;
		rayToScreen.z = 0f;
		arrowAngle = Mathf.Atan2(rayToScreen.y, rayToScreen.x);
		float num = Mathf.Tan(arrowAngle);
		if (Vector3.Angle(SceneSingleton<CameraStateManager>.i.transform.forward, to) > 90f || Mathf.Abs(rayToScreen.x) > (float)Screen.width * 0.5f || Mathf.Abs(rayToScreen.y) > (float)Screen.height * 0.5f)
		{
			result = true;
			if (rayToScreen.x > 0f)
			{
				rayToScreen = new Vector3(vector2.x, vector2.x * num, 0f);
			}
			else
			{
				rayToScreen = new Vector3(0f - vector2.x, (0f - vector2.x) * num, 0f);
			}
			if (rayToScreen.y > vector2.y)
			{
				rayToScreen = new Vector3(vector2.y / num, vector2.y, 0f);
			}
			else if (rayToScreen.y < 0f - vector2.y)
			{
				rayToScreen = new Vector3((0f - vector2.y) / num, 0f - vector2.y, 0f);
			}
		}
		rayToScreen += vector;
		return result;
	}
}
