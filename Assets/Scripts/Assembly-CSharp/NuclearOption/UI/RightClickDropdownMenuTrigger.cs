using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.UI
{
	public class RightClickDropdownMenuTrigger : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		[SerializeField]
		private PointerEventData.InputButton inputButton = PointerEventData.InputButton.Right;

		public RightClickDropdownMenuBase Target;

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (Target == null)
			{
				Debug.LogError("OnPointerClick called without a Target");
			}
			else if (eventData.button == inputButton)
			{
				Vector2 position = eventData.position;
				Target.Show(this, position);
			}
		}
	}
}
