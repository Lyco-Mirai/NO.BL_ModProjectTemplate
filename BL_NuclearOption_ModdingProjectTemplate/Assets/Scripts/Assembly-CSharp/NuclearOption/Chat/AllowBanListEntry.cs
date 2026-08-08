using System;
using Steamworks;

namespace NuclearOption.Chat
{
	public readonly struct AllowBanListEntry
	{
		public readonly CSteamID SteamID;

		public readonly string Name;

		public readonly Action<CSteamID> DeleteAction;

		public AllowBanListEntry(CSteamID steamID, string name, Action<CSteamID> deleteAction)
		{
			SteamID = steamID;
			Name = name;
			DeleteAction = deleteAction;
		}
	}
}
