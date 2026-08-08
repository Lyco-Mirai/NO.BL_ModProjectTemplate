using UnityEngine;

namespace NuclearOption.Effects
{
	public class ShaderGlobalManager : SceneSingleton<ShaderGlobalManager>
	{
		public static readonly int ID_Global_BlastMap = Shader.PropertyToID("_Global_BlastMap");

		public static readonly int ID_Datum_WorldExtent = Shader.PropertyToID("_Datum_WorldExtent");

		public static readonly int ID_Datum_OriginPosition = Shader.PropertyToID("_Datum_OriginPosition");

		public static readonly int ID_Global_FrustumPlanes = Shader.PropertyToID("_Global_FrustumPlanes");

		public static readonly int ID_Global_CameraPosition = Shader.PropertyToID("_Global_CameraPosition");

		public static readonly int ID_Global_CameraTarget = Shader.PropertyToID("_Global_CameraTarget");

		[SerializeField]
		private MapSettings mapSettings;

		private static readonly Plane[] frustumPlane = new Plane[6];

		private static readonly Vector4[] frustumPlaneVectors = new Vector4[6];

		protected override void Awake()
		{
			base.Awake();
			if (!GameManager.IsHeadless)
			{
				Shader.SetGlobalVector(ID_Datum_WorldExtent, mapSettings.MapSize / 2f);
			}
		}

		private void OnDestroy()
		{
			if (!GameManager.IsHeadless)
			{
				Shader.SetGlobalVector(ID_Datum_WorldExtent, Vector2.one);
			}
		}

		public static void SetDatum(Vector3 floatingOrigin)
		{
			if (!GameManager.IsHeadless)
			{
				Shader.SetGlobalVector(ID_Datum_OriginPosition, floatingOrigin);
			}
		}

		public static void SetCameraPlanes(Camera camera, float maxTargetOffset, out Vector3 cameraTarget)
		{
			GeometryUtility.CalculateFrustumPlanes(camera, frustumPlane);
			for (int i = 0; i < 6; i++)
			{
				Vector3 normal = frustumPlane[i].normal;
				frustumPlaneVectors[i] = new Vector4(normal.x, normal.y, normal.z, frustumPlane[i].distance);
			}
			Shader.SetGlobalVectorArray(ID_Global_FrustumPlanes, frustumPlaneVectors);
			camera.transform.GetPositionAndRotation(out var position, out var rotation);
			Vector3 forward = rotation * Vector3.forward;
			Shader.SetGlobalVector(ID_Global_CameraPosition, camera.transform.position);
			cameraTarget = CalculateCameraTarget(position, forward, maxTargetOffset);
			Shader.SetGlobalVector(ID_Global_CameraTarget, cameraTarget);
		}

		private static Vector3 CalculateCameraTarget(Vector3 position, Vector3 forward, float maxTargetOffset)
		{
			if (Mathf.Abs(forward.y) > 0.999f)
			{
				return position;
			}
			float b = 0f - forward.y;
			b = Mathf.Max(0f, b);
			float num = 1f - b;
			float num2 = maxTargetOffset * num;
			float num3 = Mathf.Sqrt(forward.x * forward.x + forward.z * forward.z);
			Vector3 vector = new Vector3(forward.x / num3, 0f, forward.z / num3);
			return position + vector * num2;
		}
	}
}
