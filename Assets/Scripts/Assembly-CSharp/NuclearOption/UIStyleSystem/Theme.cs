using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	[CreateAssetMenu(fileName = "NewTheme", menuName = "UI Style System/Theme")]
	public class Theme : ScriptableObject
	{
		[Serializable]
		public class TextStyleItem
		{
			public StyleLabel Label;

			public string labelName;

			public TextStyle Style;

			public void SetLabelName()
			{
				labelName = Label.name;
			}
		}

		[Serializable]
		public class ImageStyleItem
		{
			public StyleLabel Label;

			public string labelName;

			public ImageStyle Style;

			public void SetLabelName()
			{
				labelName = Label.name;
			}
		}

		[Serializable]
		public class ButtonStyleItem
		{
			public StyleLabel Label;

			public string labelName;

			public ButtonStyle Style;

			public void SetLabelName()
			{
				labelName = Label.name;
			}
		}

		[SerializeField]
		private string id;

		public List<TextStyleItem> TextStyles;

		public List<ImageStyleItem> ImageStyles;

		public List<ButtonStyleItem> ButtonStyles;

		public string Id
		{
			get
			{
				return id;
			}
			set
			{
				id = value;
			}
		}

		public T GetStyle<T>(StyleLabel label) where T : class
		{
			Type typeFromHandle = typeof(T);
			if (typeFromHandle == typeof(TextStyle))
			{
				foreach (TextStyleItem textStyle in TextStyles)
				{
					if (textStyle.Label == label)
					{
						return (T)(object)textStyle.Style;
					}
				}
				throw new KeyNotFoundException($"Did not find TextStyle with Label:{label} in {this}");
			}
			if (typeFromHandle == typeof(ImageStyle))
			{
				foreach (ImageStyleItem imageStyle in ImageStyles)
				{
					if (imageStyle.Label == label)
					{
						return (T)(object)imageStyle.Style;
					}
				}
				throw new KeyNotFoundException($"Did not find ImageStyle with Label:{label} in {this}");
			}
			if (typeFromHandle == typeof(ButtonStyle))
			{
				foreach (ButtonStyleItem buttonStyle in ButtonStyles)
				{
					if (buttonStyle.Label == label)
					{
						return (T)(object)buttonStyle.Style;
					}
				}
				throw new KeyNotFoundException($"Did find ButtonStyle with Label:{label} in {this}");
			}
			throw new NotSupportedException($"No styles for {typeFromHandle}");
		}

		private void SetLabelNames()
		{
			foreach (TextStyleItem textStyle in TextStyles)
			{
				textStyle.SetLabelName();
			}
			foreach (ImageStyleItem imageStyle in ImageStyles)
			{
				imageStyle.SetLabelName();
			}
			foreach (ButtonStyleItem buttonStyle in ButtonStyles)
			{
				buttonStyle.SetLabelName();
			}
		}

		private void SetLabelsFromNames(ThemeManager.ThemeContext context)
		{
			TextStyles.RemoveAll((TextStyleItem item) => !ThemeManager.StyleLabels[context].ContainsKey(item.labelName));
			foreach (TextStyleItem textStyle in TextStyles)
			{
				textStyle.Label = ThemeManager.StyleLabels[context][textStyle.labelName];
			}
			ImageStyles.RemoveAll((ImageStyleItem item) => !ThemeManager.StyleLabels[context].ContainsKey(item.labelName));
			foreach (ImageStyleItem imageStyle in ImageStyles)
			{
				imageStyle.Label = ThemeManager.StyleLabels[context][imageStyle.labelName];
			}
			ButtonStyles.RemoveAll((ButtonStyleItem item) => !ThemeManager.StyleLabels[context].ContainsKey(item.labelName));
			foreach (ButtonStyleItem buttonStyle in ButtonStyles)
			{
				buttonStyle.Label = ThemeManager.StyleLabels[context][buttonStyle.labelName];
			}
		}

		private void PopulateMissingStyles(ThemeManager.ThemeContext context)
		{
			Theme theme = context switch
			{
				ThemeManager.ThemeContext.HUD => ThemeManager.Vanilla.HudTheme, 
				ThemeManager.ThemeContext.TacScreen => ThemeManager.Vanilla.TacScreenTheme, 
				ThemeManager.ThemeContext.Menu => ThemeManager.Vanilla.MenuTheme, 
				_ => throw new ArgumentOutOfRangeException("context", context, null), 
			};
			Debug.Log($"Populating missing styles for {context}");
			foreach (TextStyleItem style in theme.TextStyles)
			{
				if (!TextStyles.Exists((TextStyleItem s) => s.Label == style.Label))
				{
					TextStyles.Add(new TextStyleItem
					{
						Label = style.Label,
						Style = style.Style
					});
				}
			}
			foreach (ImageStyleItem style2 in theme.ImageStyles)
			{
				if (!ImageStyles.Exists((ImageStyleItem s) => s.Label == style2.Label))
				{
					ImageStyles.Add(new ImageStyleItem
					{
						Label = style2.Label,
						Style = style2.Style
					});
				}
			}
			foreach (ButtonStyleItem style3 in theme.ButtonStyles)
			{
				if (!ButtonStyles.Exists((ButtonStyleItem s) => s.Label == style3.Label))
				{
					ButtonStyles.Add(new ButtonStyleItem
					{
						Label = style3.Label,
						Style = style3.Style
					});
				}
			}
		}

		public List<Tuple<string, Color, string>> GetColors()
		{
			List<Tuple<string, Color, string>> list = new List<Tuple<string, Color, string>>();
			foreach (TextStyleItem textStyle in TextStyles)
			{
				list.Add(new Tuple<string, Color, string>(textStyle.Label.displayLabel, textStyle.Style.Color, textStyle.Label.tooltip));
			}
			foreach (ImageStyleItem imageStyle in ImageStyles)
			{
				list.Add(new Tuple<string, Color, string>(imageStyle.Label.displayLabel, imageStyle.Style.Color, imageStyle.Label.tooltip));
			}
			foreach (ButtonStyleItem buttonStyle in ButtonStyles)
			{
				list.Add(new Tuple<string, Color, string>(buttonStyle.Label.displayLabel, buttonStyle.Style.Color, buttonStyle.Label.tooltip));
			}
			return list;
		}

		public void SetColors(List<Color> colors)
		{
			int num = TextStyles.Count + ImageStyles.Count + ButtonStyles.Count;
			if (colors.Count != num)
			{
				throw new ArgumentException($"Number of colors must match number of styles {colors.Count}/{num} for Theme");
			}
			for (int i = 0; i < TextStyles.Count; i++)
			{
				TextStyles[i].Style.Color = colors[i].WithAlpha(0f);
			}
			for (int j = 0; j < ImageStyles.Count; j++)
			{
				ImageStyles[j].Style.Color = colors[j + TextStyles.Count].WithAlpha(0f);
			}
			for (int k = 0; k < ButtonStyles.Count; k++)
			{
				ButtonStyles[k].Style.Color = colors[k + TextStyles.Count + ImageStyles.Count].WithAlpha(0f);
			}
		}

		[ContextMenu("Save to JSON")]
		public void SaveToJson(string context = "")
		{
			string text = Application.persistentDataPath + "/themes/" + ThemeManager.Active.name;
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string path = text + "/" + context + ".json";
			SetLabelNames();
			string contents = JsonUtility.ToJson(this, prettyPrint: true);
			File.WriteAllText(path, contents);
			ColorLog<Theme>.Info("Successfully saved : " + context + ".json");
		}

		public static Theme LoadFromJson(string jsonPath, ThemeManager.ThemeContext context)
		{
			if (!File.Exists(jsonPath))
			{
				throw new FileNotFoundException(jsonPath + " is not a valid theme.");
			}
			string json = File.ReadAllText(jsonPath);
			Theme theme = ScriptableObject.CreateInstance<Theme>();
			JsonUtility.FromJsonOverwrite(json, theme);
			if (theme == null)
			{
				throw new Exception("Failed to deserialize JSON : " + jsonPath);
			}
			theme.name = theme.id;
			theme.SetLabelsFromNames(context);
			theme.PopulateMissingStyles(context);
			return theme;
		}
	}
}
