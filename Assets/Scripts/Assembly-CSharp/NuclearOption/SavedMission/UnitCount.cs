using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class UnitCount : ICloneable
	{
		public string UnitType = "";

		public int Count;

		public UnitCount()
		{
			UnitType = string.Empty;
			Count = 1;
		}

		public UnitCount(string type, int number)
		{
			UnitType = type;
			Count = number;
		}

		public object Clone()
		{
			return new UnitCount(UnitType, Count);
		}
	}
}
