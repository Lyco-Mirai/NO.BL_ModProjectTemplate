using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace NuclearOption.AddressableScripts
{
	public static class ModLoadCache
	{
		public class LocationCache
		{
			public bool TaskPending;

			public IResourceLocation Location;
		}

		public static readonly Dictionary<string, LocationCache> CacheLocations = new Dictionary<string, LocationCache>();

		public static readonly List<LiveryMetaData> SkinMetaData = new List<LiveryMetaData>();

		public static bool HasSkinMetaData;

		public static void Clear()
		{
			ColorLog<LocationCache>.Info("Clearing Addressable Location cache");
			CacheLocations.Clear();
			SkinMetaData.Clear();
			HasSkinMetaData = false;
		}
	}
}
