using UnityEngine;
using UnityEngine.UI;

namespace JamesFrowen.ScriptableVariables.UI
{
	[RequireComponent(typeof(Button))]
	public abstract class ButtonController : MonoBehaviour
	{
		protected Button _button;

		protected virtual void Awake()
		{
			_button = GetComponent<Button>();
			_button.onClick.AddListener(onClick);
		}

		protected abstract void onClick();
	}
}
