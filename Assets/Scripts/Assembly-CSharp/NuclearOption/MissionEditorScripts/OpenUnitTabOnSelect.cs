using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class OpenUnitTabOnSelect : MonoBehaviour
	{
		[SerializeField]
		private UnitSelection unitSelection;

		[SerializeField]
		private EditorTabs editorTabs;

		[Header("Prefabs")]
		[SerializeField]
		private GameObject unitPanelPrefab;

		[SerializeField]
		private GameObject airbasePrefab;

		private void Awake()
		{
			unitSelection.OnSelect += UnitSelection_onSelect;
		}

		private void UnitSelection_onSelect(SelectionDetails selectionDetails)
		{
			if (!(selectionDetails is UnitSelectionDetails))
			{
				if (!(selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails))
				{
					if (selectionDetails is AirbaseSelectionDetails)
					{
						OnSelectAirbase();
					}
					return;
				}
				if (!(multiSelectSelectionDetails.SelectionType == typeof(UnitSelectionDetails)))
				{
					return;
				}
			}
			OnSelectUnit();
		}

		private void OnSelectUnit()
		{
			if (editorTabs.TryGetOpenTab<UnitPanel>(out var foundPanel))
			{
				foundPanel.SelectedRefreshed();
				return;
			}
			editorTabs.HideTab(clearUnit: false);
			editorTabs.ToggleTabPrefab(null, unitPanelPrefab, clearUnit: false);
		}

		private void OnSelectAirbase()
		{
			editorTabs.HideTab(clearUnit: false);
			editorTabs.ToggleTabPrefab(null, airbasePrefab, clearUnit: false);
		}
	}
}
