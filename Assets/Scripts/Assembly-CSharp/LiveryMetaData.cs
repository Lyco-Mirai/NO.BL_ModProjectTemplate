using System;
using NuclearOption.AddressableScripts;
using NuclearOption.ModScripts;
using Steamworks;

[Serializable]
[CopyToModProject]
public struct LiveryMetaData : IMetaData
{
	public string DisplayName;

	public string Faction;

	public string Aircraft;

	public string AircraftKey;

	public PublishedFileId_t Id { get; set; }

	public string FolderFullPath { get; set; }

	public readonly bool CheckAircraft(AircraftDefinition aircraft)
	{
		if (!string.IsNullOrEmpty(AircraftKey))
		{
			return AircraftKey == aircraft.jsonKey;
		}
		return Aircraft == aircraft.unitName;
	}
}
