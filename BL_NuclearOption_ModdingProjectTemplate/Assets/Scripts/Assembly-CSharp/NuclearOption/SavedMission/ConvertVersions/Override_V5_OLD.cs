using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct Override_V5_OLD<T>
	{
		public bool IsOverride;

		public T Value;
	}
}
