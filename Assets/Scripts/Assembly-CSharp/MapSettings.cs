using System;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using RoadPathfinding;
using UnityEngine;

public class MapSettings : MonoBehaviour
{
	[Serializable]
	private class MapMusic
	{
		public Faction Faction;

		[SerializeField]
		private AudioClip startMusic;

		[SerializeField]
		private AudioClip tacticalMusic;

		[SerializeField]
		private AudioClip strategicMusic;

		public AudioClip GetTacticalMusic()
		{
			return tacticalMusic;
		}

		public AudioClip GetStrategicMusic()
		{
			return strategicMusic;
		}

		public AudioClip GetStartMusic()
		{
			return startMusic;
		}
	}

	[Header("Prefab setup")]
	public NetworkMap NetworkMap;

	[Header("Map Settings")]
	public Vector2 MapSize;

	public int GridSizeX;

	public int GridSizeY;

	public int OffsetX;

	public int OffsetY;

	public float Latitude;

	[ScriptableObjectInlineCreate]
	public RoadNetworkSO RoadNetwork;

	[ScriptableObjectInlineCreate]
	public RoadNetworkSO SeaLanes;

	public Texture2D OceanBasecolor;

	public Texture2D OceanDepthmap;

	public Texture2D TerrainColorMap;

	public Sprite MapImage;

	public Transform ReflectionProbePoint;

	public PositionRotation CameraPositionRotation;

	[SerializeField]
	private MapMusic[] factionMusic;

	public event Action BeforeDestroy;

	public RoadNetwork CreateRoadNetwork()
	{
		return SafeCreateRoad(RoadNetwork);
	}

	public RoadNetwork CreateSeaLanes()
	{
		return SafeCreateRoad(SeaLanes);
	}

	private RoadNetwork SafeCreateRoad(RoadNetworkSO so)
	{
		if (so == null)
		{
			return new RoadNetwork();
		}
		RoadNetworkSO roadNetworkSO = UnityEngine.Object.Instantiate(so);
		RoadNetwork roadNetwork = roadNetworkSO.RoadNetwork;
		roadNetwork.AllowMerge = true;
		UnityEngine.Object.Destroy(roadNetworkSO);
		return roadNetwork;
	}

	public AudioClip GetTacticalMusic(Faction faction)
	{
		MapMusic[] array = factionMusic;
		foreach (MapMusic mapMusic in array)
		{
			if (mapMusic.Faction == faction)
			{
				return mapMusic.GetTacticalMusic();
			}
		}
		return null;
	}

	public AudioClip GetStrategicMusic(Faction faction)
	{
		MapMusic[] array = factionMusic;
		foreach (MapMusic mapMusic in array)
		{
			if (mapMusic.Faction == faction)
			{
				return mapMusic.GetStrategicMusic();
			}
		}
		return null;
	}

	public AudioClip GetStartMusic(Faction faction)
	{
		MapMusic[] array = factionMusic;
		foreach (MapMusic mapMusic in array)
		{
			if (mapMusic.Faction == faction)
			{
				return mapMusic.GetStartMusic();
			}
		}
		return null;
	}

	public Color GetTerrainColorAtCoordinate(GlobalPosition globalPosition)
	{
		Vector2 vector = new Vector2(globalPosition.x + MapSize.x * 0.5f, globalPosition.z + MapSize.y * 0.5f);
		Vector2 vector2 = new Vector2(vector.x / MapSize.x, vector.y / MapSize.y);
		Vector2 vector3 = new Vector2(vector2.x * (float)TerrainColorMap.width, vector2.y * (float)TerrainColorMap.height);
		return TerrainColorMap.GetPixel((int)vector3.x, (int)vector3.y);
	}

	private void OnDestroy()
	{
		this.BeforeDestroy?.Invoke();
	}
}
