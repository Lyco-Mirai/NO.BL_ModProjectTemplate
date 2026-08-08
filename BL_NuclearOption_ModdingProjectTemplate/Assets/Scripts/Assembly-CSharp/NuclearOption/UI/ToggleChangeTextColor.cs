using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class ToggleChangeTextColor : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI text;

		[SerializeField]
		private Toggle toggle;

		[SerializeField]
		private Color onColor = Color.white;

		[SerializeField]
		private Color offColor = Color.grey;

		private void Awake()
		{
			toggle.onValueChanged.AddListener(OnChanged);
			OnChanged(toggle.isOn);
		}

		private void OnValidate()
		{
			if (text != null && toggle != null)
			{
				OnChanged(toggle.isOn);
			}
		}

		private void OnChanged(bool isOn)
		{
			text.color = (isOn ? onColor : offColor);
		}
	}
}
