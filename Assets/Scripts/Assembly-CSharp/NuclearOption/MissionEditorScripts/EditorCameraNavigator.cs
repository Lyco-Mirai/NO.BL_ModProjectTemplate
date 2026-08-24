using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class EditorCameraNavigator : SceneSingleton<EditorCameraNavigator>
	{
		[SerializeField]
		private float cameraAcceleration;

		[SerializeField]
		private float cameraDamping;

		[SerializeField]
		private float shiftMultiplier;

		private Transform camTransform;

		private RaycastHit hit;

		private int layerMask = PhysicsLayers.StaticsMask;

		private Vector3 cameraSpeed = Vector3.zero;

		private void Start()
		{
			camTransform = SceneSingleton<CameraStateManager>.i.transform;
		}

		private void Update()
		{
			Vector3 localEulerAngles = new Vector3(base.transform.eulerAngles.x, base.transform.eulerAngles.y, 0f);
			if (Input.GetMouseButton(1))
			{
				localEulerAngles.x += GameManager.playerInput.GetAxis("Tilt View") * 0.1f;
				localEulerAngles.y += GameManager.playerInput.GetAxis("Pan View") * 0.1f;
			}
			float num = 1f;
			if (Input.GetKey("left shift"))
			{
				num = shiftMultiplier;
			}
			cameraSpeed += cameraAcceleration * num * base.transform.forward * (0f - GameManager.playerInput.GetAxis("Pitch")) * Time.unscaledDeltaTime;
			cameraSpeed += cameraAcceleration * num * base.transform.right * GameManager.playerInput.GetAxis("Roll") * Time.unscaledDeltaTime;
			base.transform.localEulerAngles = localEulerAngles;
			base.transform.position += cameraSpeed * 0.02f;
			if (Physics.Linecast(base.transform.position + Vector3.up * 5000f, base.transform.position - Vector3.up * 5000f, out hit, layerMask))
			{
				base.transform.position = new Vector3(base.transform.position.x, Mathf.Max(base.transform.position.y, hit.point.y + 1.7f), base.transform.position.z);
			}
			Vector3 position = base.transform.position;
			float num2 = Datum.LocalSeaY + 1.7f;
			if (position.y < num2)
			{
				position.y = num2;
				base.transform.position = position;
			}
			cameraSpeed -= cameraSpeed * cameraDamping * 0.01f;
			camTransform.position = base.transform.position;
			camTransform.rotation = base.transform.rotation;
		}
	}
}
