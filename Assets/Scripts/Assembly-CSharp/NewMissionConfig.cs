using System.Collections.Generic;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;

public struct NewMissionConfig
{
	public string Name;

	public MapKey Map;

	public PlayerMode PlayerMode;

	public List<string> JoinableFactions;

	public bool CanJoinAllFactions => JoinableFactions == null;

	public NewMissionConfig(string name, MapKey map, PlayerMode playerMode, List<string> joinableFactions)
	{
		Name = name;
		Map = map;
		PlayerMode = playerMode;
		JoinableFactions = joinableFactions;
	}

	public static NewMissionConfig DefaultMission(MapKey map = default(MapKey))
	{
		PlayerMode playerMode = PlayerMode.SingleAndMultiplayer;
		List<string> joinableFactions = new List<string> { "Boscali", "Primeva" };
		return new NewMissionConfig("Untitled Mission", map, playerMode, joinableFactions);
	}
}
