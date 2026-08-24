using System;
using Mirage.Serialization;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class ShipNetworkTransform : NetworkTransformBase
	{
		private static readonly ProfilerMarker visualUpdateMarker = new ProfilerMarker("GroundVehicle.VisualUpdate");

		public Ship Ship;

		public NetworkPIDSmoother networkSmoother;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 0;

		[NonSerialized]
		private const int RPC_COUNT = 0;

		public override float SyncInterval => 0.15f;

		private void Awake()
		{
			Ship.onInitialize += Ship_onInitialize;
		}

		private void Ship_onInitialize()
		{
			if (!base.IsServer)
			{
				networkSmoother.Initialize(Ship.rb);
			}
		}

		public override bool ShouldSend()
		{
			return !Ship.rb.isKinematic;
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
			if (NetworkFloatHelper.Validate(globalPosition, logErrors: true, "Ship.Position") && NetworkFloatHelper.Validate(value, logErrors: true, "Ship.Rotation"))
			{
				SnapshotBuffer.Insert(timestamp, new LocalSnapshot
				{
					globalPos = globalPosition,
					rotation = value
				});
			}
			else
			{
				Debug.LogError("Ignoring invalid Ship snapshot from server");
			}
		}

		public override void VisualUpdate(ref VisualUpdateTime visualTime)
		{
			using (visualUpdateMarker.Auto())
			{
				Rigidbody rb = Ship.rb;
				if (!rb.isKinematic && TryGetSnapshot(ref visualTime, out var snapshot))
				{
					networkSmoother.SmoothRB(rb, snapshot);
				}
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
