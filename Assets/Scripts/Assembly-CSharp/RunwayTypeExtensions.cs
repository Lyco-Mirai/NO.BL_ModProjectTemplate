using System.Runtime.CompilerServices;

public static class RunwayTypeExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Allowed(this RunwayType runway, RunwayQueryType query)
	{
		if (runway == RunwayType.None)
		{
			return false;
		}
		if (query == RunwayQueryType.Any)
		{
			return true;
		}
		return (int)((uint)runway & (uint)query) > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool QueryFor(this RunwayQueryType query, RunwayType runway)
	{
		if (query == RunwayQueryType.Any)
		{
			return true;
		}
		return (int)((uint)runway & (uint)query) > 0;
	}
}
