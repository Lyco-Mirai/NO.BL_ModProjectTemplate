using System.Linq;
using NuclearOption.ModScripts.Impl;

namespace NuclearOption.ModScripts
{
	public static class ModTypes
	{
		public static readonly ModType Missions = new MissionMod();

		public static readonly ModType AircraftLivery = new AircraftLiveryMod();

		public static readonly ModType[] ModTypesArray = new ModType[2] { Missions, AircraftLivery };

		public static ModType FromTag(string tag)
		{
			return ModTypesArray.First((ModType x) => x.Tag == tag);
		}
	}
}
