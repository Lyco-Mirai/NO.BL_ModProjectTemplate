using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class ClientAuthChecks
	{
		public enum CheckMode
		{
			None = 0,
			LogOnly = 1,
			Ignore = 2
		}

		private static readonly ProfilerMarker validateV1Marker = new ProfilerMarker("ValidateInternal_v1");

		private GlobalPosition previousPosition;

		private Quaternion previousRotation;

		private Vector3 previousVelocity;

		private double previousTimestamp;

		private readonly float joinTime;

		public int TotalCount { get; private set; }

		public int RejectedCount { get; private set; }

		public static CheckMode Mode { get; private set; }

		public ClientAuthChecks(GlobalPosition position, Vector3 velocity, double initialTimestamp)
		{
			previousPosition = position;
			previousVelocity = velocity;
			previousTimestamp = initialTimestamp;
			joinTime = Time.timeSinceLevelLoad;
		}

		public bool Run(ref NetworkTransformBase.NetworkSnapshot snapshot, out RejectMask rejectMask)
		{
			switch (Mode)
			{
			case CheckMode.LogOnly:
				RunInner(ref snapshot, out rejectMask);
				return true;
			case CheckMode.Ignore:
				return RunInner(ref snapshot, out rejectMask);
			default:
				rejectMask = RejectMask.Accepted;
				return true;
			}
		}

		private bool RunInner(ref NetworkTransformBase.NetworkSnapshot snapshot, out RejectMask rejectMask)
		{
			rejectMask = RejectMask.Accepted;
			RejectMask rejectMask2;
			bool num = ValidateInternal_v1(ref snapshot, out rejectMask2);
			rejectMask |= rejectMask2;
			TotalCount++;
			if (!num)
			{
				RejectedCount++;
			}
			return num;
		}

		private bool ValidateInternal_v1(ref NetworkTransformBase.NetworkSnapshot snapshot, out RejectMask rejectMask)
		{
			using (validateV1Marker.Auto())
			{
				double value = snapshot.timestamp.Value;
				float num = (float)(value - previousTimestamp);
				Vector3 vector = snapshot.velocity.Decompress();
				Vector3 vector2 = (vector - previousVelocity) / num;
				float sqrMagnitude = vector2.sqrMagnitude;
				if (((snapshot.globalPos - previousPosition) / num).sqrMagnitude > 2500f && sqrMagnitude > 3600f)
				{
					float num2 = Vector3.Dot(previousVelocity.normalized, vector2.normalized);
					if ((double)num2 > 0.3)
					{
						if (sqrMagnitude > 10000f)
						{
							rejectMask = RejectMask.AccelerationForward;
							return false;
						}
					}
					else if ((double)num2 > -0.6)
					{
						if (sqrMagnitude > 3600f)
						{
							rejectMask = RejectMask.AccelerationPerpendicular;
							return false;
						}
					}
					else
					{
						float num3 = (Mathf.Sqrt(previousVelocity.sqrMagnitude) + 10f) / num;
						if (sqrMagnitude > num3 * num3)
						{
							rejectMask = RejectMask.AccelerationBackwards;
							return false;
						}
					}
				}
				GlobalPosition globalPos = snapshot.globalPos;
				Vector3 vector3 = (previousVelocity + vector) / 2f;
				GlobalPosition b = previousPosition + vector3 * num;
				float range = vector3.magnitude * num + 10f;
				if (FastMath.OutOfRange(globalPos, b, range))
				{
					rejectMask = RejectMask.Position;
					return false;
				}
				previousPosition = globalPos;
				previousVelocity = vector;
				previousTimestamp = value;
				previousRotation = snapshot.rotation;
				rejectMask = RejectMask.Accepted;
				return true;
			}
		}

		public NetworkTransformBase.NetworkSnapshot CreateServerSnapshot()
		{
			return new NetworkTransformBase.NetworkSnapshot
			{
				globalPos = previousPosition,
				rotation = previousRotation,
				velocity = previousVelocity.Compress()
			};
		}

		public static void SetRunChecks(CheckMode? mode)
		{
			Mode = mode.GetValueOrDefault();
		}
	}
}
