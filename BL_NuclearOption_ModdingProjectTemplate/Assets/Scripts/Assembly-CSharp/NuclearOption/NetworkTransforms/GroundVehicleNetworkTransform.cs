using System;
using Mirage.Serialization;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class GroundVehicleNetworkTransform : NetworkTransformBase
	{
		private static readonly ProfilerMarker applySnapshotMarker = new ProfilerMarker("ApplySnapshot");

		private static readonly ProfilerMarker visualUpdateMarker = new ProfilerMarker("GroundVehicle.VisualUpdate");

		public GroundVehicle GroundVehicle;

		private Vector3 smoothingVel;

		private Vector3 rotationSmoothingVel;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 0;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		public override float SyncInterval => 0.15f;

		public override bool ShouldSend()
		{
			return !GroundVehicle.rb.isKinematic;
		}

		public override void Write(NetworkWriter writer)
		{
			base.transform.GetPositionAndRotation(out var position, out var rotation);
			writer.Write(position.ToGlobalPosition());
			writer.Write(rotation);
		}

		public override void Receive(double timestamp, NetworkReader reader)
		{
			GlobalPosition globalPosition = reader.Read<GlobalPosition>();
			Quaternion value = reader.Read<Quaternion>();
			if (NetworkFloatHelper.Validate(globalPosition, logErrors: true, "GroundVehicle.Position") && NetworkFloatHelper.Validate(value, logErrors: true, "GroundVehicle.Rotation"))
			{
				SnapshotBuffer.Insert(timestamp, new LocalSnapshot
				{
					globalPos = globalPosition,
					rotation = value
				});
			}
			else
			{
				Debug.LogError("Ignoring invalid GroundVehicle snapshot from server");
			}
		}

		public override void VisualUpdate(ref VisualUpdateTime visualTime)
		{
			using (visualUpdateMarker.Auto())
			{
				if (!GroundVehicle.rb.isKinematic && !GroundVehicle.LocalSim && TryGetSnapshot(ref visualTime, out var snapshot))
				{
					ApplySnapshot(snapshot);
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
				Rigidbody rb = GroundVehicle.rb;
				Vector3 position2 = base.transform.position;
				if (debugActive)
				{
					debugGui.CalculateInfluence(snapshot, position2);
				}
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

		protected override int GetRpcCount()
		{
			return 0;
		}
	}
}
