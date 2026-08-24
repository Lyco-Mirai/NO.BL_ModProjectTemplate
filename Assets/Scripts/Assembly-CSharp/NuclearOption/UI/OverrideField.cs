using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class OverrideField : MonoBehaviour
	{
		[SerializeField]
		private BaseToggle overrideToggle;

		[SerializeField]
		private Selectable[] fields;

		private void Awake()
		{
			overrideToggle.onValueChanged.AddListener(OverrideChanged);
			OverrideChanged(overrideToggle.isOn);
		}

		private void OnValidate()
		{
			if (overrideToggle != null)
			{
				OverrideChanged(overrideToggle.isOn);
			}
		}

		private void OverrideChanged(bool isOn)
		{
			Selectable[] array = fields;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].interactable = isOn;
			}
		}
	}
}
