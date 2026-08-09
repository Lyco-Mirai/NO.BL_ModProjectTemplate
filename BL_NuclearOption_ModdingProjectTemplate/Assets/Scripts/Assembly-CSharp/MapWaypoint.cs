using UnityEngine;

public class MapWaypoint
{
	public Vector3 waypointPosition;

	public Vector3 previousWaypoint;

	public GameObject marker;

	public GameObject vector;

	public MapWaypoint(Vector3 position, Vector3 previousWaypoint, GameObject marker, GameObject vector)
	{
		waypointPosition = position;
		this.previousWaypoint = previousWaypoint;
		this.marker = marker;
		this.vector = vector;
		PlaceMarker();
	}

	public void PlaceMarker()
	{
		float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
		marker.transform.position = waypointPosition;
		marker.transform.localScale = 1f * Vector3.one * num;
		vector.transform.position = waypointPosition;
		float z = (0f - Mathf.Atan2(marker.transform.localPosition.x - previousWaypoint.x, marker.transform.localPosition.y - previousWaypoint.y)) * 57.29578f + 180f;
		vector.transform.eulerAngles = new Vector3(0f, 0f, z);
		vector.transform.localScale = new Vector3(4f * num, (marker.transform.localPosition - previousWaypoint).magnitude, 4f * num);
	}

	public void UpdateMarker()
	{
		if (!(marker == null) && !(vector == null))
		{
			float num = 1f / SceneSingleton<DynamicMap>.i.mapImage.transform.localScale.x;
			marker.transform.localScale = 1f * Vector3.one * num;
			vector.transform.localScale = new Vector3(4f * num, (marker.transform.localPosition - previousWaypoint).magnitude, 4f * num);
		}
	}
}
