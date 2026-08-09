using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	[RequireComponent(typeof(ILayoutElement))]
	public class UsePreferredSize : MonoBehaviour, ILayoutSelfController, ILayoutController
	{
		[SerializeField]
		private Vector2 padding;

		[SerializeField]
		private bool setWidth;

		[SerializeField]
		private bool setHeight;

		private ILayoutElement element;

		public void SetLayoutHorizontal()
		{
			Resize(setWidth, setHeight: false);
		}

		public void SetLayoutVertical()
		{
			Resize(setWidth: false, setHeight);
		}

		private void Resize(bool setWidth, bool setHeight)
		{
			if ((setHeight || setWidth) && !(this == null))
			{
				if (element as Object == null)
				{
					element = GetComponent<ILayoutElement>();
				}
				RectTransform obj = (RectTransform)base.transform;
				Vector2 sizeDelta = obj.sizeDelta;
				if (setWidth)
				{
					sizeDelta.x = element.preferredWidth + padding.x;
				}
				if (setHeight)
				{
					sizeDelta.y = element.preferredHeight + padding.y;
				}
				obj.sizeDelta = sizeDelta;
			}
		}
	}
}
