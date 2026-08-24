using System;

namespace NuclearOption.SavedMission
{
	public class MissionLoadException : Exception
	{
		public readonly LoadErrors LoadErrors;

		public MissionLoadException(LoadErrors errors)
			: base($"Failed to load mission because of {errors.Exceptions.Count} errors")
		{
			LoadErrors = errors;
		}
	}
}
