using System.Runtime.CompilerServices;
using UnityEngine;

public struct BurstDatum
{
	public Vector3 datumPosition;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToGlobalPosition(Vector3 position)
	{
		return position - datumPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToLocalPosition(Vector3 position)
	{
		return position + datumPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GlobalX(Vector3 position)
	{
		return position.x - datumPosition.x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GlobalY(Vector3 position)
	{
		return position.y - datumPosition.y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GlobalZ(Vector3 position)
	{
		return position.z - datumPosition.z;
	}
}
