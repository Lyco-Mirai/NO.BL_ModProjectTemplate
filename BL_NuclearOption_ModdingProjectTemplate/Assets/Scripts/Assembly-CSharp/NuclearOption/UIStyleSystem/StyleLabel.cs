using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	[CreateAssetMenu(fileName = "NewStyleLabel", menuName = "UI Style System/Style Label")]
	public class StyleLabel : ScriptableObject
	{
		[SerializeField]
		public string displayLabel;

		[TextArea(3, 10)]
		[SerializeField]
		public string tooltip;
	}
}
