using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.DebugScripts
{
	public static class DebugVis
	{
		public class DebugVisTracker : MonoBehaviour
		{
			private void Update()
			{
				if (!Enabled)
				{
					CleanupMarkers();
				}
			}
		}

		private static DebugVisTracker tracker;

		private static readonly List<GameObject> Markers = new List<GameObject>();

		public static bool Enabled => PlayerSettings.debugVis;

		public static bool Create<T>(ref T field, T prefab, Transform parent = null) where T : Object
		{
			if (field != null)
			{
				return false;
			}
			field = Object.Instantiate(prefab, parent);
			if (tracker == null)
			{
				tracker = new GameObject("DebugVisTracker").AddComponent<DebugVisTracker>();
			}
			if (field is GameObject item)
			{
				Markers.Add(item);
			}
			else if (field is Component component)
			{
				Markers.Add(component.gameObject);
			}
			return true;
		}

		public static void AddMarker(GameObject go)
		{
			Markers.Add(go);
		}

		public static void CleanupMarkers()
		{
			Object.Destroy(tracker.gameObject);
			foreach (GameObject marker in Markers)
			{
				Object.Destroy(marker);
			}
		}
	}
}
