using System;
using System.Text;
using Steamworks;

namespace NuclearOption.Social
{
	[Serializable]
	public struct PresenceState
	{
		public GameState Context;

		public PresenceActivity Activity;

		public string MissionName;

		public string AircraftName;

		public string AirbaseName;

		public string FactionName;

		public string MapKey;

		public CSteamID PartyId;

		public int CurrentPlayers;

		public int MaxPlayers;

		public bool IsHost;

		public DateTime StartTime;

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("Context: ");
			stringBuilder.Append(Context);
			if (Activity != PresenceActivity.None)
			{
				stringBuilder.Append(" | ");
				stringBuilder.Append(Activity);
			}
			if (!string.IsNullOrEmpty(MissionName))
			{
				stringBuilder.Append(" | Mission: ");
				stringBuilder.Append(MissionName);
			}
			if (!string.IsNullOrEmpty(AircraftName))
			{
				stringBuilder.Append(" (Aircraft: " + AircraftName + ")");
			}
			if (!string.IsNullOrEmpty(AirbaseName))
			{
				stringBuilder.Append(" (Airbase: " + AirbaseName + ")");
			}
			if (!string.IsNullOrEmpty(FactionName))
			{
				stringBuilder.Append(" (Faction: " + FactionName + ")");
			}
			if (!string.IsNullOrEmpty(MapKey))
			{
				stringBuilder.Append(" | Map: ");
				stringBuilder.Append(MapKey);
			}
			if (PartyId != CSteamID.Nil)
			{
				stringBuilder.Append($" | Party: {PartyId} ({CurrentPlayers}/{MaxPlayers})");
			}
			if (IsHost)
			{
				stringBuilder.Append(" | Host");
			}
			return stringBuilder.ToString();
		}
	}
}
