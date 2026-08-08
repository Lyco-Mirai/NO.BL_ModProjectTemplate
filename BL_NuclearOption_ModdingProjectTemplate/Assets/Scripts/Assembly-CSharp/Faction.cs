using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Faction", menuName = "ScriptableObjects/Faction", order = 7)]
public class Faction : ScriptableObject, INetworkDefinition
{
	[Serializable]
	public class ConvoyUnit
	{
		public UnitDefinition Type;

		public int Count = 1;
	}

	[Serializable]
	public class ConvoyGroup
	{
		public string Name;

		public List<ConvoyUnit> Constituents = new List<ConvoyUnit>();

		public float GetCost()
		{
			float num = 0f;
			foreach (ConvoyUnit constituent in Constituents)
			{
				num += constituent.Type.value * (float)constituent.Count;
			}
			return num;
		}
	}

	public int LeaderboardOrder;

	public string factionName;

	public string factionTag;

	public string factionExtendedName;

	public Sprite factionHeaderSprite;

	public Sprite factionGrayscaleLogo;

	public Sprite factionColorLogo;

	public Color color;

	[Tooltip("Color used when map icon is selected")]
	public Color selectedColor;

	public AudioClip theme;

	public AudioClip idleTheme;

	[SerializeField]
	private List<ConvoyGroup> convoyGroups = new List<ConvoyGroup>();

	[field: NonSerialized]
	int? INetworkDefinition.LookupIndex { get; set; }

	public bool TryGetConvoyGroup(int index, out ConvoyGroup convoyGroup)
	{
		if (0 <= index && index < convoyGroups.Count)
		{
			convoyGroup = convoyGroups[index];
			return true;
		}
		convoyGroup = null;
		return false;
	}

	public bool TryGetConvoyGroup(string name, out ConvoyGroup outConvoyGroup)
	{
		foreach (ConvoyGroup convoyGroup in convoyGroups)
		{
			if (convoyGroup.Name == name)
			{
				outConvoyGroup = convoyGroup;
				return true;
			}
		}
		outConvoyGroup = null;
		return false;
	}

	public List<ConvoyGroup> GetConvoyGroups()
	{
		return convoyGroups;
	}
}
