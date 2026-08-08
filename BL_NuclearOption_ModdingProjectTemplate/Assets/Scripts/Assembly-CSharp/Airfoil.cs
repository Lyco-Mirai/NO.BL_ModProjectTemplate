using System;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class Airfoil
{
	public string name;

	[NonSerialized]
	public int id;

	public AnimationCurve liftCoef;

	public AnimationCurve dragCoef;

	public NativeArray<float> BuildLiftChart()
	{
		NativeArray<float> result = new NativeArray<float>(128, Allocator.Temp);
		for (int i = 0; i < 128; i++)
		{
			float time = (float)(i - 64) * 0.04908734f;
			result[i] = liftCoef.Evaluate(time);
		}
		return result;
	}

	public NativeArray<float> BuildDragChart()
	{
		NativeArray<float> result = new NativeArray<float>(128, Allocator.Temp);
		for (int i = 0; i < 128; i++)
		{
			float time = (float)(i - 64) * 0.04908734f;
			result[i] = dragCoef.Evaluate(time);
		}
		return result;
	}
}
