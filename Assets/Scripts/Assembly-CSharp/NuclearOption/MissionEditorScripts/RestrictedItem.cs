using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class RestrictedItem : MonoBehaviour
	{
		[SerializeField]
		private Text itemName;

		private RestrictionsTab restrictionsTab;

		private WeaponMount weaponMount;

		private UnitDefinition unitDefinition;

		public void SetItem(WeaponMount weaponMount, RestrictionsTab restrictionsTab)
		{
			this.weaponMount = weaponMount;
			this.restrictionsTab = restrictionsTab;
			itemName.text = weaponMount.mountName;
		}

		public void SetItem(UnitDefinition unitDefinition, RestrictionsTab restrictionsTab)
		{
			this.unitDefinition = unitDefinition;
			this.restrictionsTab = restrictionsTab;
			itemName.text = unitDefinition.unitName;
		}

		public void RemoveItem()
		{
			if (weaponMount != null)
			{
				restrictionsTab.UnrestrictWeapon(weaponMount, this);
			}
			if (unitDefinition != null)
			{
				restrictionsTab.UnrestrictAircraft(unitDefinition, this);
			}
		}
	}
}
