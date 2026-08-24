using TMPro;

namespace NuclearOption.MissionEditorScripts
{
	public static class InputFieldExtensions
	{
		public static void SetIfNotFocus(this TMP_InputField inputField, string value, bool withoutNotify = true)
		{
			if (!inputField.isFocused)
			{
				if (withoutNotify)
				{
					inputField.SetTextWithoutNotify(value);
				}
				else
				{
					inputField.text = value;
				}
			}
		}
	}
}
