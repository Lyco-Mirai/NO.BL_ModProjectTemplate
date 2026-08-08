using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Info", menuName = "ScriptableObjects/WeaponInfo", order = 3)]
public class WeaponInfo : ScriptableObject
{
	public GameObject weaponPrefab;

	public RoleIdentity effectiveness;

	public TargetRequirements targetRequirements;

	public float pK;

	public string weaponName;

	public string shortName;

	[TextArea(1, 10)]
	public string description;

	public float fireInterval;

	public float muzzleVelocity;

	public float maxSpeed = -1f;

	public float dragCoef;

	public float gravMult = 1f;

	public float pierceDamage;

	public float blastDamage;

	public Sprite weaponIcon;

	public float armorTierEffectiveness;

	public float airburstHeight;

	public float visibilityWhenFired;

	public float costPerRound;

	public float massPerRound;

	public bool useWeaponDoors = true;

	public bool boresight;

	public bool laserGuided;

	public bool missile;

	public bool bomb;

	public bool glideBomb;

	public bool gun;

	public bool overHorizon;

	public bool nuclear;

	public bool strategic;

	public bool energy;

	public bool jammer;

	public bool troops;

	public bool hideInDisplay;

	public bool cargo;

	public bool rearmGround;

	public bool rearmShip;

	public bool sling;

	public void SetMassPerRound(float massPerRound)
	{
		this.massPerRound = massPerRound;
	}

	public void SetCostPerRound(float costPerRound)
	{
		this.costPerRound = costPerRound;
	}

	public float GetMaxSpeed()
	{
		if (maxSpeed == -1f)
		{
			if (weaponPrefab != null)
			{
				Missile component = weaponPrefab.GetComponent<Missile>();
				if (component != null)
				{
					maxSpeed = component.GetTopSpeed(0f, 0f);
				}
			}
			else
			{
				maxSpeed = muzzleVelocity;
			}
		}
		return maxSpeed;
	}

	public float CalcAttacksNeeded(Unit target)
	{
		return Mathf.Max(target.definition.damageTolerance, 0.1f) / Mathf.Max(pK, 0.01f);
	}
}
