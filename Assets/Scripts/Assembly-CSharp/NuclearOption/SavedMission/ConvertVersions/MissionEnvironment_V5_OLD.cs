using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class MissionEnvironment_V5_OLD
	{
		public float timeOfDay = 10f;

		public float timeFactor;

		public float weatherIntensity;

		public float cloudAltitude = 1800f;

		public float windSpeed;

		public float windTurbulence;

		public float windHeading;

		public float windRandomHeading;

		public float moonPhase = 14f;
	}
}
