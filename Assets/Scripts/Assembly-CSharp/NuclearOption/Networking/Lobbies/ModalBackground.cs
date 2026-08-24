using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.Networking.Lobbies
{
	public class ModalBackground : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		[SerializeField]
		private GameObject modal;

		public void OnPointerClick(PointerEventData eventData)
		{
			modal.gameObject.SetActive(value: false);
		}
	}
}
