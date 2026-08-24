using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public abstract class SingleSelectionDetails : SelectionDetails
	{
		public readonly IEditorSelectable Source;

		protected SingleSelectionDetails(IEditorSelectable source, IValueWrapper<GlobalPosition> positionWrapper, IValueWrapper<Quaternion> rotationWrapper)
		{
			Source = source;
			base.PositionWrapper = positionWrapper;
			base.RotationWrapper = rotationWrapper;
		}
	}
}
