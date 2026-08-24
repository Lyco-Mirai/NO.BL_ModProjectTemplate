using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public struct BenchmarkScope : IDisposable
{
	private static readonly double msPerTick = 1000.0 / (double)Stopwatch.Frequency;

	public static bool ShowInRelease;

	private readonly string label;

	private readonly long startTick;

	public BenchmarkScope(string label, long startTick)
	{
		this = default(BenchmarkScope);
		this.label = label;
		this.startTick = startTick;
	}

	public void Dispose()
	{
		double num = MillisecondsSince(startTick);
		if (ShowInRelease)
		{
			ColorLog<BenchmarkScope>.Info($"{label} {num:0.000}ms");
		}
	}

	public static BenchmarkScope Create(string label)
	{
		return new BenchmarkScope(label, Stopwatch.GetTimestamp());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double MillisecondsSince(long startTick)
	{
		return (double)(Stopwatch.GetTimestamp() - startTick) * msPerTick;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double MillisecondsBetween(long startTick, long endTick)
	{
		return (double)(endTick - startTick) * msPerTick;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetTimestamp()
	{
		return Stopwatch.GetTimestamp();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long TicksSince(long startTick)
	{
		return Stopwatch.GetTimestamp() - startTick;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double TicksToMilliseconds(long ticks)
	{
		return (double)ticks * msPerTick;
	}
}
