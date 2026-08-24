using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class EditorSelectableProxy : MonoBehaviour, IEditorSelectable
	{
		private IEditorSelectable flagOwner;

		public static void Add(IEditorSelectable flagOwner, GameObject addTo)
		{
			addTo.AddComponent<EditorSelectableProxy>().flagOwner = flagOwner;
		}

		public SingleSelectionDetails CreateSelectionDetails()
		{
			return flagOwner.CreateSelectionDetails();
		}
	}
}
