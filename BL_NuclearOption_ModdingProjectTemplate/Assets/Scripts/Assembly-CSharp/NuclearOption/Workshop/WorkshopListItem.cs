using System.Threading;
using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class WorkshopListItem : ListItem<SteamWorkshopItem>, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
	{
		[Header("UI")]
		[SerializeField]
		private TextMeshProUGUI nameText;

		[SerializeField]
		private GameObject installBadge;

		[SerializeField]
		private GameObject ownerBadge;

		[SerializeField]
		private Image previewImage;

		[SerializeField]
		private float startScale;

		[SerializeField]
		private float hoverScale;

		[SerializeField]
		private float hoverSeconds;

		private WorkshopMenu workshopMenu;

		private CancellationTokenSource cancellationTokenSource;

		private float scale;

		private bool hover;

		protected override void Awake()
		{
			workshopMenu = GetComponentInParent<WorkshopMenu>();
		}

		private void Update()
		{
			float num = (hover ? hoverScale : 1f);
			if (scale != num)
			{
				scale = Mathf.MoveTowards(scale, num, Time.deltaTime / hoverSeconds);
				base.transform.localScale = Vector3.one * scale;
			}
		}

		private void OnDisable()
		{
			nameText.text = "";
		}

		protected override void SetValue(SteamWorkshopItem value)
		{
			if (nameText.text != value.Name)
			{
				scale = startScale;
			}
			nameText.text = value.Name;
			installBadge.SetActive(value.Subscribed);
			ownerBadge.SetActive(value.IsOwner());
			value.SetPreviewImageAsync(previewImage, ref cancellationTokenSource).Forget();
		}

		public void OpenDetails()
		{
			workshopMenu.OpenItemDetailsPanel(base.Value);
		}

		void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
		{
			OpenDetails();
		}

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
		{
			hover = true;
		}

		void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
		{
			hover = false;
		}
	}
}
