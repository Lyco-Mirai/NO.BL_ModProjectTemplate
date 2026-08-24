using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	[RequireComponent(typeof(Toggle))]
	public class ToggleRightClick : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		private Toggle toggle;

		public event Action<Toggle> RightClicked;

		private void Awake()
		{
			toggle = GetComponent<Toggle>();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Right)
			{
				this.RightClicked?.Invoke(toggle);
			}
		}
	}
}
