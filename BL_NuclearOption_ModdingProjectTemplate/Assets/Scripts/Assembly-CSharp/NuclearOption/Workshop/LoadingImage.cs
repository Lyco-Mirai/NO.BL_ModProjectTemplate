using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class LoadingImage : MonoBehaviour
	{
		[SerializeField]
		private Image loadingImage;

		[SerializeField]
		private float fadeTime;

		[SerializeField]
		private CanvasGroup group;

		private float alpha;

		private bool active;

		private void Awake()
		{
			if (!active)
			{
				base.gameObject.SetActive(value: false);
			}
		}

		private void Update()
		{
			if (active)
			{
				if (alpha < 1f)
				{
					alpha += Time.deltaTime / fadeTime;
				}
				else
				{
					alpha = 1f;
				}
			}
			else
			{
				alpha -= Time.deltaTime / fadeTime;
			}
			if (alpha <= 0f)
			{
				alpha = 0f;
				active = false;
				base.gameObject.SetActive(value: false);
			}
			else
			{
				group.alpha = alpha;
				loadingImage.transform.Rotate(new Vector3(0f, 0f, -100f * Time.deltaTime));
			}
		}

		public void SetActive(bool active)
		{
			this.active = active;
			if (active)
			{
				base.gameObject.SetActive(value: true);
			}
		}
	}
}
