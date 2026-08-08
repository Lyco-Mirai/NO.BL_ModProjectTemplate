using System;
using Mirage;
using Mirage.Serialization;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public abstract class NetworkTransformBase : NetworkBehaviour
	{
		[NetworkMessage]
		public struct NetworkSnapshot
		{
			public CompressedInputs? ClientInputs;

			public double? timestamp;

			public float? extraExtrapolation;

			public GlobalPosition globalPos;

			public Vector3Compressed velocity;

			[QuaternionPack(12)]
			public Quaternion rotation;

			internal static QuaternionPacker rotation__Packer;

			public NetworkSnapshot(CompressedInputs? clientInputs, GlobalPosition globalPos, Quaternion rotation, Vector3 velocity)
			{
				ClientInputs = clientInputs;
				timestamp = null;
				extraExtrapolation = null;
				this.globalPos = globalPos;
				this.velocity = velocity.Compress();
				this.rotation = rotation;
			}

			public LocalSnapshot ToLocal()
			{
				return new LocalSnapshot
				{
					globalPos = globalPos,
					rotation = rotation,
					velocity = velocity.Decompress(),
					extraExtrapolation = extraExtrapolation.GetValueOrDefault()
				};
			}

			public bool Valid(bool logErrors)
			{
				bool flag = true;
				if (ClientInputs.HasValue)
				{
					flag &= ClientInputs.Value.Valid(logErrors);
				}
				if (timestamp.HasValue)
				{
					flag &= NetworkFloatHelper.Validate(timestamp.Value, logErrors, "timestamp");
				}
				if (extraExtrapolation.HasValue)
				{
					flag &= NetworkFloatHelper.Validate(extraExtrapolation.Value, logErrors, "extraExtrapolation");
				}
				flag &= NetworkFloatHelper.Validate(globalPos, logErrors, "globalPos");
				flag &= NetworkFloatHelper.Validate(velocity, logErrors, "velocity");
				return flag & NetworkFloatHelper.Validate(rotation, logErrors, "rotation");
			}

			static NetworkSnapshot()
			{
				rotation__Packer = new QuaternionPacker(12);
			}
		}

		public struct LocalSnapshot
		{
			public GlobalPosition globalPos;

			public float extraExtrapolation;

			public Vector3? velocity;

			public Quaternion? rotation;
		}

		public class SnapshotBufferLocalSnapshot : SnapshotBuffer<LocalSnapshot>
		{
			public ViewSnapshot GetSnapshotForTime(double snapshotTime)
			{
				SendTransformBatcherDebugger.Gui debugGui = default(SendTransformBatcherDebugger.Gui);
				return GetSnapshotForTime(snapshotTime, debugActive: false, ref debugGui);
			}

			public ViewSnapshot GetSnapshotForTime(double snapshotTime, bool debugActive, ref SendTransformBatcherDebugger.Gui debugGui)
			{
				for (int num = Count - 1; num >= 0; num--)
				{
					if (snapshotTime > Get(num).Timestamp)
					{
						if (num == Count - 1)
						{
							if (num > 0)
							{
								if (debugActive)
								{
									debugGui.snapType = $"After Last ({Count})";
								}
								return AfterCurrent(num);
							}
							if (debugActive)
							{
								debugGui.snapType = $"After Last ({Count})";
							}
							return FromOne(num);
						}
						if (debugActive)
						{
							debugGui.snapType = $"Between {num - 1}->{num} ({Count})";
						}
						return Between(num + 1, snapshotTime);
					}
				}
				if (debugActive)
				{
					debugGui.snapType = $"before first ({Count})";
				}
				return FromOne(0);
			}

			internal ViewSnapshot FromOne(int currentIndex)
			{
				TimedSnapshot timedSnapshot = buffer[currentIndex];
				return new ViewSnapshot
				{
					ExtraExtrapolation = timedSnapshot.Snapshot.extraExtrapolation,
					Timestamp = timedSnapshot.Timestamp,
					Position = timedSnapshot.Snapshot.globalPos.ToLocalPosition(),
					Rotation = timedSnapshot.Snapshot.rotation.GetValueOrDefault(),
					Velocity = (timedSnapshot.Snapshot.velocity ?? Vector3.zero),
					Acceleration = Vector3.zero
				};
			}

			internal ViewSnapshot Between(int currentIndex, double snapshotTime)
			{
				TimedSnapshot timedSnapshot = buffer[currentIndex - 1];
				TimedSnapshot timedSnapshot2 = buffer[currentIndex];
				float num = FastMath.InverseLerp(timedSnapshot.Timestamp, timedSnapshot2.Timestamp, snapshotTime);
				float extraExtrapolation = Mathf.Lerp(timedSnapshot.Snapshot.extraExtrapolation, timedSnapshot2.Snapshot.extraExtrapolation, num);
				Vector3 position = FastMath.LerpUnclamped(timedSnapshot.Snapshot.globalPos, timedSnapshot2.Snapshot.globalPos, num).ToLocalPosition();
				ViewSnapshot result = new ViewSnapshot
				{
					ExtraExtrapolation = extraExtrapolation,
					Timestamp = snapshotTime,
					Position = position
				};
				if (timedSnapshot2.Snapshot.rotation.HasValue)
				{
					result.Rotation = Quaternion.SlerpUnclamped(timedSnapshot.Snapshot.rotation.Value, timedSnapshot2.Snapshot.rotation.Value, num);
				}
				if (timedSnapshot2.Snapshot.velocity.HasValue)
				{
					result.Velocity = Vector3.LerpUnclamped(timedSnapshot.Snapshot.velocity.Value, timedSnapshot2.Snapshot.velocity.Value, num);
					Vector3 a = AccelerationOfMiddle(currentIndex - 1);
					Vector3 b = AccelerationOfMiddle(currentIndex);
					result.Acceleration = Vector3.LerpUnclamped(a, b, num);
				}
				else
				{
					Vector3 a2 = VelocityOfMiddle(currentIndex - 1);
					Vector3 b2 = VelocityOfMiddle(currentIndex);
					result.Velocity = Vector3.LerpUnclamped(a2, b2, Mathf.Sqrt(num));
					result.Acceleration = Vector3.zero;
				}
				return result;
			}

			internal ViewSnapshot AfterCurrent(int currentIndex)
			{
				TimedSnapshot timedSnapshot = buffer[currentIndex - 1];
				TimedSnapshot timedSnapshot2 = buffer[currentIndex];
				ViewSnapshot result = new ViewSnapshot
				{
					ExtraExtrapolation = timedSnapshot2.Snapshot.extraExtrapolation,
					Timestamp = timedSnapshot2.Timestamp,
					Position = timedSnapshot2.Snapshot.globalPos.ToLocalPosition(),
					Rotation = timedSnapshot2.Snapshot.rotation.GetValueOrDefault()
				};
				float num = (float)(timedSnapshot2.Timestamp - timedSnapshot.Timestamp);
				if (timedSnapshot2.Snapshot.velocity.HasValue)
				{
					result.Velocity = timedSnapshot2.Snapshot.velocity.Value;
					Vector3 vector = timedSnapshot2.Snapshot.velocity.Value - timedSnapshot.Snapshot.velocity.Value;
					result.Acceleration = vector / num;
				}
				else
				{
					Vector3 vector2 = timedSnapshot2.Snapshot.globalPos - timedSnapshot.Snapshot.globalPos;
					result.Velocity = vector2 / num / 2f;
					result.Acceleration = Vector3.zero;
				}
				return result;
			}

			private Vector3 VelocityOfMiddle(int middleIndex)
			{
				GetEitherSide(middleIndex, out var before, out var after);
				double timestamp = before.Timestamp;
				GlobalPosition globalPos = before.Snapshot.globalPos;
				double timestamp2 = after.Timestamp;
				Vector3 vector = after.Snapshot.globalPos - globalPos;
				double num = timestamp2 - timestamp;
				return vector / (float)num;
			}

			private Vector3 AccelerationOfMiddle(int middleIndex)
			{
				GetEitherSide(middleIndex, out var before, out var after);
				double timestamp = before.Timestamp;
				Vector3 vector = before.Snapshot.velocity ?? Vector3.zero;
				double timestamp2 = after.Timestamp;
				Vector3 vector2 = (after.Snapshot.velocity ?? Vector3.zero) - vector;
				double num = timestamp2 - timestamp;
				return new Vector3((float)((double)vector2.x / num), (float)((double)vector2.y / num), (float)((double)vector2.z / num));
			}

			private void GetEitherSide(int middleIndex, out TimedSnapshot before, out TimedSnapshot after)
			{
				int num = middleIndex - 1;
				if (num < 0)
				{
					num = middleIndex;
				}
				int num2 = middleIndex + 1;
				if (num2 >= base.Count)
				{
					num2 = middleIndex;
				}
				before = buffer[num];
				after = buffer[num2];
			}
		}

		public struct ViewSnapshot
		{
			public float ExtraExtrapolation;

			public double Timestamp;

			public Vector3 Position;

			public Quaternion Rotation;

			public Vector3 Velocity;

			public Vector3 Acceleration;

			public void Deconstruct(out double timestamp, out Vector3 pos, out Quaternion rot, out Vector3 vel, out Vector3 acc)
			{
				timestamp = Timestamp;
				pos = Position;
				rot = Rotation;
				vel = Velocity;
				acc = Acceleration;
			}

			public readonly float SnapshotAge(double extrapolationTime)
			{
				return (float)(extrapolationTime + (double)ExtraExtrapolation - Timestamp);
			}

			public ViewSnapshot Extrapolate(double extrapolationTime, float maxExtrapolateAge)
			{
				ViewSnapshot viewSnapshot = this;
				viewSnapshot.Deconstruct(out var _, out var pos, out var rot, out var vel, out var acc);
				Vector3 vector = pos;
				Quaternion rotation = rot;
				Vector3 vector2 = vel;
				Vector3 vector3 = acc;
				float num = SnapshotAge(extrapolationTime);
				if (num > maxExtrapolateAge)
				{
					num = maxExtrapolateAge;
				}
				Vector3 position = vector + vector2 * num + 0.5f * num * num * vector3;
				Vector3 velocity = vector2 + num * vector3;
				return new ViewSnapshot
				{
					Position = position,
					Rotation = rotation,
					Velocity = velocity
				};
			}
		}

		private static readonly ProfilerMarker getSnapshotForTimeMarker = new ProfilerMarker("GetSnapshotForTime");

		private static readonly ProfilerMarker extrapolateMarker = new ProfilerMarker("Extrapolate");

		[Tooltip("How much to multiply extrapolation offset by. 0 = no extrapolation, 1 = full extrapolation")]
		[Range(0f, 1f)]
		public float extrapolationFactor = 1f;

		private double nextUpdate;

		private bool forceSync;

		public readonly SnapshotBufferLocalSnapshot SnapshotBuffer = new SnapshotBufferLocalSnapshot();

		[NonSerialized]
		public bool debugActive;

		[NonSerialized]
		public SendTransformBatcherDebugger.Gui debugGui;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 0;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		public abstract float SyncInterval { get; }

		public SendTransformBatcher SendBatcher { get; private set; }

		public virtual void Setup(SendTransformBatcher sendBatcher)
		{
			SendBatcher = sendBatcher;
		}

		public void ForceSync()
		{
			forceSync = true;
		}

		public void ResetUpdateTime(double time)
		{
			nextUpdate = time + (double)SyncInterval;
		}

		public bool TimeToUpdate(double time)
		{
			if (forceSync || time > nextUpdate)
			{
				nextUpdate = time + (double)SyncInterval;
				forceSync = false;
				return true;
			}
			return false;
		}

		public abstract bool ShouldSend();

		public abstract void Write(NetworkWriter writer);

		public abstract void Receive(double timestamp, NetworkReader writer);

		public abstract void VisualUpdate(ref VisualUpdateTime visualTime);

		protected bool TryGetSnapshot(ref VisualUpdateTime visualTime, out ViewSnapshot snapshot)
		{
			if (SnapshotBuffer.Count < 1)
			{
				snapshot = default(ViewSnapshot);
				return false;
			}
			using (getSnapshotForTimeMarker.Auto())
			{
				float num = SyncInterval * 2.5f;
				double snapshotTime = visualTime.interpolationTime - (double)num;
				snapshot = SnapshotBuffer.GetSnapshotForTime(snapshotTime, debugActive, ref debugGui);
			}
			double extrapolationTime = visualTime.interpolationTime + visualTime.extrapolationOffset * (double)extrapolationFactor;
			if (debugActive)
			{
				float num2 = snapshot.SnapshotAge(extrapolationTime);
				debugGui.snapshot_velocity = num2 * snapshot.Velocity;
				debugGui.snapshot_acceleration = 0.5f * num2 * num2 * snapshot.Acceleration;
			}
			using (extrapolateMarker.Auto())
			{
				snapshot = snapshot.Extrapolate(extrapolationTime, visualTime.maxExtrapolateAge);
			}
			return true;
		}

		private void MirageProcessed()
		{
		}

		protected override int GetRpcCount()
		{
			return 0;
		}
	}
}
