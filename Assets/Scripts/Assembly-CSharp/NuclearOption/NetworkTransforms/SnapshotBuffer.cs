using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class SnapshotBuffer<T> : ISnapshotBuffer where T : struct
	{
		public readonly struct TimedSnapshot
		{
			public readonly double Timestamp;

			public readonly T Snapshot;

			public TimedSnapshot(double timestamp, T snapshot)
			{
				Timestamp = timestamp;
				Snapshot = snapshot;
			}
		}

		protected readonly List<TimedSnapshot> buffer = new List<TimedSnapshot>(4);

		public int Count => buffer.Count;

		public TimedSnapshot Get(int i)
		{
			return buffer[i];
		}

		public TimedSnapshot? GetNullable(int i)
		{
			if (0 > i || i >= buffer.Count)
			{
				return null;
			}
			return buffer[i];
		}

		public void GetLastValues(out TimedSnapshot previous, out TimedSnapshot current)
		{
			List<TimedSnapshot> list = buffer;
			previous = list[list.Count - 2];
			List<TimedSnapshot> list2 = buffer;
			current = list2[list2.Count - 1];
		}

		public void Insert(double timestamp, T snapshot, bool ignoreWarning = false)
		{
			if (buffer.Count >= 1)
			{
				List<TimedSnapshot> list = buffer;
				if (timestamp <= list[list.Count - 1].Timestamp)
				{
					if (!ignoreWarning)
					{
						Debug.LogWarning("older snapshot inserted into buffer");
					}
					return;
				}
			}
			buffer.Add(new TimedSnapshot(timestamp, snapshot));
		}

		public void RemoveOld(double timestamp)
		{
			int i = 0;
			for (int num = buffer.Count - 4; i < num && buffer[i].Timestamp < timestamp; i++)
			{
			}
			if (i > 0)
			{
				buffer.RemoveRange(0, i);
			}
		}
	}
}
