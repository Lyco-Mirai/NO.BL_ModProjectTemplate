using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	public class ShowHoverText : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerMoveHandler
	{
		[SerializeField]
		private string showText;

		[SerializeField]
		private HoverText hover;

		public void SetText(string text)
		{
			showText = text;
		}

		public void SetHover(HoverText hoverText)
		{
			hover = hoverText;
		}

		public void OnPointerMove(PointerEventData eventData)
		{
			if (hover != null)
			{
				Vector2 position = eventData.position;
				hover.Move(this, position);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			hover.Refresh(showText);
			hover.Show(this, showText);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			hover.Hide(this);
		}
	}
}
