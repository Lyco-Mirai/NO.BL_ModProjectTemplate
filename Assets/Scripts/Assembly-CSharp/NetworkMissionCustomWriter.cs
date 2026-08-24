using System;
using Mirage.Serialization;

public static class NetworkMissionCustomWriter
{
	public static void WriteSyncMissionPart(this NetworkWriter writer, NetworkMission.SyncMissionPart part)
	{
		throw new NotSupportedException("SerailizedMessages.WriteSyncMissionPart should be used instead of this");
	}

	public static NetworkMission.SyncMissionPart ReadSyncMissionPart(this NetworkReader reader)
	{
		return NetworkMission.PartSender.ReadSyncMissionPart(reader);
	}
}
