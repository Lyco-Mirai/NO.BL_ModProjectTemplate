using UnityEngine;

namespace NuclearOption.SavedMission
{
	public sealed class ValueWrapperGlobalPosition : ValueWrapper<GlobalPosition>, IValueWrapper<Vector3>, IValueWrapper
	{
		Vector3 IValueWrapper<Vector3>.Value => base.Value.AsVector3();

		void IValueWrapper<Vector3>.RegisterOnChange(object owner, ValueWrapper<Vector3>.OnChangeDelegate callback)
		{
			RegisterOnChange(owner, delegate(GlobalPosition v)
			{
				callback(v.AsVector3());
			});
		}

		void IValueWrapper<Vector3>.SetValue(Vector3 value, object source, bool invokeOnChangeOnly)
		{
			SetValue(new GlobalPosition(value), source, invokeOnChangeOnly);
		}

		public ValueWrapperGlobalPosition()
		{
		}

		public ValueWrapperGlobalPosition(GlobalPosition value)
			: base(value)
		{
		}
	}
}
