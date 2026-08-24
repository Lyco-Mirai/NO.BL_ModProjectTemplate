using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class InputFieldChecker : MonoBehaviour
	{
		public static bool InsideInputField;

		private EventSystem system;

		private void Update()
		{
			if (system == null)
			{
				system = EventSystem.current;
			}
			if (!(system == null))
			{
				GameObject currentSelectedGameObject = system.currentSelectedGameObject;
				CheckInputAndFocus(currentSelectedGameObject);
			}
		}

		private void CheckInputAndFocus(GameObject currentObject)
		{
			if (currentObject != null)
			{
				if (currentObject.TryGetComponent<TMP_InputField>(out var component))
				{
					InsideInputField = component.isFocused;
					return;
				}
				if (currentObject.TryGetComponent<InputField>(out var component2))
				{
					InsideInputField = component2.isFocused;
					return;
				}
			}
			InsideInputField = false;
		}

		private void OnDestroy()
		{
			InsideInputField = false;
		}
	}
}
