using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class ContextMenuCategory
	{
		public string categoryName;

		public GameObject foldoutGo;

		public Button headerButton;

		public RectTransform container;

		public TextMeshProUGUI arrowText;

		public bool expanded;

		public List<GameObject> spawnedButtons = new List<GameObject>();
	}
}
