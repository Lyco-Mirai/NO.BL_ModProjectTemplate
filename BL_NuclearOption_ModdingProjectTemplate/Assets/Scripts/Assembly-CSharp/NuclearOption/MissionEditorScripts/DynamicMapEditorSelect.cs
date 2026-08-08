using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class DynamicMapEditorSelect : MonoBehaviour
	{
		[SerializeField]
		private UnitSelection unitSelection;

		private void Awake()
		{
			unitSelection.OnSelect += UnitSelection_OnSelect;
		}

		private void OnDestroy()
		{
			unitSelection.OnSelect -= UnitSelection_OnSelect;
		}

		private void UnitSelection_OnSelect(SelectionDetails details)
		{
			if (SceneSingleton<DynamicMap>.i != null)
			{
				SceneSingleton<DynamicMap>.i.DeselectAllIcons();
				if (details != null)
				{
					SelectOne(details);
				}
			}
			static void SelectOne(SelectionDetails selectionDetails)
			{
				if (!(selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails))
				{
					if (!(selectionDetails is AirbaseSelectionDetails airbaseSelectionDetails))
					{
						if (selectionDetails is UnitSelectionDetails unitSelectionDetails)
						{
							SceneSingleton<DynamicMap>.i.SelectIcon(unitSelectionDetails.Unit);
						}
					}
					else
					{
						SceneSingleton<DynamicMap>.i.SelectIcon(airbaseSelectionDetails.Airbase);
					}
					return;
				}
				foreach (SingleSelectionDetails item in multiSelectSelectionDetails.Items)
				{
					SelectOne(item);
				}
			}
		}
	}
}
