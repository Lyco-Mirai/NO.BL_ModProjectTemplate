using NuclearOption.SavedMission;
using RuntimeHandle;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public abstract class SelectionDetails
	{
		public abstract string DisplayName { get; }

		public abstract bool IsDestroyed { get; }

		public virtual bool AutoUnhover => true;

		public virtual HandleAxes AllowedPositionAxes => HandleAxes.XYZ;

		public virtual bool PositionHandleAllowed => PositionWrapper != null;

		public virtual bool RotationHandleAllowed => RotationWrapper != null;

		public IValueWrapper<GlobalPosition> PositionWrapper { get; protected set; }

		public IValueWrapper<Quaternion> RotationWrapper { get; protected set; }

		public virtual bool TryGetFaction(out Faction faction)
		{
			faction = null;
			return false;
		}

		public abstract void Focus();

		public abstract bool Delete();

		public override string ToString()
		{
			return GetType().Name;
		}
	}
}
