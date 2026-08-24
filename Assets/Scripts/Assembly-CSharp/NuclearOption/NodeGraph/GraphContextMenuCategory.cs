using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphContextMenuCategory : MonoBehaviour
	{
		[SerializeField]
		private Button headerButton;

		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private TextMeshProUGUI arrowText;

		[SerializeField]
		private RectTransform itemsContainer;

		public Button HeaderButton => headerButton;

		public TextMeshProUGUI TitleText => titleText;

		public TextMeshProUGUI ArrowText => arrowText;

		public RectTransform ItemsContainer => itemsContainer;

		private void OnValidate()
		{
		}
	}
}
