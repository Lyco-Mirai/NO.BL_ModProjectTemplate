using System;
using Mirage.Serialization;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class MissileNetworkTransform : NetworkTransformBase
	{
		public Missile Missile;

		private Vector3 smoothingVel;

		private Vector3 rotationSmoothingVel;

		private bool spawnLerpTimeStarted;

		private float spawnLerpTime;

		private const float SPAWN_SMOOTH_TIME = 4f;

		private float smoothing = 0.05f;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 0;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		public override float SyncInterval => 0.05f;

		private void Awake()
		{
			smoothing = GetSmoothing();
		}

		private float GetSmoothing()
		{
			return 0.05f;
		}

		public override bool ShouldSend()
		{
			return true;
		}

		public override void Write(NetworkWriter writer)
		{
			Rigidbody rb = Missile.rb;
			Vector3 position = rb.position;
			Vector3 velocity = rb.velocity;
			writer.Write(position.ToGlobalPosition());
			writer.Write(velocity.Compress());
		}

		public override void Receive(double timestamp, NetworkReader reader)
		{
			GlobalPosition globalPosition = reader.Read<GlobalPosition>();
			Vector3Compressed value = reader.Read<Vector3Compressed>();
			if (NetworkFloatHelper.Validate(globalPosition, logErrors: true, "Missile.Position") && NetworkFloatHelper.Validate(value, logErrors: true, "Missile.Velocity"))
			{
				SnapshotBuffer.Insert(timestamp, new LocalSnapshot
				{
					globalPos = globalPosition,
					velocity = value.Decompress()
				});
			}
			else
			{
				Debug.LogError("Ignoring invalid Missile snapshot from server");
			}
		}

		public override void VisualUpdate(ref VisualUpdateTime visualTime)
		{
			if (!Missile.disabled && !Missile.rb.isKinematic && TryGetSnapshot(ref visualTime, out var snapshot))
			{
				ApplySnapshot(snapshot);
			}
		}

		private void ApplySnapshot(ViewSnapshot snapshot)
		{
			Vector3 vector = snapshot.Position;
			Vector3 vector2 = snapshot.Velocity;
			Quaternion rotation = FastMath.LookRotation(vector2);
			Rigidbody rb = Missile.rb;
			if (!spawnLerpTimeStarted)
			{
				spawnLerpTimeStarted = true;
				if (Missile.owner != null)
				{
					spawnLerpTime = Missile.timeSinceSpawn;
				}
				else
				{
					spawnLerpTime = -4f;
				}
			}
			float num = Missile.timeSinceSpawn - spawnLerpTime;
			if (num < 4f && Missile.owner != null)
			{
				vector = Vector3.Lerp(rb.position, vector, num / 4f);
				vector2 = Vector3.Lerp(rb.velocity, vector2, num / 4f);
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
