using UnityEngine;

public struct Vector3Compressed
{
	public CompressedFloat x;

	public CompressedFloat y;

	public CompressedFloat z;

	public static readonly Vector3Compressed zero = Vector3.zero.Compress();

	public static readonly Vector3Compressed forward = Vector3.forward.Compress();

	public override string ToString()
	{
		return $"Compressed({this.Decompress()})";
	}
}
