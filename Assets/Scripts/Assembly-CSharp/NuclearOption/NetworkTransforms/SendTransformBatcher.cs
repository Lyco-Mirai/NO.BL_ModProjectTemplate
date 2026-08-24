using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Serialization;
using NuclearOption.DebugScripts;
using NuclearOption.Debugging;
using NuclearOption.Networking;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class SendTransformBatcher : MonoBehaviour
	{
		[Serializable]
		public struct SmoothTimeValues
		{
			public float MaxExtrapolation;

			public float MaxSnapshotAge;

			public float SnapThreshold;

			public static SmoothTimeValues Lerp(SmoothTimeValues a, SmoothTimeValues b, float t)
			{
				return new SmoothTimeValues
				{
					MaxExtrapolation = Mathf.Lerp(a.MaxExtrapolation, b.MaxExtrapolation, t),
					MaxSnapshotAge = Mathf.Lerp(a.MaxSnapshotAge, b.MaxSnapshotAge, t),
					SnapThreshold = Mathf.Lerp(a.SnapThreshold, b.SnapThreshold, t)
				};
			}
		}

		[Serializable]
		public enum ClientTimeMode
		{
			EMA_Offset = 0,
			SmoothNetworkTimeStep = 1
		}

		[NetworkMessage]
		private struct TransformMessage
		{
			public double timestamp;

			public ArraySegment<byte> data;
		}

		private static readonly ProfilerMarker clientUpdateMarker = new ProfilerMarker("ClientUpdate");

		private static readonly ProfilerMarker serverUpdateMarker = new ProfilerMarker("ServerUpdate");

		private const int MAX_MESSAGE_SIZE = 1100;

		private const int SNAPSHOT_BITCOUNT_SIZE = 16;

		[SerializeField]
		private NetworkServer Server;

		[SerializeField]
		private NetworkClient Client;

		[SerializeField]
		private SendTransformBatcherDebugger debugger;

		[Header("Time Smoothing Settings")]
		[Tooltip("how many seconds to use min smooth values for after joining")]
		[SerializeField]
		private float delayAfterJoin = 2f;

		[Tooltip("how many seconds to lerp up to max smooth values")]
		[SerializeField]
		private float delayTillMaxSmooth = 10f;

		[SerializeField]
		private float setTransformRange = 1000f;

		private Camera camera;

		private Vector3 cameraPosition;

		[SerializeField]
		private SmoothTimeValues startTimeValues = new SmoothTimeValues
		{
			MaxExtrapolation = 0f,
			MaxSnapshotAge = 0f,
			SnapThreshold = 0.4f
		};

		[SerializeField]
		private SmoothTimeValues fullTimeValues = new SmoothTimeValues
		{
			MaxExtrapolation = 0.2f,
			MaxSnapshotAge = 0.5f,
			SnapThreshold = 2f
		};

		private readonly List<AircraftNetworkTransform> _behavioursClientAircraft = new List<AircraftNetworkTransform>();

		private readonly List<NetworkTransformBase> _behavioursClientOther = new List<NetworkTransformBase>();

		private readonly List<NetworkTransformBase> _behavioursAll = new List<NetworkTransformBase>();

		private readonly Dictionary<NetworkIdentity, NetworkTransformBase> _lookup = new Dictionary<NetworkIdentity, NetworkTransformBase>();

		private readonly List<INetworkPlayer> playerCache = new List<INetworkPlayer>();

		private double lastReceive;

		private SmoothNetworkTime smoothTime;

		private double lastServerTime;

		private bool snapTime;

		private double firstReceivedTime;

		private SmoothTimeValues currentTimeValues;

		[NonSerialized]
		public ClientAuthStream clientAuthDebugStream;

		[Header("Debug Settings")]
		public LineRenderer LineRendererPrefab;

		public bool NoPlayers { get; private set; }

		public double LocalSnapshotTime => Time.fixedTimeAsDouble;

		public double ServerSmoothTime => smoothTime.InterpolationTime;

		public float MaxExtrapolation => currentTimeValues.MaxExtrapolation;

		public double Debug_extrapolationOffset => smoothTime?.GetExtrapolationOffset(MaxExtrapolation) ?? 0.0;

		public bool IsCloseToCamera(Vector3 position)
		{
			if (GameManager.IsHeadless)
			{
				return false;
			}
			return FastMath.InRange(cameraPosition, position, setTransformRange);
		}

		private void Start()
		{
			Server.ManualUpdate = true;
			Client.ManualUpdate = true;
			EarlyUpdate().Forget();
			Server.Started.AddListener(ServerStarted);
			Server.Stopped.AddListener(ServerStopped);
			Client.Started.AddListener(ClientStarted);
			Client.Disconnected.AddListener(ClientStopped);
		}

		private void OnDestroy()
		{
			clientAuthDebugStream?.Dispose();
			clientAuthDebugStream = null;
		}

		private void ClientStarted()
		{
			if (!Server.Active)
			{
				AddWorldEvents(Client.World);
				((IMessageReceiver)Client.MessageHandler).RegisterHandler((MessageDelegate<TransformMessage>)HandleTransformMessage, false);
				lastServerTime = 0.0;
				lastReceive = 0.0;
				currentTimeValues = startTimeValues;
				smoothTime = new SmoothNetworkTime();
			}
		}

		private void ServerStarted()
		{
			AddWorldEvents(Server.World);
			lastServerTime = 0.0;
			lastReceive = 0.0;
			currentTimeValues = startTimeValues;
			smoothTime = new SmoothNetworkTime();
			if (!string.IsNullOrEmpty(ClientAuthStream.OpenPath))
			{
				clientAuthDebugStream = new ClientAuthStream();
				clientAuthDebugStream.Open(ClientAuthStream.OpenPath);
			}
		}

		private void ClientStopped(ClientStoppedReason arg0)
		{
			if (!Server.Active)
			{
				Cleanup();
			}
		}

		private void ServerStopped()
		{
			Cleanup();
			clientAuthDebugStream?.Dispose();
			clientAuthDebugStream = null;
		}

		private void Cleanup()
		{
			_behavioursClientAircraft.Clear();
			_behavioursClientOther.Clear();
			_behavioursAll.Clear();
			_lookup.Clear();
			lastReceive = 0.0;
			currentTimeValues = startTimeValues;
			smoothTime = null;
			lastServerTime = 0.0;
			snapTime = false;
		}

		private void AddWorldEvents(NetworkWorld world)
		{
			world.onUnspawn += World_onUnspawn;
			world.AddAndInvokeOnSpawn(World_onSpawn);
		}

		private void World_onSpawn(NetworkIdentity identity)
		{
			if (identity.TryGetComponent<NetworkTransformBase>(out var component))
			{
				if (component is AircraftNetworkTransform item)
				{
					_behavioursClientAircraft.Add(item);
				}
				else
				{
					_behavioursClientOther.Add(component);
				}
				_behavioursAll.Add(component);
				_lookup.Add(identity, component);
				component.Setup(this);
				component.ResetUpdateTime(Time.timeAsDouble);
			}
		}

		private void World_onUnspawn(uint netId, NetworkIdentity identity)
		{
			if (_lookup.TryGetValue(identity, out var value))
			{
				if (value is AircraftNetworkTransform item)
				{
					_behavioursClientAircraft.Remove(item);
				}
				else
				{
					_behavioursClientOther.Remove(value);
				}
				_behavioursAll.Remove(value);
				_lookup.Remove(identity);
			}
		}

		private async UniTask EarlyUpdate()
		{
			CancellationToken cancel = base.destroyCancellationToken;
			await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
			while (!cancel.IsCancellationRequested)
			{
				PlayerLoopPerformanceTracker.StartReceive();
				Server.UpdateReceive();
				Client.UpdateReceive();
				clientAuthDebugStream?.MarkUpdatedFinished();
				PlayerLoopPerformanceTracker.EndReceive();
				await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
			}
		}

		private void LateUpdate()
		{
			if (GameManager.gameState == GameState.Multiplayer && !NetworkManagerNuclearOption.IsLoadingScene)
			{
				if (Client.Active)
				{
					ClientUpdate();
				}
				if (Server.Active)
				{
					ServerUpdate();
				}
			}
			PlayerLoopPerformanceTracker.StartSend();
			Server.UpdateSent();
			Client.UpdateSent();
			PlayerLoopPerformanceTracker.EndSend();
		}

		private void ClientUpdate()
		{
			using (clientUpdateMarker.Auto())
			{
				smoothTime.Update(Time.unscaledDeltaTime);
				if (!Server.Active || Server.AuthenticatedPlayers.Count > 1)
				{
					VisualUpdate();
				}
				snapTime = false;
			}
		}

		private void VisualUpdate()
		{
			if (camera == null)
			{
				camera = Camera.main;
			}
			cameraPosition = camera.transform.position;
			bool active = Server.Active;
			double interpolationTime = smoothTime.InterpolationTime;
			double extrapolationOffset = smoothTime.GetExtrapolationOffset(MaxExtrapolation);
			VisualUpdateTime visualTime = new VisualUpdateTime
			{
				interpolationTime = interpolationTime,
				extrapolationOffset = extrapolationOffset,
				maxExtrapolateAge = currentTimeValues.MaxSnapshotAge,
				snap = snapTime
			};
			foreach (AircraftNetworkTransform item in _behavioursClientAircraft)
			{
				if (!active || (object)item != null)
				{
					item.VisualUpdate(ref visualTime);
					double timestamp = interpolationTime - (double)(item.SyncInterval * 8f);
					item.SnapshotBuffer.RemoveOld(timestamp);
				}
			}
			foreach (NetworkTransformBase item2 in _behavioursClientOther)
			{
				item2.VisualUpdate(ref visualTime);
				double timestamp2 = interpolationTime - (double)(item2.SyncInterval * 8f);
				item2.SnapshotBuffer.RemoveOld(timestamp2);
			}
			if (DebugVis.Enabled && SceneSingleton<CameraStateManager>.i != null)
			{
				debugger.UpdateDebugFollow(ref visualTime);
			}
		}

		private void ServerUpdate()
		{
			using (serverUpdateMarker.Auto())
			{
				double localSnapshotTime = LocalSnapshotTime;
				if (lastServerTime == localSnapshotTime)
				{
					return;
				}
				lastServerTime = localSnapshotTime;
				if (Server.IsHost)
				{
					smoothTime.OnMessage(lastServerTime, 0.0, 2f, out var snap);
					snapTime |= snap;
				}
				double unscaledTimeAsDouble = Time.unscaledTimeAsDouble;
				playerCache.Clear();
				foreach (INetworkPlayer authenticatedPlayer in Server.AuthenticatedPlayers)
				{
					if (authenticatedPlayer.SceneIsReady && !authenticatedPlayer.IsHost)
					{
						playerCache.Add(authenticatedPlayer);
					}
				}
				NoPlayers = playerCache.Count == 0;
				using PooledNetworkWriter pooledNetworkWriter = NetworkWriterPool.GetWriter();
				using PooledNetworkWriter pooledNetworkWriter2 = NetworkWriterPool.GetWriter();
				bool flag = false;
				foreach (NetworkTransformBase item in _behavioursAll)
				{
					if (item.TimeToUpdate(unscaledTimeAsDouble) && !NoPlayers && item.ShouldSend())
					{
						pooledNetworkWriter.WriteNetworkIdentity(item.Identity);
						item.Write(pooledNetworkWriter);
						if (pooledNetworkWriter.ByteLength + pooledNetworkWriter2.ByteLength > 1100)
						{
							Send(pooledNetworkWriter2, localSnapshotTime);
							flag = true;
							pooledNetworkWriter2.Reset();
						}
						int bitPosition = pooledNetworkWriter.BitPosition;
						pooledNetworkWriter2.Write((ulong)bitPosition, 16);
						pooledNetworkWriter2.CopyFromWriter(pooledNetworkWriter);
						pooledNetworkWriter.Reset();
					}
				}
				if (!flag || pooledNetworkWriter2.ByteLength > 0)
				{
					Send(pooledNetworkWriter2, localSnapshotTime);
				}
			}
		}

		private void Send(PooledNetworkWriter fullWriter, double networkTime)
		{
			NetworkServer.SendToMany(playerCache, new TransformMessage
			{
				timestamp = networkTime,
				data = fullWriter.ToArraySegment()
			}, Mirage.Channel.Unreliable);
		}

		private void HandleTransformMessage(TransformMessage message)
		{
			if (lastReceive > message.timestamp)
			{
				return;
			}
			if (lastReceive != message.timestamp)
			{
				if (lastReceive == 0.0)
				{
					firstReceivedTime = message.timestamp;
				}
				float num = (float)(message.timestamp - firstReceivedTime);
				if (num <= delayAfterJoin)
				{
					currentTimeValues = startTimeValues;
				}
				else
				{
					float num2 = num - delayAfterJoin;
					if (num2 < delayTillMaxSmooth)
					{
						currentTimeValues = SmoothTimeValues.Lerp(startTimeValues, fullTimeValues, num2 / delayTillMaxSmooth);
					}
					else
					{
						currentTimeValues = fullTimeValues;
					}
				}
				double extrapolationOffset = Client.World.Time.Rtt / 2.0;
				smoothTime.OnMessage(message.timestamp, extrapolationOffset, currentTimeValues.SnapThreshold, out var snap);
				snapTime |= snap;
			}
			lastReceive = message.timestamp;
			using PooledNetworkReader pooledNetworkReader = NetworkReaderPool.GetReader(message.data, Client.World);
			while (pooledNetworkReader.CanReadBits(16))
			{
				int num3 = (int)pooledNetworkReader.Read(16);
				int bitPosition = pooledNetworkReader.BitPosition;
				NetworkIdentity networkIdentity = pooledNetworkReader.ReadNetworkIdentity();
				if (networkIdentity != null && _lookup.TryGetValue(networkIdentity, out var value))
				{
					value.Receive(message.timestamp, pooledNetworkReader);
				}
				else
				{
					pooledNetworkReader.MoveBitPosition(bitPosition + num3);
				}
				_ = pooledNetworkReader.BitPosition;
			}
		}
	}
}
