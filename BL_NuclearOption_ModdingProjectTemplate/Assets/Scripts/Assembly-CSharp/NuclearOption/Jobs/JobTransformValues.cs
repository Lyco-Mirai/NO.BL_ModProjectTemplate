using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NuclearOption.Jobs
{
	[StructLayout(LayoutKind.Explicit, Size = 28)]
	public struct JobTransformValues
	{
		[StructLayout(LayoutKind.Explicit, Size = 28)]
		public readonly struct ReadOnly
		{
			[FieldOffset(0)]
			public readonly Quaternion Rotation;

			[FieldOffset(16)]
			public readonly Vector3 Position;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Vector3 Forward()
			{
				return Rotation * Vector3.forward;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Vector3 Back()
			{
				return Rotation * Vector3.back;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Vector3 Up()
			{
				return Rotation * Vector3.up;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Vector3 Down()
			{
				return Rotation * Vector3.down;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Vector3 Right()
			{
				return Rotation * Vector3.right;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Vector3 Left()
			{
				return Rotation * Vector3.left;
			}
		}

		[FieldOffset(0)]
		public Quaternion Rotation;

		[FieldOffset(16)]
		public Vector3 Position;
	}
}
