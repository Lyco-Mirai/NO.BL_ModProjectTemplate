using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class HoverColor : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public MaskableGraphic Target;

		public Color Normal;

		public Color Hover;

		private void Awake()
		{
			Target.color = Normal;
		}

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
		{
			Target.color = Hover;
		}

		void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
		{
			Target.color = Normal;
		}
	}
}
