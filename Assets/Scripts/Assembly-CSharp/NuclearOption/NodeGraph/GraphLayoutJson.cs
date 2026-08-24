using System;
using System.Collections.Generic;
using System.IO;
using Mirage;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	[Serializable]
	public class GraphLayoutJson
	{
		public Vector2 panPosition;

		public float zoom = 1f;

		public List<GraphNodeLayoutJson> nodes = new List<GraphNodeLayoutJson>();

		[NonSerialized]
		public Action SyncLayout;

		public bool TryGetNodePosition(NodeId nodeId, out Vector2 position)
		{
			position = Vector2.zero;
			if (nodes == null)
			{
				return false;
			}
			GraphNodeLayoutJson graphNodeLayoutJson = nodes.Find((GraphNodeLayoutJson n) => n.nodeId == nodeId);
			if (graphNodeLayoutJson == null)
			{
				return false;
			}
			position = graphNodeLayoutJson.position;
			return true;
		}

		public static int GetJsonHash(GraphLayoutJson layout, out string json)
		{
			json = JsonUtility.ToJson(layout, prettyPrint: true);
			return json.GetStableHashCode();
		}

		public static void SaveToFile(GraphLayoutJson layout, string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("SaveLayout called with null or empty path");
				return;
			}
			string json = JsonUtility.ToJson(layout, prettyPrint: true);
			SaveToFile(path, json);
		}

		public static void SaveToFile(string path, string json)
		{
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("SaveLayout called with null or empty path");
				return;
			}
			try
			{
				string directoryName = Path.GetDirectoryName(path);
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				File.WriteAllText(path, json);
			}
			catch (Exception arg)
			{
				Debug.LogError($"Failed to save graph layout to '{path}': {arg}");
			}
		}

		public static bool TryLoadLayout(string path, out GraphLayoutJson metadata)
		{
			metadata = null;
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return false;
			}
			try
			{
				string json = File.ReadAllText(path);
				metadata = JsonUtility.FromJson<GraphLayoutJson>(json);
			}
			catch (Exception arg)
			{
				Debug.LogError($"Failed to parse graph layout: {arg}");
				return false;
			}
			return metadata != null;
		}
	}
}
