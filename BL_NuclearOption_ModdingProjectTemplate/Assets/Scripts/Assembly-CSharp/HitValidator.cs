using System.Collections.Generic;
using UnityEngine;

public class HitValidator
{
	private class FiringLog
	{
		private struct Snapshot
		{
			private readonly Ray ray;

			public readonly float timestamp;

			public Snapshot(Vector3 position, Vector3 velocity)
			{
				ray = new Ray(position, velocity);
				timestamp = Time.timeSinceLevelLoad;
			}

			public bool HitPlausible(Vector3 hitPosition, Vector3 hitVelocity)
			{
				float num = Vector3.Distance(hitPosition, ray.origin);
				if (Vector3.Angle(hitPosition - ray.origin, ray.direction) * Mathf.Clamp01(num * 0.01f) < 10f)
				{
					return num < 3000f;
				}
				return false;
			}

			public float GetAge()
			{
				return Time.timeSinceLevelLoad - timestamp;
			}
		}

		private readonly List<Snapshot> snapshots = new List<Snapshot>();

		private float lastUpdate;

		public FiringLog(float lastUpdate)
		{
			this.lastUpdate = lastUpdate;
		}

		public void LogFiring(Vector3 firePosition, Vector3 fireVelocity)
		{
			if (snapshots.Count > 0)
			{
				List<Snapshot> list = snapshots;
				if (list[list.Count - 1].GetAge() < 0.1f)
				{
					return;
				}
			}
			snapshots.Add(new Snapshot(firePosition, fireVelocity));
			if (snapshots[0].GetAge() > 5f)
			{
				snapshots.RemoveAt(0);
			}
		}

		public bool HitValidated(Vector3 hitPosition, Vector3 hitVelocity)
		{
			for (int num = snapshots.Count - 1; num >= 0; num--)
			{
				if (snapshots[num].GetAge() > 5f)
				{
					snapshots.RemoveAt(num);
				}
				else if (snapshots[num].HitPlausible(hitPosition, hitVelocity))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static Dictionary<PersistentID, FiringLog> firingLogs;

	private HitValidator()
	{
	}

	public static void LogFiring(PersistentID shooter, Vector3 position, Vector3 velocity)
	{
		if (!firingLogs.TryGetValue(shooter, out var value))
		{
			value = new FiringLog(0f);
			firingLogs.Add(shooter, value);
		}
		value.LogFiring(position, velocity);
	}

	public static bool HitValidated(Unit claimer, Vector3 hitPosition, Vector3 hitVelocity)
	{
		if (claimer == null)
		{
			ColorLog<HitValidator>.InfoWarn("Unable to validate claimed hit - unit does not exist");
			return false;
		}
		if (!firingLogs.TryGetValue(claimer.persistentID, out var value))
		{
			ColorLog<HitValidator>.InfoWarn("Unable to validate hit claimed by " + GetOwnerInfo(claimer) + " - firing log does not exist");
			return false;
		}
		if (!value.HitValidated(hitPosition, hitVelocity))
		{
			ColorLog<HitValidator>.LogError("Unable to validate hit claimed by " + GetOwnerInfo(claimer));
			return false;
		}
		return true;
		static string GetOwnerInfo(Unit unit)
		{
			if (unit is Aircraft aircraft && aircraft.Player != null)
			{
				return $"{unit} (Owner: {aircraft.Player})";
			}
			return $"{unit}";
		}
	}

	public static void Initialize()
	{
		if (firingLogs == null)
		{
			firingLogs = new Dictionary<PersistentID, FiringLog>();
		}
		else
		{
			firingLogs.Clear();
		}
	}
}
