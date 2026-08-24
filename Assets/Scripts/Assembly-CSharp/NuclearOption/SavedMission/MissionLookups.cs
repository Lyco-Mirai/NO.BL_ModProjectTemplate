using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	public class MissionLookups
	{
		public readonly Dictionary<string, Objective> Objectives = new Dictionary<string, Objective>();

		public readonly Dictionary<string, Outcome> Outcomes = new Dictionary<string, Outcome>();

		public readonly Dictionary<string, SavedUnit> SavedUnits = new Dictionary<string, SavedUnit>();

		public readonly Dictionary<string, SavedAirbase> Airbases = new Dictionary<string, SavedAirbase>();

		public readonly LoadErrors LoadErrors;

		public MissionLookups(LoadErrors loadErrors)
		{
			LoadErrors = loadErrors;
		}

		public bool TryGetAirbaseReference(string airbaseUniqueName, out SavedAirbase airbase)
		{
			airbase = null;
			if (string.IsNullOrEmpty(airbaseUniqueName))
			{
				return false;
			}
			if (Airbases.TryGetValue(airbaseUniqueName, out airbase))
			{
				return true;
			}
			if (!airbaseUniqueName.StartsWith("<UNIT_AIRBASE>++"))
			{
				return false;
			}
			string text = airbaseUniqueName.Substring("<UNIT_AIRBASE>++".Length);
			if (!SavedUnits.TryGetValue(text, out var value))
			{
				return false;
			}
			if (value.Unit != null && value.Unit.TryGetComponent<Airbase>(out var component))
			{
				airbase = component.SavedAirbase;
			}
			else
			{
				airbase = new SavedAirbase
				{
					UniqueName = airbaseUniqueName,
					DisplayName = text
				};
			}
			Airbases.Add(airbaseUniqueName, airbase);
			return true;
		}
	}
}
