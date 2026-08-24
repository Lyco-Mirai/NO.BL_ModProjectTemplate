using TMPro;
using UnityEngine;

namespace NuclearOption.UI
{
	public class CategoryTitle : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI title;

		public string Text
		{
			get
			{
				return title.text;
			}
			set
			{
				title.text = value;
			}
		}

		public CategoryTitle(string titleText)
		{
			title.text = titleText;
		}
	}
}
