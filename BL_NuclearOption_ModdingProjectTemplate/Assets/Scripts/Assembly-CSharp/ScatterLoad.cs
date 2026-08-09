using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public static class ScatterLoad
{
	public const int BYTES_PER_TREE = 12;

	public unsafe static int LoadPositionCount(TextAsset binaryData)
	{
		return binaryData.bytes.Length / sizeof(Vector3);
	}

	public unsafe static void Load(TextAsset binaryData, int positionCount, GraphicsBuffer targetBuffer)
	{
		fixed (byte* dataPointer = &binaryData.bytes[0])
		{
			NativeArray<Vector3> data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(dataPointer, positionCount, Allocator.None);
			targetBuffer.SetData(data);
		}
	}
}
