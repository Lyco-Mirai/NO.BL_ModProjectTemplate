using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class AircraftNetworkTransform : NetworkTransformBase
	{
		private static readonly ProfilerMarker applySnapshotMarker = new ProfilerMarker("ApplySnapshot");

		private static readonly ProfilerMarker visualUpdateMarker = new ProfilerMarker("Aircraft.VisualUpdate");

		private const float CLIENT_AUTH_MIN_TIME_STEP = 0.015f;

		private const int CLIENT_AUTH_MAX_QUEUED = 4;

		public Aircraft Aircraft;

		[Tooltip("now many snapshots to send inputs with (4 = send it every 4th snapshot)")]
		public int SendInputsInterval = 4;

		private int inputIntervalCounter;

		private const int CLIENT_SNAPSHOTS_RESENDS = 4;

		private readonly (int sendsRemaining, NetworkSnapshot snapshot)[] clientSnapshots = new(int, NetworkSnapshot)[4];

		private int clientSnapshotsNextIndex;

		private Vector3 smoothingVel;

		private Vector3 rotationSmoothingVel;

		private ExponentialMovingAverage clientAuthOffset = new ExponentialMovingAverage(20);

		private ExponentialMovingAverage clientRttEMA = new ExponentialMovingAverage(20);

		private float[] clientRTTSMA = new float[20];

		private int clientRTTSMAIndex;

		private int clientRTTSMAIndexMax;

		private SmoothNetworkTime clientAuthTimer = new SmoothNetworkTime();

		private double lastClientLocalTime;

		private double lastReceivedClientTime;

		private double lastServerLocalTime;

		private double lastClientSnapshotTime;

		private ClientAuthChecks_Simple clientAuthSimple;

		private int rejectResetCount;

		private readonly SnapshotTelemetry[] telemetryBuffer = new SnapshotTelemetry[20];

		private int telemetryBufferIndex;

		private int telemetryCount;

		private CancellationTokenSource cancelClientAuthUpdate;

		private readonly Queue<(NetworkSnapshot snapshot, double clientLocalTime, double clientServerTime)> _clientAuthQueue = new Queue<(NetworkSnapshot, double, double)>(4);

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 0;

		[NonSerialized]
		private const int RPC_COUNT = 1;

		public override float SyncInterval => 0.05f;

		public bool UseClientValue
		{
			get
			{
				if (base.Owner != null)
				{
					return !base.Owner.IsHost;
				}
				return false;
			}
		}

		protected virtual void Awake()
		{
			base.Identity.OnStartClient.AddListener(OnStartClient);
		}

		private void OnDestroy()
		{
			cancelClientAuthUpdate?.Cancel();
		}

		private void OnStartClient()
		{
			if (base.HasAuthority && !base.IsServer)
			{
				cancelClientAuthUpdate = new CancellationTokenSource();
				this.StartSlowUpdateDelayed(SyncInterval, ClientAuthUpdate, cancelClientAuthUpdate.Token);
			}
		}

		public override void Setup(SendTransformBatcher sendBatcher)
		{
			base.Setup(sendBatcher);
			GlobalPosition startPosition = Aircraft.startPosition;
			_ = Aircraft.startingVelocity;
			_ = sendBatcher.LocalSnapshotTime;
			clientAuthSimple = new ClientAuthChecks_Simple(this, startPosition, 0.0);
		}

		private void FixedUpdate()
		{
			if (base.IsServer)
			{
				ProcessNextClientAuth();
			}
			if (ClientAuthChecks_Simple.ClientChecksEnabled && !(Aircraft == null) && !(Aircraft.rb == null) && base.HasAuthority && !(NetworkSceneSingleton<LevelInfo>.i == null))
			{
				GlobalPosition globalPosition = Aircraft.rb.transform.GlobalPosition();
				if (clientAuthSimple == null)
				{
					double initialTimestamp = ((base.SendBatcher != null) ? base.SendBatcher.LocalSnapshotTime : Time.timeAsDouble);
					clientAuthSimple = new ClientAuthChecks_Simple(this, globalPosition, initialTimestamp);
				}
				clientAuthSimple.TestUnderTerrain(globalPosition, out var _);
			}
		}

		private void ClientAuthUpdate()
		{
			if (!base.HasAuthority)
			{
				cancelClientAuthUpdate.Cancel();
				cancelClientAuthUpdate = null;
				return;
			}
			NetworkSnapshot snapshot = CreateSnapshot();
			double localSnapshotTime = base.SendBatcher.LocalSnapshotTime;
			double serverSmoothTime = base.SendBatcher.ServerSmoothTime;
			if (ValidateCmdClientAuth(snapshot, localSnapshotTime, serverSmoothTime, logErrors: true))
			{
				CmdClientAuth(snapshot, localSnapshotTime, serverSmoothTime);
			}
			else
			{
				Debug.LogError("Not sending CmdClientAuth because values were invalid");
			}
		}

		private static bool ValidateCmdClientAuth(NetworkSnapshot snapshot, double clientLocalTime, double clientServerTime, bool logErrors)
		{
			return (byte)(1u & (NetworkFloatHelper.Validate(clientLocalTime, logErrors, "clientLocalTime") ? 1u : 0u) & (NetworkFloatHelper.Validate(clientServerTime, logErrors, "clientServerTime") ? 1u : 0u) & (snapshot.Valid(logErrors) ? 1u : 0u)) != 0;
		}

		[RateLimit(Refill = 25, MaxTokens = 100, Penalty = 1)]
		[ServerRpc(channel = Channel.Unreliable)]
		private void CmdClientAuth(NetworkSnapshot snapshot, double clientLocalTime, double clientServerTime)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				UserCode_CmdClientAuth_65459396(snapshot, clientLocalTime, clientServerTime);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			//GeneratedNetworkCode._Write_NuclearOption_002ENetworkTransforms_002ENetworkTransformBase_002FNetworkSnapshot(writer, snapshot);
			writer.WriteDoubleConverter(clientLocalTime);
			writer.WriteDoubleConverter(clientServerTime);
			ServerRpcSender.Send(this, 0, writer, Channel.Unreliable, requireAuthority: true);
			writer.Release();
		}

		private void ProcessNextClientAuth()
		{
			if (!_clientAuthQueue.TryDequeue(out (NetworkSnapshot, double, double) result))
			{
				return;
			}
			if (base.Owner == null)
			{
				_clientAuthQueue.Clear();
				return;
			}
			var (snapshot, num, num2) = result;
			if (snapshot.globalPos.y < -10f)
			{
				if (base.Identity.Owner != null)
				{
					ColorLog<AircraftNetworkTransform>.Info($"Revoking client authority for underwater aircraft {this} at {snapshot.globalPos}");
					base.Identity.RemoveClientAuthority();
				}
				_clientAuthQueue.Clear();
				return;
			}
			double localSnapshotTime = base.SendBatcher.LocalSnapshotTime;
			double serverSmoothTime = base.SendBatcher.ServerSmoothTime;
			_ = lastClientLocalTime;
			_ = lastServerLocalTime;
			lastClientLocalTime = num;
			lastServerLocalTime = localSnapshotTime;
			float num3 = (float)(serverSmoothTime - num2);
			clientRttEMA.Add(num3);
			clientRTTSMA[clientRTTSMAIndex] = num3;
			clientRTTSMAIndex = (clientRTTSMAIndex + 1) % clientRTTSMA.Length;
			clientRTTSMAIndexMax = Mathf.Max(clientRTTSMAIndexMax, clientRTTSMAIndex);
			float num4 = (float)clientRttEMA.Value;
			double newValue = localSnapshotTime - num;
			clientAuthOffset.Add(newValue);
			double value = clientAuthOffset.Value;
			double num5 = num + value;
			float value2 = num4 / 2f;
			if (num5 < lastClientSnapshotTime + 0.014999999664723873)
			{
				num5 = lastClientSnapshotTime + (double)Mathf.Max(0.5f * SyncInterval, 0.015f);
			}
			snapshot.timestamp = num5;
			lastClientSnapshotTime = num5;
			value2 = Mathf.Clamp(value2, 0f, base.SendBatcher.MaxExtrapolation);
			snapshot.extraExtrapolation = value2;
			RejectMask rejectMask;
			int errorCost;
			float instantSpeed;
			float averageSpeed;
			bool num6 = clientAuthSimple.Run(ref snapshot, num, out rejectMask, out errorCost, out instantSpeed, out averageSpeed);
			SnapshotTelemetry snapshotTelemetry = new SnapshotTelemetry
			{
				globalPos = snapshot.globalPos,
				clientLocalTime = num,
				serverLocalTime = localSnapshotTime,
				rawRTT = num3,
				instantSpeed = instantSpeed,
				averageSpeed = averageSpeed,
				rejectMask = rejectMask
			};
			telemetryBuffer[telemetryBufferIndex] = snapshotTelemetry;
			telemetryBufferIndex = (telemetryBufferIndex + 1) % telemetryBuffer.Length;
			if (telemetryCount < telemetryBuffer.Length)
			{
				telemetryCount++;
			}
			base.SendBatcher.clientAuthDebugStream?.Log(this, rejectMask, snapshot, num, num2);
			if (rejectMask == RejectMask.Accepted)
			{
				rejectResetCount = 0;
			}
			else
			{
				rejectResetCount++;
			}
			if (rejectResetCount != 0 && rejectResetCount % 10 == 0)
			{
				ColorLog<AircraftNetworkTransform>.Info($"{base.Owner} Invalid snapshot {rejectResetCount}");
			}
			if (num6)
			{
				clientSnapshots[clientSnapshotsNextIndex] = (sendsRemaining: 4, snapshot: snapshot);
				clientSnapshotsNextIndex = (clientSnapshotsNextIndex + 1) % clientSnapshots.Length;
				ForceSync();
				if (!base.Owner.IsHost)
				{
					QueueNewSnapshot(snapshot.timestamp.Value, snapshot, isLast: true);
				}
			}
			else
			{
				LogClientAuthRejection(rejectMask, snapshot.globalPos, instantSpeed, averageSpeed, value);
				base.Owner.SetError(errorCost, NuclearOptionPlayerErrorFlags.InvalidTransformSnapshot);
			}
		}

		private void LogClientAuthRejection(RejectMask rejectMask, GlobalPosition position, float instantSpeed, float averageSpeed, double localOffset)
		{
			float num = float.MaxValue;
			float num2 = float.MinValue;
			float num3 = 0f;
			float num4 = float.MaxValue;
			float num5 = float.MinValue;
			float num6 = 0f;
			float num7 = float.MaxValue;
			float num8 = float.MinValue;
			float num9 = 0f;
			int num10 = 0;
			int num11 = ((telemetryCount >= telemetryBuffer.Length) ? telemetryBufferIndex : 0);
			for (int i = 0; i < telemetryCount; i++)
			{
				int num12 = (num11 + i) % telemetryBuffer.Length;
				SnapshotTelemetry snapshotTelemetry = telemetryBuffer[num12];
				if (i > 0)
				{
					int num13 = (num11 + i - 1) % telemetryBuffer.Length;
					SnapshotTelemetry snapshotTelemetry2 = telemetryBuffer[num13];
					float num14 = (float)(snapshotTelemetry.clientLocalTime - snapshotTelemetry2.clientLocalTime);
					float num15 = (float)(snapshotTelemetry.serverLocalTime - snapshotTelemetry2.serverLocalTime);
					if (num14 < num)
					{
						num = num14;
					}
					if (num14 > num2)
					{
						num2 = num14;
					}
					num3 += num14;
					if (num15 < num4)
					{
						num4 = num15;
					}
					if (num15 > num5)
					{
						num5 = num15;
					}
					num6 += num15;
					num10++;
				}
				if (snapshotTelemetry.rawRTT < num7)
				{
					num7 = snapshotTelemetry.rawRTT;
				}
				if (snapshotTelemetry.rawRTT > num8)
				{
					num8 = snapshotTelemetry.rawRTT;
				}
				num9 += snapshotTelemetry.rawRTT;
			}
			if (num10 == 0)
			{
				num = (num2 = (num3 = 0f));
				num4 = (num5 = (num6 = 0f));
			}
			float num16 = ((num10 > 0) ? (num3 / (float)num10) : 0f);
			float num17 = ((num10 > 0) ? (num6 / (float)num10) : 0f);
			float num18 = ((telemetryCount > 0) ? (num9 / (float)telemetryCount) : 0f);
			ColorLog<AircraftNetworkTransform>.InfoWarn($"ClientAuth REJECT [{rejectMask}] Owner:{base.Owner} (Aircraft:{Aircraft}) | " + $"GlobalPosition:{position} | " + $"InstantSpeed:{instantSpeed:F1} m/s, AvgSpeed:{averageSpeed:F1} m/s | " + $"dtClient (min/max/avg): {num:F3}/{num2:F3}/{num16:F3}s | " + $"dtServer (min/max/avg): {num4:F3}/{num5:F3}/{num17:F3}s | " + $"rawRTT (min/max/avg): {num7 * 1000f:F1}/{num8 * 1000f:F1}/{num18 * 1000f:F1}ms | " + $"Offset:{localOffset:F3}s");
		}

		public void DumpTelemetryHistory()
		{
			if (telemetryCount == 0)
			{
				ColorLog<AircraftNetworkTransform>.InfoWarn($"DumpTelemetryHistory for {base.Owner} (Aircraft:{Aircraft}): Buffer is empty.");
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"Telemetry History Dump for {base.Owner} (Aircraft:{Aircraft})");
			int num = ((telemetryCount >= telemetryBuffer.Length) ? telemetryBufferIndex : 0);
			for (int i = 0; i < telemetryCount; i++)
			{
				int num2 = (num + i) % telemetryBuffer.Length;
				SnapshotTelemetry snapshotTelemetry = telemetryBuffer[num2];
				stringBuilder.AppendLine($"[{i}] Pos:{snapshotTelemetry.globalPos}, ClientTime:{snapshotTelemetry.clientLocalTime:F3}s, ServerTime:{snapshotTelemetry.serverLocalTime:F3}s, rawRTT:{snapshotTelemetry.rawRTT * 1000f:F1}ms, InstantSpeed:{snapshotTelemetry.instantSpeed:F1}m/s, AvgSpeed:{snapshotTelemetry.averageSpeed:F1}m/s, RejectMask:{snapshotTelemetry.rejectMask}");
			}
			ColorLog<AircraftNetworkTransform>.InfoWarn(stringBuilder.ToString());
		}

		public override bool ShouldSend()
		{
			if (UseClientValue)
			{
				for (int i = 0; i < clientSnapshots.Length; i++)
				{
					if (clientSnapshots[i].sendsRemaining > 0)
					{
						return true;
					}
				}
				return false;
			}
			return !Aircraft.rb.isKinematic;
		}

		public override void Write(NetworkWriter writer)
		{
			if (UseClientValue)
			{
				int num = 0;
				for (int i = 0; i < clientSnapshots.Length; i++)
				{
					if (clientSnapshots[i].sendsRemaining > 0)
					{
						num++;
					}
				}
				writer.Write((ulong)(num - 1), 2);
				for (int j = 0; j < clientSnapshots.Length; j++)
				{
					int num2 = (j + clientSnapshotsNextIndex) % clientSnapshots.Length;
					ref(int, NetworkSnapshot) reference = ref clientSnapshots[num2];
					if (reference.Item1 > 0)
					{
						reference.Item1--;
						writer.Write(reference.Item2);
					}
				}
			}
			else
			{
				writer.Write(0uL, 2);
				NetworkSnapshot value = CreateSnapshot();
				writer.Write(value);
			}
		}

		private NetworkSnapshot CreateSnapshot()
		{
			CompressedInputs? clientInputs = null;
			inputIntervalCounter--;
			if (inputIntervalCounter <= 0)
			{
				inputIntervalCounter = SendInputsInterval;
				clientInputs = new CompressedInputs(Aircraft.GetInputs());
			}
			Rigidbody rb = Aircraft.rb;
			Transform transform = rb.transform;
			return new NetworkSnapshot(clientInputs, transform.GlobalPosition(), transform.rotation, rb.velocity);
		}

		public override void Receive(double timestamp, NetworkReader reader)
		{
			int num = (int)(reader.Read(2) + 1);
			for (int i = 0; i < num; i++)
			{
				NetworkSnapshot fullSnapshot = reader.Read<NetworkSnapshot>();
				if (fullSnapshot.Valid(logErrors: true))
				{
					QueueNewSnapshot(timestamp, fullSnapshot, i == num - 1);
				}
				else
				{
					Debug.LogError("Ignoring invalid Aircraft snapshot from server");
				}
			}
		}

		private void QueueNewSnapshot(double timestamp, NetworkSnapshot fullSnapshot, bool isLast)
		{
			if (!Aircraft.LocalSim)
			{
				if (isLast && fullSnapshot.ClientInputs.HasValue)
				{
					Aircraft.ApplySetInputs(fullSnapshot.ClientInputs.Value);
				}
				double timestamp2 = fullSnapshot.timestamp ?? timestamp;
				SnapshotBuffer.Insert(timestamp2, fullSnapshot.ToLocal(), ignoreWarning: true);
			}
		}

		public override void VisualUpdate(ref VisualUpdateTime visualTime)
		{
			using (visualUpdateMarker.Auto())
			{
				if (!Aircraft.LocalSim && TryGetSnapshot(ref visualTime, out var snapshot))
				{
					ApplySnapshot(snapshot);
					Aircraft.CheckSpawnedInPosition();
				}
			}
		}

		private void ApplySnapshot(ViewSnapshot snapshot)
		{
			using (applySnapshotMarker.Auto())
			{
				Vector3 position = snapshot.Position;
				Vector3 velocity = snapshot.Velocity;
				Quaternion rotation = snapshot.Rotation;
				Vector3 position2 = base.transform.position;
				if (debugActive)
				{
					debugGui.CalculateInfluence(snapshot, position2);
				}
				Rigidbody rb = Aircraft.rb;
				if (base.SendBatcher.IsCloseToCamera(position))
				{
					base.transform.SetPositionAndRotation(position, rotation);
				}
				rb.velocity = velocity;
				rb.angularVelocity = Vector3.zero;
				rb.Move(position, rotation);
			}
		}

		private void MirageProcessed()
		{
		}

		private void UserCode_CmdClientAuth_65459396(NetworkSnapshot snapshot, double clientLocalTime, double clientServerTime)
		{
			if (clientLocalTime - lastReceivedClientTime < 0.014999999664723873)
			{
				return;
			}
			if (!ValidateCmdClientAuth(snapshot, clientLocalTime, clientServerTime, logErrors: false))
			{
				base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
				return;
			}
			lastReceivedClientTime = clientLocalTime;
			while (_clientAuthQueue.Count >= 4)
			{
				_clientAuthQueue.Dequeue();
			}
			_clientAuthQueue.Enqueue((snapshot, clientLocalTime, clientServerTime));
		}

		protected static void Skeleton_CmdClientAuth_65459396(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			//((AircraftNetworkTransform)behaviour).UserCode_CmdClientAuth_65459396(GeneratedNetworkCode._Read_NuclearOption_002ENetworkTransforms_002ENetworkTransformBase_002FNetworkSnapshot(reader), reader.ReadDoubleConverter(), reader.ReadDoubleConverter());
		}

		protected override int GetRpcCount()
		{
			return 1;
		}

		protected override void RegisterRpc(RemoteCallCollection collection)
		{
			base.RegisterRpc(collection);
			collection.Register(0, "NuclearOption.NetworkTransforms.AircraftNetworkTransform.CmdClientAuth", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdClientAuth_65459396, RpcRateLimitConfig.Enabled(1f, 25, 100, 1));
		}
	}
}
