using System.Threading;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct JobSharedFields
	{
		public BurstDatum datum;

		public float fixedDeltaTime;

		public float timeSinceLevelLoad;

		public int tickOffset;

		public long controlMath;

		public long controlAccess;

		public long aeroAccess;

		public long aeroMath;

		public long vehicleAccess;

		public long vehicle1;

		public long vehicle2;

		public long waterAccess;

		public long waterMath;

		public int DebugMarkersCount;

		public PtrArray<DebugVisJobMarker> DebugMarkersArray;

		public void LogAndReset(JobPerf jobPerf)
		{
			controlMath = 0L;
			controlAccess = 0L;
			aeroAccess = 0L;
			aeroMath = 0L;
			vehicleAccess = 0L;
			vehicle1 = 0L;
			vehicle2 = 0L;
			waterAccess = 0L;
			waterMath = 0L;
		}

		public bool NextDebugIndex(out Ptr<DebugVisJobMarker> marker)
		{
			int num = Interlocked.Increment(ref DebugMarkersCount) - 1;
			if (num < DebugMarkersArray.Length)
			{
				marker = DebugMarkersArray.GetPtr(num);
				return true;
			}
			marker = default(Ptr<DebugVisJobMarker>);
			return false;
		}

		public void DrawDebugMarkers()
		{
			if (DebugMarkersArray.Length == 0)
			{
				DebugMarkersArray = new PtrArray<DebugVisJobMarker>(64);
				return;
			}
			int num = Mathf.Min(DebugMarkersCount, DebugMarkersArray.Length);
			DebugMarkersCount = 0;
			for (int i = 0; i < num; i++)
			{
				DebugVisJobMarker debugVisJobMarker = DebugMarkersArray[i];
				switch (debugVisJobMarker.Type)
				{
				case DebugVisJobMarkerType.DebugPoint:
				{
					GameObject gameObject2 = Object.Instantiate(GameAssets.i.debugPoint, Datum.origin);
					gameObject2.transform.position = debugVisJobMarker.Position;
					gameObject2.transform.localScale = Vector3.one * 3f;
					Object.Destroy(gameObject2, 0.2f);
					break;
				}
				case DebugVisJobMarkerType.DebugArrowGreen:
				{
					GameObject gameObject = Object.Instantiate(GameAssets.i.debugArrowGreen, Datum.origin);
					gameObject.transform.position = debugVisJobMarker.Position;
					gameObject.transform.rotation = debugVisJobMarker.Rotation;
					gameObject.transform.localScale = debugVisJobMarker.Scale;
					Object.Destroy(gameObject, 0.2f);
					break;
				}
				}
			}
		}
	}
}
