using System.Runtime.CompilerServices;
using UnityEngine;

namespace NuclearOption.CodeGenHelper
{
	public static class CodeGenHelperMethods
	{
		private static void Throw<T>(string from, T value) where T : struct
		{
			throw new FloatException($"Invalid float from {from}. Value={value}");
		}

		private static void Throw<T1, T2>(string from, T1 value1, T2 value2) where T1 : struct where T2 : struct
		{
			throw new FloatException($"Invalid float from {from}. Value1={value1} Value2={value2}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool PosCheck(Vector3 pos)
		{
			if (float.IsFinite(pos.x) && float.IsFinite(pos.y))
			{
				return float.IsFinite(pos.z);
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool RotCheck(Quaternion rot)
		{
			if (float.IsFinite(rot.x) && float.IsFinite(rot.y) && float.IsFinite(rot.z))
			{
				return float.IsFinite(rot.w);
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Transform_set_position(Transform transform, Vector3 pos)
		{
			if (PosCheck(pos))
			{
				transform.position = pos;
			}
			else
			{
				Throw("Transform.set_position", pos);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Transform_set_rotation(Transform transform, Quaternion rot)
		{
			if (RotCheck(rot))
			{
				transform.rotation = rot;
			}
			else
			{
				Throw("Transform.set_rotation", rot);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Transform_SetPositionAndRotation(Transform transform, Vector3 pos, Quaternion rot)
		{
			if (PosCheck(pos) && RotCheck(rot))
			{
				transform.SetPositionAndRotation(pos, rot);
			}
			else
			{
				Throw("Transform.SetPositionAndRotation", pos, rot);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Transform_set_localPosition(Transform transform, Vector3 pos)
		{
			if (PosCheck(pos))
			{
				transform.localPosition = pos;
			}
			else
			{
				Throw("Transform.set_localPosition", pos);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Transform_set_localRotation(Transform transform, Quaternion rot)
		{
			if (RotCheck(rot))
			{
				transform.localRotation = rot;
			}
			else
			{
				Throw("Transform.set_localRotation", rot);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Transform_SetLocalPositionAndRotation(Transform transform, Vector3 pos, Quaternion rot)
		{
			if (PosCheck(pos) && RotCheck(rot))
			{
				transform.SetLocalPositionAndRotation(pos, rot);
			}
			else
			{
				Throw("Transform.SetLocalPositionAndRotation", pos, rot);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Rigidbody_set_position(Rigidbody rb, Vector3 pos)
		{
			if (PosCheck(pos))
			{
				rb.position = pos;
			}
			else
			{
				Throw("Rigidbody.set_position", pos);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Rigidbody_MovePosition(Rigidbody rb, Vector3 pos)
		{
			if (PosCheck(pos))
			{
				rb.MovePosition(pos);
			}
			else
			{
				Throw("Rigidbody.MovePosition", pos);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Rigidbody_set_velocity(Rigidbody rb, Vector3 vel)
		{
			if (PosCheck(vel))
			{
				rb.velocity = vel;
			}
			else
			{
				Throw("Rigidbody.set_velocity", vel);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Rigidbody_set_angularVelocity(Rigidbody rb, Vector3 vel)
		{
			if (PosCheck(vel))
			{
				rb.angularVelocity = vel;
			}
			else
			{
				Throw("Rigidbody.set_angularVelocity", vel);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Rigidbody_set_rotation(Rigidbody rb, Quaternion rot)
		{
			if (RotCheck(rot))
			{
				rb.rotation = rot;
			}
			else
			{
				Throw("Rigidbody.set_rotation", rot);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Rigidbody_MoveRotation(Rigidbody rb, Quaternion rot)
		{
			if (RotCheck(rot))
			{
				rb.MoveRotation(rot);
			}
			else
			{
				Throw("Rigidbody.MoveRotation", rot);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check_Rigidbody_Move(Rigidbody rb, Vector3 pos, Quaternion rot)
		{
			if (RotCheck(rot))
			{
				rb.Move(pos, rot);
			}
			else
			{
				Throw("Rigidbody.Move", rot);
			}
		}
	}
}
