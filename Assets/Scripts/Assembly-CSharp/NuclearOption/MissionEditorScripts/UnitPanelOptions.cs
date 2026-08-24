using NuclearOption.MissionEditorScripts.MultiSelect;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public abstract class UnitPanelOptions : MonoBehaviour
	{
		protected UnitPanel unitMenu;

		protected MultiSelect<SavedUnit> targets;

		public void Setup(UnitPanel unitMenu, MultiSelect<SavedUnit> targets)
		{
			this.unitMenu = unitMenu;
			this.targets = targets;
			SetupInner();
		}

		protected void OnDestroy()
		{
			if (targets != null)
			{
				Cleanup();
				targets = null;
			}
		}

		protected abstract void SetupInner();

		public abstract void OnTargetsChanged();

		public abstract void Cleanup();
	}
}
