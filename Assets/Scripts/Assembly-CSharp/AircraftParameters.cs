using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;

[CreateAssetMenu(fileName = "New Aircraft Parameters", menuName = "ScriptableObjects/AircraftParameters", order = 1)]
public class AircraftParameters : ScriptableObject
{
	[Serializable]
	public class OnboardAoAEffects
	{
		public AudioClip AudioClip;

		public float OnsetSpeed = 60f;

		public float FullVolumeSpeed = 200f;

		public float OnsetAlpha = 5f;

		public float FullVolumeAlpha = 45f;

		public float ShakeFactor = 0.1f;
	}

	[Serializable]
	public class Livery
	{
		public string name;

		public Faction faction;

		public AssetReferenceLiveryData assetReference;
	}

	public string aircraftName;

	public int rankRequired;

	public Airfoil[] airfoils;

	private Airfoil defaultAirfoil;

	public List<Loadout> loadouts;

	public StandardLoadout[] StandardLoadouts;

	public float DefaultFuelLevel = 1f;

	public List<Livery> liveries;

	public GameObject StatusDisplay;

	public GameObject HUDExtras;

	public AudioClip takeoffMusic;

	public OnboardAoAEffects AoAEffects;

	public float aircraftGLimit = 9f;

	public float PIDReferenceAirspeed;

	public float maxSpeed;

	public float takeoffSpeed;

	public float takeoffDistance;

	public bool verticalLanding;

	public float turningRadius;

	public float cornerSpeed;

	public float approachSpeed = 60f;

	public float landingSpeed = 30f;

	public float shortLandingSpeed = 30f;

	public float cruiseThrottle = 0.9f;

	public float minimumRadarAlt;

	public float levelBias;

	public float hoverTiltFactor = 1f;

	public Vector3 collectivePID;

	public Vector3 hoverPID;

	public Vector3 tiltPID;

	public float groundTurningRadius = 10f;

	public int GetAirfoilID(int index)
	{
		if (index < 0)
		{
			return -1;
		}
		return airfoils[index].id;
	}

	public void AddAirfoils(ref List<Airfoil> airfoilsList)
	{
		Airfoil[] array = airfoils;
		foreach (Airfoil item in array)
		{
			airfoilsList.Add(item);
		}
	}

	public StandardLoadout GetRandomStandardLoadout(AircraftDefinition definition, FactionHQ hq)
	{
		if (StandardLoadouts == null)
		{
			return null;
		}
		if (StandardLoadouts.Length == 0)
		{
			return null;
		}
		WeaponManager weaponManager = definition.unitPrefab.GetComponent<Aircraft>().weaponManager;
		List<StandardLoadout> list = new List<StandardLoadout>();
		StandardLoadout[] standardLoadouts = StandardLoadouts;
		foreach (StandardLoadout standardLoadout in standardLoadouts)
		{
			if (!standardLoadout.disabled && standardLoadout.AllowedByHQ(weaponManager, hq))
			{
				list.Add(standardLoadout);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		int index = UnityEngine.Random.Range(0, list.Count);
		return list[index];
	}

	public int GetRandomLiveryForFaction(Faction faction)
	{
		int num = 0;
		foreach (Livery livery in liveries)
		{
			if (livery.faction == faction)
			{
				num++;
			}
		}
		if (num == 0)
		{
			return 0;
		}
		int num2 = UnityEngine.Random.Range(0, num);
		for (int i = 0; i < liveries.Count; i++)
		{
			if (liveries[i].faction == faction)
			{
				if (num2 == 0)
				{
					return i;
				}
				num2--;
			}
		}
		Debug.LogError("Failed random index failed");
		return 0;
	}

	public int GetFirstLiveryForFaction(Faction faction)
	{
		for (int i = 0; i < liveries.Count; i++)
		{
			if (liveries[i].faction == faction)
			{
				return i;
			}
		}
		if (faction != null)
		{
			Debug.LogWarning("No skips found for faction " + faction.factionName);
		}
		return 0;
	}
}
