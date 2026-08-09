using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedRunway : ISaveableReference
	{
		public string Name = "";

		public bool Reversable;

		public bool Takeoff;

		public bool Landing;

		public bool Arrestor;

		public bool SkiJump;

		public float Width;

		public GlobalPosition Start;

		public GlobalPosition End;

		public GlobalPosition[] exitPoints = new GlobalPosition[0];

		bool ISaveableReference.Destroyed { get; set; }

		string ISaveableReference.UniqueName => Name;

		bool ISaveableReference.CanBeReference => false;

		bool ISaveableReference.CanBeSorted => true;

		public event RenamedDelegate OnRenamed;

		public void Rename(string newName)
		{
			if (!(Name == newName))
			{
				string name = Name;
				Name = newName;
				this.OnRenamed?.Invoke(this, name, newName);
			}
		}

		public string ToUIString(bool oneLine = false)
		{
			string text = "Runway " + Name;
			if (oneLine)
			{
				return text;
			}
			return $"{text}\n{Start.AsVector3()} -> {End.AsVector3()}";
		}
	}
}
