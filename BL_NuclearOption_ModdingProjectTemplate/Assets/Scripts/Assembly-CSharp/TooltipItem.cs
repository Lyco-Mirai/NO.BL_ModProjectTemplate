using UnityEngine;
using UnityEngine.UI;

public class TooltipItem : MonoBehaviour
{
	public Image icon;

	public Text label;

	public Text value;

	public void Setup(Sprite image, Color? color, string? desc, int? number)
	{
		if (image != null)
		{
			icon.sprite = image;
			icon.color = (color.HasValue ? color.Value : Color.white);
			icon.enabled = true;
		}
		else
		{
			icon.enabled = false;
		}
		if (desc != null)
		{
			label.text = desc;
			label.color = (color.HasValue ? color.Value : Color.white);
			label.enabled = true;
		}
		else
		{
			label.enabled = false;
		}
		if (number.HasValue)
		{
			value.text = number.ToString();
			value.color = (color.HasValue ? color.Value : Color.white);
			value.enabled = true;
		}
		else
		{
			value.enabled = false;
		}
	}

	public void SetValue(int number)
	{
		value.enabled = true;
		value.text = number.ToString();
	}

	public void SetLabel(string text)
	{
		label.enabled = true;
		label.text = text;
	}

	public int GetValue()
	{
		if (!int.TryParse(value.text, out var result))
		{
			return 0;
		}
		return result;
	}

	public void AddValue(int number)
	{
		int num = int.Parse(value.text) + number;
		value.text = num.ToString();
	}

	public void SetColor(Color color)
	{
		icon.color = color;
		label.color = color;
		value.color = color;
	}
}
