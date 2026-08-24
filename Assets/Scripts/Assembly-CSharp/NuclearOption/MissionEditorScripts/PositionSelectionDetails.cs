using System;
using NuclearOption.SavedMission.Objectives;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class PositionSelectionDetails : SingleSelectionDetails
	{
		public PositionHandle Handle;

		private readonly Action destroyCallback;

		public override string DisplayName => Handle.GetDisplayName();

		public override bool IsDestroyed => Handle == null;

		public PositionSelectionDetails(PositionHandle handle, Action destroyCallback)
			: base(handle, handle.PositionWrapper, null)
		{
			Handle = handle;
			this.destroyCallback = destroyCallback;
		}

		public override void Focus()
		{
			Vector3 position = base.PositionWrapper.Value.ToLocalPosition();
			SceneSingleton<CameraStateManager>.i.FocusPosition(position, null, 50f);
		}

		public override bool Delete()
		{
			if (destroyCallback != null)
			{
				destroyCallback();
				return true;
			}
			return false;
		}
	}
}
