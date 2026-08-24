using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct GlobalPosition : IEquatable<GlobalPosition>
{
	[FieldOffset(0)]
	public float x;

	[FieldOffset(4)]
	public float y;

	[FieldOffset(8)]
	public float z;

	public GlobalPosition(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public GlobalPosition(Vector3 v)
	{
		x = v.x;
		y = v.y;
		z = v.z;
	}

	public override readonly int GetHashCode()
	{
		return AsVector3().GetHashCode();
	}

	public readonly Vector3 AsVector3()
	{
		return new Vector3(x, y, z);
	}

	public static GlobalPosition operator +(GlobalPosition pos, Vector3 move)
	{
		return new GlobalPosition(pos.x + move.x, pos.y + move.y, pos.z + move.z);
	}

	public static GlobalPosition operator -(GlobalPosition pos, Vector3 move)
	{
		return new GlobalPosition(pos.x - move.x, pos.y - move.y, pos.z - move.z);
	}

	public static Vector3 operator -(GlobalPosition to, GlobalPosition from)
	{
		return FastMath.Direction(from, to);
	}

	public static bool operator ==(GlobalPosition a, GlobalPosition b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(GlobalPosition a, GlobalPosition b)
	{
		return a.NotEqual(b);
	}

	public override readonly bool Equals(object obj)
	{
		if (obj is GlobalPosition other)
		{
			return Equals(other);
		}
		return false;
	}

	public readonly bool Equals(GlobalPosition other)
	{
		if (x == other.x && y == other.y)
		{
			return z == other.z;
		}
		return false;
	}

	public readonly bool NotEqual(GlobalPosition other)
	{
		return !Equals(other);
	}

	public override readonly string ToString()
	{
		return $"Global({x:0.0},{y:0.0},{z:0.0})";
	}
}
