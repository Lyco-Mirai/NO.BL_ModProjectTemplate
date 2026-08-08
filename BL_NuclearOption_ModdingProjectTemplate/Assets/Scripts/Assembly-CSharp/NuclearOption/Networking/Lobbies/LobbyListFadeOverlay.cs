using UnityEngine;

namespace NuclearOption.Networking.Lobbies
{
	public class LobbyListFadeOverlay : MonoBehaviour
	{
		[SerializeField]
		private GameObject refreshingOverlay;

		[SerializeField]
		private CanvasGroup refreshingOverlayGroup;

		[SerializeField]
		private float fadeInTime;

		[SerializeField]
		private float fadeOutTime;

		private float alpha;

		private float targetAlpha;

		private float speed;

		public void Show()
		{
			refreshingOverlay.SetActive(value: true);
			targetAlpha = 1f;
			speed = ((fadeInTime > 0f) ? (1f / fadeInTime) : 10000f);
			base.enabled = true;
		}

		public void Hide()
		{
			targetAlpha = 0f;
			speed = ((fadeOutTime > 0f) ? (1f / fadeInTime) : 10000f);
		}

		private void LateUpdate()
		{
			alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.deltaTime * speed);
			refreshingOverlayGroup.alpha = alpha;
			if (alpha == targetAlpha && targetAlpha == 0f)
			{
				refreshingOverlay.SetActive(value: false);
				base.enabled = false;
			}
		}
	}
}
