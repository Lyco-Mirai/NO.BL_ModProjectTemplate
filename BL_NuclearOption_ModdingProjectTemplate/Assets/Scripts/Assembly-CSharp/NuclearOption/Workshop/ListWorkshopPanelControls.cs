using NuclearOption.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class ListWorkshopPanelControls : MonoBehaviour
	{
		public GameObject Holder;

		public TMP_InputField FilterNameInput;

		[Space]
		public BetterToggleGroup OrderByToggleGroup;

		public Button ClearFilterButton;

		public Button RefreshButton;
	}
}
