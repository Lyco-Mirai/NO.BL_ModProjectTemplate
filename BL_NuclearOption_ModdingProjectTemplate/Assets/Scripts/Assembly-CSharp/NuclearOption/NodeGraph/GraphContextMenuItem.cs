using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphContextMenuItem : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private TextMeshProUGUI labelText;

		public Button Button => button;

		public TextMeshProUGUI LabelText => labelText;

		private void OnValidate()
		{
		}
	}
}
