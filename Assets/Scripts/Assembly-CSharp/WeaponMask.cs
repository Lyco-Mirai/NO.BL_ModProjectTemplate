using System;

[Serializable]
public struct WeaponMask
{
	public readonly int Mask;

	public WeaponMask(int mask)
	{
		Mask = mask;
	}

	public bool Get(int index)
	{
		return (Mask & (1 << index)) != 0;
	}

	public static WeaponMask Set(WeaponMask oldMask, int index, bool value)
	{
		int mask = oldMask.Mask;
		mask = ((!value) ? (mask & ~(1 << index)) : (mask | (1 << index)));
		return new WeaponMask(mask);
	}

	public static bool HasChangedToTrue(WeaponMask oldMask, WeaponMask newMask, int index)
	{
		bool num = oldMask.Get(index);
		bool flag = newMask.Get(index);
		return !num && flag;
	}
}
