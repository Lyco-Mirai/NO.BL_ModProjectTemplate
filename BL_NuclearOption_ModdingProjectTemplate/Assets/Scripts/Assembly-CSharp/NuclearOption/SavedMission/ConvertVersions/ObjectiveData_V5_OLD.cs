using System;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct ObjectiveData_V5_OLD
	{
		public string StringValue;

		public float FloatValue;

		public Vector3 VectorValue;
	}
}
