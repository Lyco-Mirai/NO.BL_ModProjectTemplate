using Mirage.Logging;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class ClientAuthChecks_Simple
	{
		public const int HISTORY_SIZE = 20;

		private const int BASE_ERROR_COST = 5;

		private const int UNDER_TERRAIN_ERROR_COST_PER_SECOND = 100;

		private const float MAX_SNAPSHOT_SPEED = 3000f;

		private const float MAX_AVERAGE_SPEED = 900f;

		private static readonly ProfilerMarker validateMarker = new ProfilerMarker("ClientAuthChecks_Simple.Run");

		private static readonly ILogger verboseLogs = LogFactory.GetLogger<ClientAuthChecks_Simple>();

		private static readonly RaycastHit[] linecastHitBuffer = new RaycastHit[16];

		private GlobalPosition previousPosition;

		private double previousTimestamp;

		private float currentSpeedSum;

		private int speedHistoryIndex;

		public readonly float[] speedHistory = new float[20];

		private readonly AircraftNetworkTransform owner;

		private readonly Vector2 mapExtentsXZ;

		public const bool CLIENT_CHECKS_ENABLED_DEFAULT = false;

		public int TotalCount { get; private set; }

		public int RejectedCount { get; private set; }

		public static bool Enabled { get; private set; } = true;

		public static bool ClientChecksEnabled { get; private set; } = false;

		public ClientAuthChecks_Simple(AircraftNetworkTransform owner, GlobalPosition initialPosition, double initialTimestamp)
		{
			this.owner = owner;
			previousPosition = initialPosition;
			previousTimestamp = initialTimestamp;
			speedHistoryIndex = 0;
			Vector2 mapSize = NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.MapSize;
			mapExtentsXZ = mapSize * 0.5f;
		}

		public bool Run(ref NetworkTransformBase.NetworkSnapshot snapshot, double clientLocalTime, out RejectMask rejectMask, out int errorCost, out float instantSpeed, out float averageSpeed)
		{
			using (validateMarker.Auto())
			{
				if (!Enabled)
				{
					rejectMask = RejectMask.Accepted;
					errorCost = 0;
					instantSpeed = 0f;
					averageSpeed = 0f;
					return true;
				}
				bool num = ValidateInternal(clientLocalTime, snapshot.globalPos, out rejectMask, out errorCost, out instantSpeed, out averageSpeed);
				TotalCount++;
				if (!num)
				{
					RejectedCount++;
				}
				return num;
			}
		}

		public bool ValidateInternal(double snapshotTimestamp, GlobalPosition snapshotPosition, out RejectMask rejectMask, out int errorCost, out float instantSpeed, out float averageSpeed)
		{
			float num = (float)(snapshotTimestamp - previousTimestamp);
			float num2 = FastMath.Distance(previousPosition, snapshotPosition);
			instantSpeed = num2 / num;
			float num3 = currentSpeedSum - speedHistory[speedHistoryIndex] + instantSpeed;
			averageSpeed = num3 / 20f;
			if (verboseLogs.LogEnabled())
			{
				verboseLogs.Log($"{owner.Owner} Instant: {instantSpeed:F1} m/s | Average: {averageSpeed:F1} m/s");
			}
			rejectMask = RejectMask.Accepted;
			errorCost = 0;
			TestSpeed(instantSpeed, 3000f, in previousPosition, in snapshotPosition, out var percentOfMax);
			int num4 = CalculateSpeedCost(percentOfMax);
			if (num4 > 0)
			{
				rejectMask |= RejectMask.SnapshotPosition;
				errorCost += num4;
			}
			TestSpeed(averageSpeed, 900f, in previousPosition, in snapshotPosition, out var percentOfMax2);
			int num5 = CalculateSpeedCost(percentOfMax2);
			if (num5 > 0)
			{
				rejectMask |= RejectMask.AveragePosition;
				errorCost += num5;
			}
			TestUnderTerrain(snapshotPosition, out var isUnderTerrain);
			if (isUnderTerrain)
			{
				errorCost += CalculateUnderTerrainCost(num);
				rejectMask |= RejectMask.UnderTerrain;
			}
			if (errorCost > 0)
			{
				return false;
			}
			previousPosition = snapshotPosition;
			previousTimestamp = snapshotTimestamp;
			currentSpeedSum = num3;
			speedHistory[speedHistoryIndex] = instantSpeed;
			speedHistoryIndex = (speedHistoryIndex + 1) % 20;
			rejectMask = RejectMask.Accepted;
			return true;
		}

		private static void TestSpeed(float speed, float max, in GlobalPosition previous, in GlobalPosition snapshot, out float percentOfMax)
		{
			if (speed <= max)
			{
				percentOfMax = 0f;
				return;
			}
			float num = 0f - FastMath.NormalizedDirection(previous, snapshot).y;
			if (num <= 0.5f)
			{
				percentOfMax = speed / max;
				return;
			}
			float num2 = num * 2f;
			float num3 = max * num2;
			percentOfMax = speed / num3;
		}

		public void TestUnderTerrain(GlobalPosition snapshotPosition, out bool isUnderTerrain)
		{
			if (snapshotPosition.y > 5000f)
			{
				isUnderTerrain = false;
				return;
			}
			if (Mathf.Abs(snapshotPosition.x) > mapExtentsXZ.x || Mathf.Abs(snapshotPosition.z) > mapExtentsXZ.y)
			{
				isUnderTerrain = false;
				return;
			}
			if (snapshotPosition.y < -50f)
			{
				isUnderTerrain = true;
				return;
			}
			Vector3 vector = snapshotPosition.ToLocalPosition();
			Vector3 end = new GlobalPosition(snapshotPosition.x, -51f, snapshotPosition.z).ToLocalPosition();
			RaycastHit hitInfo;
			bool flag = Physics.Linecast(vector, end, out hitInfo, PhysicsLayers.StaticsMask);
			Vector3 vector2 = default(Vector3);
			Collider collider = null;
			Vector3 vector3 = default(Vector3);
			if (flag)
			{
				isUnderTerrain = false;
				vector2 = hitInfo.point;
				collider = hitInfo.collider;
			}
			else
			{
				vector3 = new GlobalPosition(snapshotPosition.x, 5001f, snapshotPosition.z).ToLocalPosition();
				Vector3 down = Vector3.down;
				float maxDistance = vector3.y - vector.y;
				int num = Physics.RaycastNonAlloc(vector3, down, linecastHitBuffer, maxDistance, PhysicsLayers.StaticsMask.value);
				bool flag2 = false;
				for (int i = 0; i < num; i++)
				{
					RaycastHit raycastHit = linecastHitBuffer[i];
					if (raycastHit.collider.GetComponentInParent<IIgnoreTerrainCheck>() == null)
					{
						flag2 = true;
						vector2 = raycastHit.point;
						collider = raycastHit.collider;
						break;
					}
				}
				isUnderTerrain = flag2;
			}
			if (ClientChecksEnabled)
			{
				Color color = (flag ? Color.green : Color.yellow);
				Debug.DrawLine(vector, end, color, 0.05f);
				if (!flag)
				{
					Color color2 = (isUnderTerrain ? Color.red : Color.gray);
					Debug.DrawLine(vector3, vector, color2, 0.05f);
				}
				if (isUnderTerrain)
				{
					if (verboseLogs.ErrorEnabled())
					{
						verboseLogs.LogError(string.Format("{0} {1} {2} Under Terrain at {3}, terrain:{4},{5}", owner.name, owner.NetId, owner.HasAuthority ? "Local" : ((owner.Owner == null) ? ((object)"Server") : ((object)owner.Owner)), snapshotPosition, vector2, collider));
					}
				}
				else if (verboseLogs.LogEnabled())
				{
					verboseLogs.Log(string.Format("{0} {1} {2} Above Terrain at {3}, terrain:{4},{5}", owner.name, owner.NetId, owner.HasAuthority ? "Local" : ((owner.Owner == null) ? ((object)"Server") : ((object)owner.Owner)), snapshotPosition, vector2, collider));
				}
			}
			else if (isUnderTerrain && verboseLogs.WarnEnabled())
			{
				verboseLogs.LogWarning(string.Format("{0} {1} {2} Under Terrain", owner.name, owner.NetId, owner.HasAuthority ? "Local" : ((owner.Owner == null) ? ((object)"Server") : ((object)owner.Owner))));
			}
		}

		public static int CalculateUnderTerrainCost(float deltaTime)
		{
			return Mathf.CeilToInt(100f * deltaTime);
		}

		public static int CalculateSpeedCost(float percentOfMax)
		{
			if (percentOfMax <= 1f)
			{
				return 0;
			}
			if (percentOfMax > 1000f)
			{
				return 1000000;
			}
			float num = percentOfMax * percentOfMax;
			return Mathf.FloorToInt(5f * num);
		}

		public static void SetRunChecks(bool? enabled)
		{
			Enabled = enabled ?? true;
		}

		public static void SetClientChecksEnabled(bool? enabled)
		{
			ClientChecksEnabled = enabled == true;
		}
	}
}
