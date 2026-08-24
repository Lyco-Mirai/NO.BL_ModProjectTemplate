using System;
using Mirage.Serialization;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class PilotDismountedNetworkTransform : NetworkTransformBase
	{
		public PilotDismounted Pilot;

		private Vector3 smoothingVel;

		private Vector3 rotationSmoothingVel;

		private bool spawnLerpTimeStarted;

		private float spawnLerpTime;

		private const float SPAWN_SMOOTH_TIME = 12f;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 0;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		public override float SyncInterval => 0.1f;

		public override bool ShouldSend()
		{
			return !Pilot.rb.isKinematic;
		}

		public override void Write(NetworkWriter writer)
		{
			Rigidbody rb = Pilot.rb;
			Vector3 position = rb.position;
			Quaternion rotation = rb.rotation;
			Vector3 velocity = rb.velocity;
			writer.Write(position.ToGlobalPosition());
			writer.Write(rotation);
			writer.Write(velocity.Compress());
		}

		public override void Receive(double timestamp, NetworkReader reader)
		{
			GlobalPosition globalPosition = reader.Read<GlobalPosition>();
			Quaternion value = reader.Read<Quaternion>();
			Vector3Compressed value2 = reader.Read<Vector3Compressed>();
			if (NetworkFloatHelper.Validate(globalPosition, logErrors: true, "Pilot.Position") && NetworkFloatHelper.Validate(value, logErrors: true, "Pilot.Rotation") && NetworkFloatHelper.Validate(value2, logErrors: true, "Pilot.Velocity"))
			{
				SnapshotBuffer.Insert(timestamp, new LocalSnapshot
				{
					globalPos = globalPosition,
					rotation = value,
					velocity = value2.Decompress()
				});
			}
			else
			{
				Debug.LogError("Ignoring invalid Pilot snapshot from server");
			}
		}

		public override void VisualUpdate(ref VisualUpdateTime visualTime)
		{
			if (!Pilot.rb.isKinematic && !Pilot.LocalSim && !Pilot.IsOnEjectionRail && TryGetSnapshot(ref visualTime, out var snapshot))
			{
				ApplySnapshot(snapshot);
			}
		}

		private void ApplySnapshot(ViewSnapshot snapshot)
		{
			if (!spawnLerpTimeStarted)
			{
				spawnLerpTimeStarted = true;
				if (Pilot.cockpitPart != null)
				{
					spawnLerpTime = Pilot.timeSinceSpawn;
				}
				else
				{
					spawnLerpTime = -12f;
				}
			}
			Vector3 vector = snapshot.Position;
			Quaternion rotation = snapshot.Rotation;
			Vector3 vector2 = snapshot.Velocity;
			Rigidbody rb = Pilot.rb;
			float num = Pilot.timeSinceSpawn - spawnLerpTime;
			if (num < 12f && Pilot.cockpitPart != null)
			{
				vector = Vector3.Lerp(rb.position, vector, num / 12f);
				vector2 = Vector3.Lerp(rb.velocity, vector2, num / 12f);
			}
			if (base.SendBatcher.IsCloseToCamera(vector))
			{
				base.transform.SetPositionAndRotation(vector, rotation);
			}
			rb.velocity = vector2;
			rb.angularVelocity = Vector3.zero;
			rb.Move(vector, rotation);
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
