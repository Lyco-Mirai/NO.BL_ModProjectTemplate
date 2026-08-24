using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class FloatingOrigin : MonoBehaviour
{
	private static readonly ProfilerMarker originShiftMarker = new ProfilerMarker("FloatingOrigin.OriginShift");

	private static readonly ProfilerMarker moveRootsMarker = new ProfilerMarker("FloatingOrigin.OriginShift_MoveRoots");

	[SerializeField]
	private Transform globalDatum;

	[SerializeField]
	private AudioClip shiftSound;

	[Tooltip("Distance from origin on any axis that triggers a shift.")]
	[SerializeField]
	private float threshold = 1024f;

	[Tooltip("Aligns the origin move to a grid, Origin Shift will always be a multiple of this. This helps shaders create stable positions at large scales")]
	[SerializeField]
	private float originShiftStep = 64f;

	public static FloatingOrigin Instance;

	private Bounds worldSimulate;

	private readonly List<GameObject> roots = new List<GameObject>();

	private void Awake()
	{
		Datum.SetOrigin(globalDatum);
		Instance = this;
		worldSimulate.center = Datum.origin.position;
		worldSimulate.SetMinMax(new Vector3(worldSimulate.center.x - 90000f, worldSimulate.center.y - 90000f, worldSimulate.center.z - 90000f), new Vector3(worldSimulate.center.x + 90000f, worldSimulate.center.y + 90000f, worldSimulate.center.z + 90000f));
	}

	private Vector3 ShiftPosition(Vector3 cameraPosition)
	{
		return new Vector3(Mathf.Round(cameraPosition.x / originShiftStep) * originShiftStep, Mathf.Round(cameraPosition.y / originShiftStep) * originShiftStep, Mathf.Round(cameraPosition.z / originShiftStep) * originShiftStep);
	}

	private bool ShouldShift(Vector3 cameraPosition)
	{
		if (!(Mathf.Abs(cameraPosition.x) > threshold) && !(Mathf.Abs(cameraPosition.y) > threshold))
		{
			return Mathf.Abs(cameraPosition.z) > threshold;
		}
		return true;
	}

	public void OriginShift(Vector3 cameraPosition)
	{
		if (EditorHandle.DraggingHandle || !ShouldShift(cameraPosition))
		{
			return;
		}
		using (originShiftMarker.Auto())
		{
			Vector3 vector = ShiftPosition(cameraPosition);
			using (moveRootsMarker.Auto())
			{
				roots.Clear();
				SceneManager.GetActiveScene().GetRootGameObjects(roots);
				foreach (GameObject root in roots)
				{
					root.transform.position -= vector;
				}
			}
			Datum.AfterOriginShift();
			Physics.SyncTransforms();
		}
	}

	private void RBKillBounds()
	{
		Object[] array = Object.FindObjectsOfType(typeof(Rigidbody));
		for (int i = 0; i < array.Length; i++)
		{
			Rigidbody rigidbody = (Rigidbody)array[i];
			if (!worldSimulate.Contains(rigidbody.transform.position))
			{
				Debug.Log(rigidbody.gameObject.name + " was outside world bounds and has been destroyed");
				Object.Destroy(rigidbody.gameObject);
			}
		}
	}
}
