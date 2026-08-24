public readonly struct DamageablePart
{
	public readonly IDamageable Damageable;

	public readonly bool Removed;

	public DamageablePart(IDamageable part, bool removed = false)
	{
		Damageable = part;
		Removed = removed;
	}

	public DamageablePart CreateRemoved()
	{
		return new DamageablePart(Damageable, removed: true);
	}
}
