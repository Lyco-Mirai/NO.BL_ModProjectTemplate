using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit Definition", menuName = "ScriptableObjects/UnitDefinition", order = 6)]
public class UnitDefinition : ScriptableObject, INetworkDefinition, IHasJsonKey
{
	public TypeIdentity typeIdentity;

	public RoleIdentity roleIdentity;

	[Header("Important: dont change jsonKey after it is set")]
	[Tooltip("name used it look up to find unit, saved in mission json")]
	public string jsonKey;

	public string unitName;

	public string bogeyName;

	[TextArea(1, 10)]
	public string description;

	public string code;

	public float visibleRange;

	public float iconRange;

	public float radarSize;

	public Sprite friendlyIcon;

	public Sprite hostileIcon;

	public Sprite mapIcon;

	public bool mapOrient;

	public bool IsObstacle = true;

	public float iconSize;

	public float mapIconSize = 1f;

	public int captureCapacity;

	public float captureStrength;

	public float captureDefense;

	public float length;

	public float width;

	public float height;

	public float value;

	public float mass;

	public float manpower;

	public float armorTier;

	public float damageTolerance;

	public bool CanSlingLoad;

	public GameObject unitPrefab;

	public Vector3 spawnOffset;

	[Header("Enabled check")]
	[SerializeField]
	private bool disabled;

	[Tooltip("Should unit only be enabled for events like april fools")]
	[SerializeField]
	private bool isEventContent;

	public bool dontAutomaticallyAddToEncyclopedia;

	[Header("Editor limits")]
	[Tooltip("Min Height above the ground when moving the unit in editor (can be negative)")]
	public float minEditorHeight;

	[Tooltip("Max Height above the ground when moving the unit in editor")]
	public float maxEditorHeight = 1000f;

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

	public float GetOpportunity(RoleIdentity role)
	{
		return typeIdentity.ThreatPosedBy(role);
	}

	public void CacheMass()
	{
		mass = unitPrefab.GetComponent<Unit>().GetPrefabMass();
	}

	public float ThreatPosedBy(RoleIdentity role)
	{
		return typeIdentity.ThreatPosedBy(role);
	}
}
