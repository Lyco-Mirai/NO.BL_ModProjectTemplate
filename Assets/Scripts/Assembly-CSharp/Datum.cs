using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuclearOption.Effects;
using UnityEngine;

public static class Datum
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct DatumLog
	{
	}

	public class DatumDestroyTracker : MonoBehaviour
	{
		private void OnDestroy()
		{
			OnDatumDestroyed(base.transform);
		}
	}

	public static readonly GlobalPosition SeaLevel = new GlobalPosition(0f, 0f, 0f);

	public static Transform origin { get; private set; }

	public static Vector3 originPosition { get; private set; }

	public static float LocalSeaY
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return originPosition.y;
		}
	}

	public static void AfterOriginShift()
	{
		originPosition = origin.position;
		ShaderGlobalManager.SetDatum(originPosition);
		DetailRenderer.DatumShifted = true;
	}

	public static DatumDestroyTracker SetOrigin(Transform newOrigin)
	{
		if (newOrigin == null)
		{
			throw new ArgumentNullException("newOrigin");
		}
		origin = newOrigin;
		DatumDestroyTracker result = origin.gameObject.AddComponent<DatumDestroyTracker>();
		AfterOriginShift();
		return result;
	}

	public static void OnDatumDestroyed(Transform oldDatum)
	{
		origin = null;
		originPosition = Vector3.zero;
	}

	public static Plane WaterPlane()
	{
		return new Plane(Vector3.up, origin.position);
	}

	public static BurstDatum GetBurstDatum()
	{
		return new BurstDatum
		{
			datumPosition = originPosition
		};
	}
}
