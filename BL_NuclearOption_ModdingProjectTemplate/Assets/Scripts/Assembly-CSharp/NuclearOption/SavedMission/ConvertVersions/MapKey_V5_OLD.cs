using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct MapKey_V5_OLD
	{
		[Obsolete("V5", true)]
		public enum KeyType_V5_OLD : byte
		{
			None = 0,
			GameWorldPrefab = 1,
			BuiltinScene = 2
		}

		public KeyType_V5_OLD Type;

		public string TypeName;

		public string Path;
	}
}
