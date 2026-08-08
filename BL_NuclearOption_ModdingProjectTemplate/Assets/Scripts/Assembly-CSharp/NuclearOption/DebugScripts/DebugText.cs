using TMPro;
using UnityEngine;

namespace NuclearOption.DebugScripts
{
	[DefaultExecutionOrder(100)]
	public class DebugText : MonoBehaviour
	{
		public TextMeshProUGUI Text;

		private Camera camera;

		private void OnEnable()
		{
			camera = Camera.main;
		}

		private void LateUpdate()
		{
			if (camera != null)
			{
				Vector3 forward = base.transform.position - camera.transform.position;
				base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			}
		}
	}
}
