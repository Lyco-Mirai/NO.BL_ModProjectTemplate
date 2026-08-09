using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Mount", menuName = "ScriptableObjects/WeaponMount", order = 4)]
public class WeaponMount : ScriptableObject, INetworkDefinition, IHasJsonKey
{
	public GameObject prefab;

	[Tooltip("name used it look up to find unit, saved in mission json")]
	public string jsonKey;

	public WeaponInfo info;

	public string mountName;

	public int ammo;

	public bool turret;

	public bool missileBay;

	public bool radar;

	public bool tailHook;

	public bool slingloadHook;

	public bool countermeasure;

	public bool colorable;

	public bool Cargo;

	public bool Troops;

	public bool sortWeapons = true;

	public bool GearSafety = true;

	public bool GroundSafety = true;

	public bool GunAmmo;

	public float emptyCost;

	public float emptyMass;

	[HideInInspector]
	public float mass;

	public float drag;

	public float emptyDrag;

	public float RCS;

	public float emptyRCS;

	[Header("Enabled check")]
	[SerializeField]
	private bool disabled;

	[Tooltip("Should unit only be enabled for events like april fools")]
	[SerializeField]
	private bool isEventContent;

	public bool dontAutomaticallyAddToEncyclopedia;

	[field: NonSerialized]
	int? INetworkDefinition.LookupIndex { get; set; }

	string IHasJsonKey.JsonKey
	{
		get
		{
			return jsonKey;
		}
		set
		{
			if (!Application.isEditor)
			{
				throw new Exception("JsonKey should only be set in UnityEditor");
			}
			jsonKey = value;
		}
	}

	public bool NotAllowed(bool includeEventContent)
	{
		return !IsAllowed(includeEventContent);
	}

	public bool IsAllowed(bool includeEventContent)
	{
		if (disabled)
		{
			return false;
		}
		if (isEventContent)
		{
			return includeEventContent;
		}
		return true;
	}

	public void Initialize()
	{
		if (Cargo)
		{
			Unit[] componentsInChildren = prefab.GetComponentsInChildren<Unit>();
			foreach (Unit unit in componentsInChildren)
			{
				mass += unit.GetMass();
			}
		}
		if (info != null)
		{
			if (info.weaponPrefab != null)
			{
				Missile component = info.weaponPrefab.GetComponent<Missile>();
				info.SetMassPerRound(component.definition.mass);
				info.SetCostPerRound(component.definition.value);
				mass = emptyMass + info.massPerRound * (float)ammo;
				Weapon[] componentsInChildren2 = prefab.GetComponentsInChildren<Weapon>();
				if (componentsInChildren2.Length != 0)
				{
					ammo = componentsInChildren2.Length;
					mountName = ((ammo > 1) ? $"{info.weaponName} x{ammo}" : (info.weaponName ?? ""));
					if (componentsInChildren2[0].info != null)
					{
						info = componentsInChildren2[0].info;
					}
				}
				return;
			}
			Gun componentInChildren = prefab.GetComponentInChildren<Gun>();
			if (componentInChildren != null)
			{
				ammo = componentInChildren.GetFullAmmo();
				mountName = ((ammo > 1) ? $"{info.weaponName} ({ammo} rounds)" : (info.weaponName ?? ""));
				info = componentInChildren.info;
			}
			if (!Cargo)
			{
				return;
			}
			Weapon[] componentsInChildren3 = prefab.GetComponentsInChildren<Weapon>();
			if (componentsInChildren3.Length != 0)
			{
				ammo = componentsInChildren3.Length;
				if (componentsInChildren3[0].info != null)
				{
					info = componentsInChildren3[0].info;
				}
			}
		}
		else
		{
			TailHook componentInChildren2 = prefab.GetComponentInChildren<TailHook>();
			if (componentInChildren2 != null)
			{
				mass = componentInChildren2.GetMass();
			}
		}
	}

	public float GetDragPerRound()
	{
		return (drag - emptyDrag) / (float)ammo;
	}

	public float GetRCSPerRound()
	{
		return (RCS - emptyRCS) / (float)ammo;
	}
}
