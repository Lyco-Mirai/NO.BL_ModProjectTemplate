using System;

namespace NuclearOption.SavedMission.Outcomes
{
	public static class ChangeTypeHelper
	{
		public static void ChangeValue(float value, ChangeType type, float oldValue, Action<float> setValue)
		{
			switch (type)
			{
			case ChangeType.Add:
				if (value != 0f)
				{
					setValue(oldValue + value);
				}
				break;
			case ChangeType.Subtract:
				if (value != 0f)
				{
					setValue(oldValue - value);
				}
				break;
			case ChangeType.Set:
				if (value != oldValue)
				{
					setValue(value);
				}
				break;
			}
		}
	}
}
