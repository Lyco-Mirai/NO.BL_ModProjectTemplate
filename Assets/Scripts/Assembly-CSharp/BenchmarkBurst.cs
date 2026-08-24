using System.Runtime.CompilerServices;
using Unity.Profiling.LowLevel.Unsafe;

public static class BenchmarkBurst
{
	private static readonly double msPerTick;

	static BenchmarkBurst()
	{
		ProfilerUnsafeUtility.TimestampConversionRatio timestampToNanosecondsConversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
		msPerTick = (double)timestampToNanosecondsConversionRatio.Numerator / (double)timestampToNanosecondsConversionRatio.Denominator / 1000000.0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetTimestamp()
	{
		return ProfilerUnsafeUtility.Timestamp;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double MillisecondsSince(long startTicks)
	{
		return (double)(ProfilerUnsafeUtility.Timestamp - startTicks) * msPerTick;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long TicksSince(long startTicks)
	{
		return ProfilerUnsafeUtility.Timestamp - startTicks;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double TicksToMilliseconds(long ticks)
	{
		return (double)ticks * msPerTick;
	}
}
