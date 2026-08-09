using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class MissionEnvironment
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

		public float? moonPhaseNullable
		{
			get
			{
				if (!(moonPhase > -1f))
				{
					return null;
				}
				return moonPhase;
			}
		}
	}
}
