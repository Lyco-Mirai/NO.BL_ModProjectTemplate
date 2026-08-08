using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedRunway_V5_OLD
	{
		public string Name;

		public bool Reversable;

		public bool Takeoff;

		public bool Landing;

		public bool Arrestor;

		public bool SkiJump;

		public float Width;

		public GlobalPosition Start;

		public GlobalPosition End;

		public GlobalPosition[] exitPoints = new GlobalPosition[0];
	}
}
