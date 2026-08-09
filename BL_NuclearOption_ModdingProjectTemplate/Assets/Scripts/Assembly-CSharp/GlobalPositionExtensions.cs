using System.Runtime.CompilerServices;
using Mirage.Serialization;
using UnityEngine;

public static class GlobalPositionExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GlobalPosition ToGlobalPosition(this Vector3 position)
	{
		Vector3 vector = position - Datum.originPosition;
		return new GlobalPosition(vector.x, vector.y, vector.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GlobalX(this Vector3 position)
	{
		return position.x - Datum.originPosition.x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GlobalY(this Vector3 position)
	{
		return position.y - Datum.originPosition.y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GlobalZ(this Vector3 position)
	{
		return position.z - Datum.originPosition.z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 ToLocalPosition(this GlobalPosition position)
	{
		return position.AsVector3() + Datum.originPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float LocalX(this GlobalPosition position)
	{
		return position.x + Datum.originPosition.x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float LocalY(this GlobalPosition position)
	{
		return position.y + Datum.originPosition.y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float LocalZ(this GlobalPosition position)
	{
		return position.z + Datum.originPosition.z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GlobalPosition GlobalPosition(this Unit unit)
	{
		return unit.transform.position.ToGlobalPosition();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GlobalPosition GlobalPosition(this Transform transform)
	{
		return transform.position.ToGlobalPosition();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteGlobalPosition(this NetworkWriter writer, GlobalPosition value)
	{
		writer.WriteVector3(value.AsVector3());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GlobalPosition ReadGlobalPosition(this NetworkReader reader)
	{
		return new GlobalPosition(reader.ReadVector3());
	}
}
