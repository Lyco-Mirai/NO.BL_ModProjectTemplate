using UnityEngine;

namespace NuclearOption.SceneLoading
{
	[CreateAssetMenu(fileName = "MapDetails", menuName = "ScriptableObjects/MapDetails", order = 997)]
	public class MapDetails : ScriptableObject
	{
		public string PrefabName;

		public string MapName;

		public Sprite MapImage;
	}
}
