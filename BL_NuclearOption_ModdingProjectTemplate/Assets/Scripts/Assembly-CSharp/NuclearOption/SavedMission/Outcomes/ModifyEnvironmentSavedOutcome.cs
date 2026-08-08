using System;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class ModifyEnvironmentSavedOutcome : SavedOutcome
	{
		public Override<float> timeOfDay;

		public Override<float> weather;

		public Override<float> cloudAltitude;

		public Override<float> windSpeed;

		public Override<float> windTurbulence;

		public Override<float> windHeading;

		public override OutcomeType OutcomeTypeEnum => OutcomeType.ModifyEnvironment;
	}
}
