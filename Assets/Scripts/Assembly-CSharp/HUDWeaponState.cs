using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDWeaponState : MonoBehaviour
{
	protected WeaponInfo weaponInfo;

	protected List<Unit> targetList;

	public virtual void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
	}

	public virtual void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		weaponInfo = weaponStation.WeaponInfo;
	}

	public virtual void HUDFixedUpdate(Aircraft aircraft, List<Unit> targetList)
	{
	}
}
