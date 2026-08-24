using System;

public struct PersistentID : IEquatable<PersistentID>
{
	public uint Id;

	public static PersistentID None => default(PersistentID);

	public bool IsValid => Id != 0;

	public bool NotValid => Id == 0;

	public readonly bool TryGetUnit(out Unit unit)
	{
		return UnitRegistry.TryGetUnit(this, out unit);
	}

	public override string ToString()
	{
		return Id.ToString();
	}

	public bool Equals(PersistentID other)
	{
		return Id == other.Id;
	}

	public override bool Equals(object obj)
	{
		if (obj is PersistentID other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}

	public static bool operator ==(PersistentID left, PersistentID right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(PersistentID left, PersistentID right)
	{
		return !left.Equals(right);
	}
}
