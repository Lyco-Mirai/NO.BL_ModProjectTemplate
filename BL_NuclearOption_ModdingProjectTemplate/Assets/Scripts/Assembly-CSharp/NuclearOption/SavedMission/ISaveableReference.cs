namespace NuclearOption.SavedMission
{
	public interface ISaveableReference
	{
		bool CanBeSorted { get; }

		bool Destroyed { get; set; }

		string UniqueName { get; }

		bool CanBeReference { get; }

		event RenamedDelegate OnRenamed;

		void Rename(string newName);

		string ToUIString(bool oneLine = false);
	}
}
