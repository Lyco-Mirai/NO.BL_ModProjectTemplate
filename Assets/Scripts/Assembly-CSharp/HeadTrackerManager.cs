using System;
using UnityEngine;
using BL.NO_Patches;

public static class HeadTrackerManager
{
	public static IHeadTracker ActiveTracker
	{
		get
		{
			if (PlayerSettings.headTrackerType == HeadTrackerType.TrackIR)
			{
				return TrackIRComponent.i;
			}
			return null;
		}
	}

	public static Tuple<Vector3, Quaternion> GetOffset(Vector3 basePosition, Quaternion baseOrientation)
	{
		IHeadTracker activeTracker = ActiveTracker;
		if (activeTracker != null)
		{
			return activeTracker.GetHeadTrackerOffset(basePosition, baseOrientation);
		}
		return new Tuple<Vector3, Quaternion>(basePosition, baseOrientation);
	}

	public static void Recenter()
	{
		ActiveTracker?.Recenter();
	}
}
