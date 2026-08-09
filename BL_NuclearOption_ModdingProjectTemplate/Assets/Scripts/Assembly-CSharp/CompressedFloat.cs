using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct CompressedFloat
{
	public const float FLOAT_TO_HALF_MIN = -65504f;

	public const float FLOAT_TO_HALF_MAX = 65504f;

	[FieldOffset(0)]
	public ushort Value;
}
