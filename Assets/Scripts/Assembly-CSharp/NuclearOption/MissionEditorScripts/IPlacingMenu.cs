using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public interface IPlacingMenu
	{
		(bool placeMore, IEditorSelectable placedObject) Place(bool shift);

		void CancelPlace();

		void MoveCursor(Transform placementTransform);
	}
}
