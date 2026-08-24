using UnityEngine;

namespace NuclearOption.SavedMission
{
	public class ValueWrapperQuaternion : ValueWrapper<Quaternion>, IValueWrapper<Vector3>, IValueWrapper
	{
		Vector3 IValueWrapper<Vector3>.Value => base.Value.eulerAngles;

		void IValueWrapper<Vector3>.RegisterOnChange(object owner, ValueWrapper<Vector3>.OnChangeDelegate callback)
		{
			RegisterOnChange(owner, delegate(Quaternion v)
			{
				callback(v.eulerAngles);
			});
		}

		void IValueWrapper<Vector3>.SetValue(Vector3 value, object source, bool invokeOnChangeOnly)
		{
			SetValue(Quaternion.Euler(value), source, invokeOnChangeOnly);
		}

		public ValueWrapperQuaternion()
		{
		}

		public ValueWrapperQuaternion(Quaternion value)
			: base(value)
		{
		}
	}
}
