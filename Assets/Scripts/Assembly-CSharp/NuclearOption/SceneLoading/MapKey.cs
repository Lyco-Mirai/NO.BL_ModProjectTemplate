using System;

namespace NuclearOption.SceneLoading
{
	[Serializable]
	public struct MapKey : IEquatable<MapKey>
	{
		[Serializable]
		public enum KeyType : byte
		{
			None = 0,
			GameWorldPrefab = 1,
			BuiltinScene = 2
		}

		public KeyType Type;

		public string Path;

		[NonSerialized]
		public bool SkipChecks;

		public MapKey(KeyType type, string path)
		{
			Type = type;
			Path = path;
			SkipChecks = false;
		}

		public static MapKey GameWorldPrefab(string path)
		{
			return new MapKey(KeyType.GameWorldPrefab, path);
		}

		public static MapKey BuiltinScene(string path)
		{
			return new MapKey(KeyType.BuiltinScene, path);
		}

		public static MapKey AddressableBuiltin(string path)
		{
			throw new NotImplementedException();
		}

		public static MapKey AddressableAppData(string path)
		{
			throw new NotImplementedException();
		}

		public static MapKey AddressableWorkshop(string path)
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			return $"({Type},{Path})";
		}

		public readonly bool Equals(MapKey other)
		{
			if (Type == other.Type)
			{
				return Path == other.Path;
			}
			return false;
		}

		public readonly bool IsDefault()
		{
			return default(MapKey).Equals(this);
		}
	}
}
