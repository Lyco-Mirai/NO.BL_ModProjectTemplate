namespace NuclearOption.SavedMission
{
	public class ValueWrapperInt : ValueWrapper<int>, IValueWrapper<float>, IValueWrapper
	{
		float IValueWrapper<float>.Value => base.Value;

		void IValueWrapper<float>.RegisterOnChange(object owner, ValueWrapper<float>.OnChangeDelegate callback)
		{
			RegisterOnChange(owner, delegate(int v)
			{
				callback(v);
			});
		}

		void IValueWrapper<float>.SetValue(float value, object source, bool invokeOnChangeOnly)
		{
			SetValue((int)value, source, invokeOnChangeOnly);
		}

		public ValueWrapperInt()
		{
		}

		public ValueWrapperInt(int value)
			: base(value)
		{
		}
	}
}
