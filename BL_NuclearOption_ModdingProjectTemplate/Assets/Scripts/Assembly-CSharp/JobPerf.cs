using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;

public class JobPerf : IDisposable
{
	private struct Entry
	{
		public string Label;

		public double Time;
	}

	private static readonly ProfilerMarker flushMarker = new ProfilerMarker("JobPerf.Flush");

	private StreamWriter writer;

	private readonly List<Entry> entries = new List<Entry>();

	public bool Enabled;

	[Conditional("ENABLE_JOB_PERF")]
	public void Flush()
	{
		using (flushMarker.Auto())
		{
			if (Enabled)
			{
				foreach (Entry entry in entries)
				{
					writer.WriteLine($"{entry.Label},{entry.Time:0.000},");
				}
			}
			entries.Clear();
		}
	}

	public void Open(string fileName = "JobPerf.csv")
	{
		UnityEngine.Debug.Log("Skipping JobPerf");
	}

	public void Dispose()
	{
		Enabled = false;
		writer?.Close();
		writer = null;
	}

	[Conditional("ENABLE_JOB_PERF")]
	public void WriteStart(string label, long start)
	{
		if (Enabled)
		{
			entries.Add(new Entry
			{
				Label = label,
				Time = BenchmarkScope.MillisecondsSince(start)
			});
		}
	}

	[Conditional("ENABLE_JOB_PERF")]
	public void WriteTicks(string label, long ticks)
	{
		if (Enabled)
		{
			entries.Add(new Entry
			{
				Label = label,
				Time = BenchmarkScope.TicksToMilliseconds(ticks)
			});
		}
	}

	[Conditional("ENABLE_JOB_PERF")]
	public void WriteTicksBurst(string label, long ticks)
	{
		if (Enabled)
		{
			entries.Add(new Entry
			{
				Label = label,
				Time = BenchmarkBurst.TicksToMilliseconds(ticks)
			});
		}
	}

	[Conditional("ENABLE_JOB_PERF")]
	public void WriteOther(string label, double value)
	{
		if (Enabled)
		{
			entries.Add(new Entry
			{
				Label = label,
				Time = value
			});
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetTimestamp()
	{
		return 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetTimestampBurst()
	{
		return 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Conditional("ENABLE_JOB_PERF")]
	internal static void AddBurst(ref long timer, long start)
	{
	}
}
