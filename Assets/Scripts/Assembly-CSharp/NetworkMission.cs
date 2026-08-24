using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Mirage;
using Mirage.Events;
using Mirage.Logging;
using Mirage.Serialization;
using Mirage.Serialization.BrotliCompression;
using Mirage.SocketLayer;
using NuclearOption.SavedMission;
using Unity.Profiling;
using UnityEngine;

public class NetworkMission
{
	[NetworkMessage]
	public struct SyncMissionPart
	{
		public ArraySegment<byte> Bytes;
	}

	[NetworkMessage]
	public struct SyncMissionHeader
	{
		public string Name;

		public State State;

		public int JsonVersion;

		public MissionSettings missionSettings;

		public MissionEnvironment environment;
	}

	[NetworkMessage]
	public struct SyncMissionFooter
	{
		public string Name;
	}

	[NetworkMessage]
	public struct SyncMission
	{
		public string Name;

		public Mission Mission;

		public SyncMission(Mission mission)
		{
			Mission = mission;
			Name = mission?.Name;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	[NetworkMessage]
	public struct SyncMissionStart
	{
	}

	[Serializable]
	public enum State
	{
		Unloaded = 0,
		Loading = 1,
		Loaded = 2,
		Running = 3
	}

	public class PartSender
	{
		public static class SizeTracker<T>
		{
			private static bool AddedToEvent;

			public static int Count { get; private set; }

			public static int Bits { get; private set; }

			public static int Bytes => (Bits + 7) / 8;

			public static int AverageBytes => Bytes / Count;

			public static void Add(int bits)
			{
				if (!AddedToEvent)
				{
					AddedToEvent = true;
					LogSizeTracker += Log;
				}
				Count++;
				Bits += bits;
			}

			private static void Log()
			{
				if (logger.LogEnabled())
				{
					logger.Log($"Size from {typeof(T)}, Count:{Count}, Bytes:{Bytes} Average:{AverageBytes}");
				}
			}
		}

		public StringStore StringStore = new StringStore();

		private List<NetworkWriter> WriterParts = new List<NetworkWriter>();

		private StringStoreBrotliEncoder stringEncoder;

		public int TotalBytes { get; private set; }

		public int TotalMessages { get; private set; }

		private static event Action LogSizeTracker;

		public void WriteHeader(SyncMissionHeader header)
		{
			if (WriterParts.Count > 0)
			{
				throw new InvalidOperationException("SyncMissionHeader should be the first part and only written once");
			}
			NetworkWriter writer = GetWriter();
			MessagePacker.Pack(header, writer);
			WriterParts.Add(writer);
			TotalBytes += writer.ByteLength;
			TotalMessages++;
		}

		public NetworkWriter StartPart()
		{
			NetworkWriter writer = GetWriter();
			writer.WriteUInt16((ushort)MessagePacker.GetId<SyncMissionPart>());
			writer.WriteUInt16(0);
			return writer;
		}

		public void EndPart(NetworkWriter writer)
		{
			int num = writer.ByteLength - 4;
			if (num == 0)
			{
				logger.LogWarning("StartPartEnd called with empty segment");
				return;
			}
			if (num > 65535)
			{
				throw new EndOfStreamException("Mission part was over 64kb");
			}
			writer.WriteAtBytePosition((ulong)num, 16, 2);
			WriterParts.Add(writer);
			TotalBytes += num;
			TotalMessages++;
		}

		public static SyncMissionPart ReadSyncMissionPart(NetworkReader reader)
		{
			ushort count = reader.ReadUInt16();
			ArraySegment<byte> bytes = reader.ReadBytesSegment(count);
			return new SyncMissionPart
			{
				Bytes = bytes
			};
		}

		public void WriteFooter(SyncMissionFooter footer)
		{
			NetworkWriter writer = GetWriter();
			MessagePacker.Pack(footer, writer);
			WriterParts.Add(writer);
			TotalBytes += writer.ByteLength;
			TotalMessages++;
		}

		public void WriteStringStore()
		{
			long timestamp = BenchmarkScope.GetTimestamp();
			int num;
			using (brotliCompressionMarker.Auto())
			{
				stringEncoder = StringStoreBrotliEncoder.Encode(StringStore);
				(int lengthsByteCount, int stringsByteCount) byteLength = stringEncoder.GetByteLength();
				int item = byteLength.lengthsByteCount;
				int item2 = byteLength.stringsByteCount;
				num = item + item2;
				TotalBytes += num;
				TotalMessages += 2;
			}
			if (logger.WarnEnabled())
			{
				double num2 = BenchmarkScope.MillisecondsSince(timestamp);
				logger.LogWarning("SyncMission:\n" + $"  Strings: {StringStore.Strings.Count}\n" + $"  CharCount: {StringStore.Strings.Sum((string x) => x.Length)}\n" + $"  CompressedBytes: {num}\n" + $"  TotalBytes: {TotalBytes}\n" + $"  TotalMessages: {TotalMessages}\n" + $"  BrotliEncodingTook: {num2:0.000}ms");
			}
			PartSender.LogSizeTracker?.Invoke();
			StringStore = null;
		}

		private NetworkWriter GetWriter()
		{
			return new NetworkWriter(1100);
		}

		public void Send(INetworkPlayer player)
		{
			using (sendMissionPartsMarker.Auto())
			{
				try
				{
					if (logger.LogEnabled())
					{
						logger.Log($"Sending {WriterParts.Count} parts to {player}");
					}
					player.Send(WriterParts[0].ToArraySegment());
					stringEncoder.Send(player);
					for (int i = 1; i < WriterParts.Count; i++)
					{
						ArraySegment<byte> segment = WriterParts[i].ToArraySegment();
						player.Send(segment);
					}
				}
				catch (BufferFullException arg)
				{
					Debug.LogError($"Failed to send mission to {player} because their buffer was full: {arg}");
					player.Disconnect();
				}
			}
		}

		public static PartSender Create(SyncMission current)
		{
			PartSender partSender = new PartSender();
			SerializeMission(partSender, current);
			partSender.WriteStringStore();
			return partSender;
		}

		public static void SerializeMission(PartSender sender, SyncMission current)
		{
			using (serializeMissionPartsMarker.Auto())
			{
				SyncMissionHeader header = new SyncMissionHeader
				{
					Name = current.Name,
					JsonVersion = current.Mission.JsonVersion,
					environment = current.Mission.environment,
					missionSettings = current.Mission.missionSettings
				};
				if (logger.LogEnabled())
				{
					logger.Log("Sending header " + current.Name);
				}
				sender.WriteHeader(header);
				PooledNetworkWriter itemWriter = NetworkWriterPool.GetWriter();
				try
				{
					itemWriter.StringStore = sender.StringStore;
					WriteList<SavedAircraft>(current.Mission.aircraft);
					WriteList<SavedVehicle>(current.Mission.vehicles);
					WriteList<SavedShip>(current.Mission.ships);
					WriteList<SavedBuilding>(current.Mission.buildings);
					WriteList<SavedScenery>(current.Mission.scenery);
					WriteList<SavedContainer>(current.Mission.containers);
					WriteList<SavedMissile>(current.Mission.missiles);
					WriteList<SavedPilot>(current.Mission.pilots);
					WriteList<MissionFaction>(current.Mission.factions);
					WriteList<SavedAirbase>(current.Mission.airbases);
					WriteList<SavedObjective>(current.Mission.objectives);
					WriteList<SavedOutcome>(current.Mission.outcomes);
				}
				finally
				{
					if (itemWriter != null)
					{
						((IDisposable)itemWriter).Dispose();
					}
				}
				if (logger.LogEnabled())
				{
					logger.Log("Sending footer " + current.Name);
				}
				sender.WriteFooter(new SyncMissionFooter
				{
					Name = current.Name
				});
				void WriteList<T>(List<T> list)
				{
					NetworkWriter networkWriter = sender.StartPart();
					if (logger.LogEnabled())
					{
						logger.Log($"Writing {list.GetType()} {list.Count} items");
					}
					networkWriter.WritePackedUInt32((uint)list.Count);
					foreach (T item in list)
					{
						int bitPosition = itemWriter.BitPosition;
						itemWriter.Write(item);
						SizeTracker<T>.Add(itemWriter.BitPosition - bitPosition);
						if (itemWriter.ByteLength + networkWriter.ByteLength > 1100)
						{
							if (logger.LogEnabled())
							{
								logger.Log($"Sending part {networkWriter.ByteLength} bytes");
							}
							sender.EndPart(networkWriter);
							networkWriter = sender.StartPart();
						}
						networkWriter.PadToByte();
						networkWriter.CopyFromWriter(itemWriter);
						itemWriter.Reset();
					}
					if (networkWriter.ByteLength > 0)
					{
						if (logger.LogEnabled())
						{
							logger.Log($"Sending part {networkWriter.ByteLength} bytes (last)");
						}
						sender.EndPart(networkWriter);
					}
				}
			}
		}
	}

	private class PartTracker
	{
		private readonly List<Func<ArraySegment<byte>, bool>> readers;

		private int currentList;

		private uint? currentListCount;

		private readonly StringStoreBrotliDecoder decoder;

		private StringStore stringStore => decoder.StringStore;

		public PartTracker(Mission mission, StringStoreBrotliDecoder decoder)
		{
			PartTracker partTracker = this;
			this.decoder = decoder;
			readers = new List<Func<ArraySegment<byte>, bool>>
			{
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.aircraft),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.vehicles),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.ships),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.buildings),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.scenery),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.containers),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.missiles),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.pilots),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.factions),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.airbases),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.objectives),
				(ArraySegment<byte> b) => partTracker.ReadList(b, mission.outcomes)
			};
		}

		private bool ReadList<T>(ArraySegment<byte> bytes, List<T> list)
		{
			using PooledNetworkReader pooledNetworkReader = NetworkReaderPool.GetReader(bytes, null);
			pooledNetworkReader.StringStore = stringStore;
			if (!currentListCount.HasValue)
			{
				currentListCount = pooledNetworkReader.ReadPackedUInt32();
				if (logger.LogEnabled())
				{
					logger.Log($"Start reading {currentListCount.Value} items");
				}
			}
			if (list.Count == currentListCount)
			{
				currentListCount = null;
				if (logger.LogEnabled())
				{
					logger.Log("Read all items");
				}
				return true;
			}
			while (pooledNetworkReader.CanReadBytes(1))
			{
				T item = pooledNetworkReader.Read<T>();
				pooledNetworkReader.PadToByte();
				list.Add(item);
				if (list.Count == currentListCount)
				{
					currentListCount = null;
					if (logger.LogEnabled())
					{
						logger.Log("Read all items");
					}
					return true;
				}
			}
			if (logger.LogEnabled())
			{
				logger.Log("Done with part, not finished list");
			}
			return false;
		}

		public void ReadNext(ArraySegment<byte> bytes)
		{
			if (stringStore == null)
			{
				throw new InvalidOperationException("stringStore should have been received before part");
			}
			if (readers[currentList](bytes))
			{
				if (logger.LogEnabled())
				{
					logger.Log($"Done with list {currentList}/{readers.Count}");
				}
				currentList++;
			}
		}

		public bool IsAtEnd()
		{
			return readers.Count == currentList;
		}
	}

	private static readonly ProfilerMarker serverAuthenticatedMarker = new ProfilerMarker("NetworkMission.Authenticated");

	private static readonly ProfilerMarker setMissionMarker = new ProfilerMarker("NetworkMission.SetMission");

	private static readonly ProfilerMarker serializeMissionSingleMarker = new ProfilerMarker("NetworkMission.SerializeSingle");

	private static readonly ProfilerMarker sendMissionSingleMarker = new ProfilerMarker("NetworkMission.SendSingle");

	private static readonly ProfilerMarker serializeMissionPartsMarker = new ProfilerMarker("NetworkMission.SerializeParts");

	private static readonly ProfilerMarker brotliCompressionMarker = new ProfilerMarker("NetworkMission.BrotliCompression");

	private static readonly ProfilerMarker sendMissionPartsMarker = new ProfilerMarker("NetworkMission.SendParts");

	public static readonly ILogger logger = LogFactory.GetLogger<NetworkMission>();

	private const int SINGLE_SEND_MISSION_SIZE = 64000;

	private const int MAX_MESSAGE_SIZE = 1100;

	private SyncMission current;

	private State currentState;

	private NetworkWriter missionWriter;

	private bool sendAsParts;

	private readonly NetworkServer server;

	private readonly NetworkClient client;

	private PartSender partSender;

	private PartTracker partTracker;

	private readonly AddLateEvent<(SyncMission mission, State state, bool stateChangeOnly)> changed = new AddLateEvent<(SyncMission, State, bool)>();

	public IAddLateEvent<(SyncMission mission, State state, bool stateChangeOnly)> Changed => changed;

	public NetworkMission(NetworkServer server, NetworkClient client)
	{
		this.server = server;
		this.client = client;
		client.Started.AddListener(ClientStarted);
		server.Authenticated.AddListener(ServerAuthenticated);
	}

	private void ServerAuthenticated(INetworkPlayer player)
	{
		using (serverAuthenticatedMarker.Auto())
		{
			if (player != server.LocalPlayer && current.Mission != null)
			{
				if (sendAsParts)
				{
					SendPartsToPlayer(player);
				}
				else
				{
					ArraySegment<byte> segment = missionWriter.ToArraySegment();
					player.Send(segment);
				}
				if (currentState == State.Running)
				{
					player.Send(default(SyncMissionStart));
				}
			}
		}
	}

	private void ClientStarted()
	{
		((IMessageReceiver)client.MessageHandler).RegisterHandler((MessageDelegate<SyncMissionStart>)OnReceiveMissionStart, false);
		((IMessageReceiver)client.MessageHandler).RegisterHandler((MessageDelegate<SyncMission>)OnReceiveMission, false);
		((IMessageReceiver)client.MessageHandler).RegisterHandler((MessageDelegateWithPlayer<SyncMissionHeader>)OnReceiveMissionHeader);
		((IMessageReceiver)client.MessageHandler).RegisterHandler((MessageDelegateWithPlayer<SyncMissionPart>)OnReceiveMissionPart);
		((IMessageReceiver)client.MessageHandler).RegisterHandler((MessageDelegateWithPlayer<SyncMissionFooter>)OnReceiveMissionFooter);
	}

	private void OnReceiveMission(SyncMission message)
	{
		State state = ((message.Mission != null) ? State.Loaded : State.Unloaded);
		if (message.Mission == null)
		{
			message.Mission = Mission.NullMission;
		}
		ColorLog<NetworkMission>.Info($"OnReceiveMission ({currentState}, {current.Name}) -> ({state}, {message.Name})");
		current = message;
		currentState = state;
		changed?.Invoke((current, currentState, false));
	}

	private void OnReceiveMissionStart(SyncMissionStart message)
	{
		State state = State.Running;
		ColorLog<NetworkMission>.Info($"OnReceiveState: {currentState} -> {state}");
		currentState = state;
		changed?.Invoke((current, currentState, true));
	}

	public void SetRunning()
	{
		if (currentState == State.Running)
		{
			ColorLog<NetworkMission>.Info($"Mission was already in state {State.Running}");
			return;
		}
		ColorLog<NetworkMission>.Info($"Setting state to {currentState} -> {State.Running}");
		currentState = State.Running;
		server.SendToAll(default(SyncMissionStart), authenticatedOnly: true, excludeLocalPlayer: true);
	}

	public void Set(Mission mission, State state)
	{
		using (setMissionMarker.Auto())
		{
			if (mission == Mission.NullMission)
			{
				mission = null;
			}
			if (mission == current.Mission)
			{
				ColorLog<NetworkMission>.Info("Mission already equal to " + (mission?.Name ?? "NULL") + ", skipping set");
				return;
			}
			partSender = null;
			if (missionWriter != null)
			{
				missionWriter.Reset();
			}
			else
			{
				missionWriter = new NetworkWriter(64000, allowResize: false);
			}
			ColorLog<NetworkMission>.Info(string.Format("Setting mission to ({0}, {1})", state, mission?.Name ?? "NULL"));
			current = new SyncMission(mission);
			currentState = state;
			bool flag = false;
			if (!IsMissionBig())
			{
				flag = TrySingleSend();
			}
			sendAsParts = !flag;
			if (!flag)
			{
				if (mission == null)
				{
					throw new InvalidOperationException("Null mission should have been sent with TrySingleSend");
				}
				CreatePartSenderAsync(state);
			}
			else
			{
				AfterMissionSent(state);
			}
		}
	}

	private void CreatePartSenderAsync(State state)
	{
		partSender = PartSender.Create(current);
		SendPartsToAll();
		AfterMissionSent(state);
	}

	private void AfterMissionSent(State state)
	{
		if (state == State.Running)
		{
			server.SendToAll(default(SyncMissionStart), authenticatedOnly: true, excludeLocalPlayer: true);
		}
	}

	public void Clear()
	{
		ColorLog<NetworkMission>.Info("Clear Mission");
		Set(null, State.Unloaded);
	}

	private bool IsMissionBig()
	{
		if (current.Mission == null)
		{
			return false;
		}
		return (SafeCount<SavedAircraft>(current.Mission.aircraft) + SafeCount<SavedVehicle>(current.Mission.vehicles) + SafeCount<SavedShip>(current.Mission.ships) + SafeCount<SavedBuilding>(current.Mission.buildings) + SafeCount<SavedScenery>(current.Mission.scenery) + SafeCount<SavedContainer>(current.Mission.containers) + SafeCount<SavedMissile>(current.Mission.missiles) + SafeCount<SavedPilot>(current.Mission.pilots) + SafeCount<MissionFaction>(current.Mission.factions) + SafeCount<SavedAirbase>(current.Mission.airbases) + SafeCount<SavedObjective>(current.Mission.objectives) + SafeCount<SavedOutcome>(current.Mission.outcomes)) * 40 > 30000;
		static int SafeCount<T>(List<T> list)
		{
			return list?.Count ?? 0;
		}
	}

	private bool TrySingleSend()
	{
		try
		{
			using (serializeMissionSingleMarker.Auto())
			{
				MessagePacker.Pack(current, missionWriter);
			}
			using (sendMissionSingleMarker.Auto())
			{
				ArraySegment<byte> segment = missionWriter.ToArraySegment();
				foreach (INetworkPlayer authenticatedPlayer in server.AuthenticatedPlayers)
				{
					if (!authenticatedPlayer.IsHost)
					{
						authenticatedPlayer.Send(segment);
					}
				}
				return true;
			}
		}
		catch (InvalidOperationException ex)
		{
			missionWriter.Reset();
			if (ex.Message.Contains("Can not write over end of buffer, new length "))
			{
				if (logger.WarnEnabled())
				{
					logger.LogWarning($"Mission was larger than {64000}, Sending as parts instead");
				}
				return false;
			}
			throw;
		}
	}

	private void SendPartsToAll()
	{
		foreach (INetworkPlayer authenticatedPlayer in server.AuthenticatedPlayers)
		{
			if (!authenticatedPlayer.IsHost)
			{
				SendPartsToPlayer(authenticatedPlayer);
			}
		}
	}

	private void SendPartsToPlayer(INetworkPlayer player)
	{
		partSender.Send(player);
	}

	private void OnReceiveMissionHeader(INetworkPlayer player, SyncMissionHeader message)
	{
		current = new SyncMission
		{
			Name = message.Name,
			Mission = new Mission()
		};
		ColorLog<NetworkMission>.Info($"Reading header {current.Name}. {currentState} -> {State.Loading}");
		currentState = State.Loading;
		current.Mission.JsonVersion = message.JsonVersion;
		current.Mission.missionSettings = message.missionSettings;
		current.Mission.environment = message.environment;
		current.Mission.objectives = new List<SavedObjective>();
		current.Mission.outcomes = new List<SavedOutcome>();
		StringStoreBrotliDecoder decoder = new StringStoreBrotliDecoder(client.MessageHandler);
		partTracker = new PartTracker(current.Mission, decoder);
	}

	private void OnReceiveMissionPart(INetworkPlayer player, SyncMissionPart message)
	{
		if (partTracker == null)
		{
			logger.LogWarning("Received another SyncMissionPart but tracker was null. (this could be from error in previous part)");
			return;
		}
		try
		{
			if (logger.LogEnabled())
			{
				logger.Log($"Part {message.Bytes.Count} bytes");
			}
			partTracker.ReadNext(message.Bytes);
		}
		catch
		{
			logger.LogError("Failed to read SyncMissionPart");
			current = default(SyncMission);
			partTracker = null;
			throw;
		}
	}

	private void OnReceiveMissionFooter(INetworkPlayer player, SyncMissionFooter message)
	{
		if (message.Name != current.Name)
		{
			throw new ArgumentException("Name mismatch for missions");
		}
		if (!partTracker.IsAtEnd())
		{
			throw new ArgumentException("Not at end of parts");
		}
		partTracker = null;
		if (logger.LogEnabled())
		{
			logger.Log("Reading footer " + current.Name);
		}
		ColorLog<NetworkMission>.Info($"OnReceiveMissionFooter: {message.Name}. {currentState} -> {State.Loaded}");
		currentState = State.Loaded;
		changed.Invoke((current, currentState, false));
	}
}
