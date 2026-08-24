using System.Collections.Generic;
using RoadPathfinding;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class RoadNodeInspector : MonoBehaviour
	{
		[SerializeField]
		private Text nodeName;

		[SerializeField]
		private Text NeighborsList;

		[SerializeField]
		private GameObject neighborHighlightEffect;

		public void DisplayNodeInfo(Node node)
		{
			nodeName.text = "Path Node " + node.id;
			NeighborsList.text = "Neighbors: ";
			foreach (KeyValuePair<Road, Node> item in node.connectionsLookup)
			{
				Text neighborsList = NeighborsList;
				neighborsList.text = neighborsList.text + " \n Path Node " + item.Value.id;
				GameObject obj = Object.Instantiate(neighborHighlightEffect, Datum.origin);
				obj.transform.localPosition = item.Value.position.AsVector3();
				Object.Destroy(obj, 10f);
			}
		}
	}
}
