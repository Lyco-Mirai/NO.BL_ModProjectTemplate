using JamesFrowen.ScriptableVariables.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	public abstract class HighlightButton : ButtonController
	{
		[Header("Highlight")]
		[SerializeField]
		private Image _buttonImage;

		[SerializeField]
		private Color _highlight;

		[SerializeField]
		private Color _normal;

		public void Highlight(bool highlight)
		{
			_buttonImage.color = (highlight ? _highlight : _normal);
		}
	}
}
