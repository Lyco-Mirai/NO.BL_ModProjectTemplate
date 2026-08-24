using System;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public static class SmartDateFormatter
	{
		[Serializable]
		public struct ItemStyle
		{
			public Override<Color> Color;

			public Override<float> Size;

			public ItemStyle(Color? color, float? size = null)
			{
				Color = new Override<Color>(color.HasValue, color.GetValueOrDefault());
				Size = new Override<float>(size.HasValue, size.GetValueOrDefault());
			}
		}

		[Serializable]
		public struct Theme
		{
			public ItemStyle Time;

			public ItemStyle Days;

			public ItemStyle Date;

			public ItemStyle Year;

			public static Theme Default => new Theme(default(ItemStyle), new ItemStyle(new Color(0.8f, 0.8f, 0.8f), 0.8f), new ItemStyle(new Color(0.8f, 0.8f, 0.8f), 0.9f), new ItemStyle(new Color(0.6f, 0.6f, 0.6f), 0.8f));

			public static Theme None => default(Theme);

			public Theme(ItemStyle time, ItemStyle days, ItemStyle date, ItemStyle year)
			{
				Time = time;
				Days = days;
				Date = date;
				Year = year;
			}
		}

		public static string ToSmartDate(DateTime? time, Theme? theme = null)
		{
			if (!time.HasValue)
			{
				return "";
			}
			return ToSmartDate(time.Value, theme);
		}

		public static string ToSmartDate(DateTime time, Theme? _theme = null)
		{
			Theme theme = _theme ?? Theme.Default;
			DateTime now = DateTime.Now;
			TimeSpan timeSpan = now - time;
			if (timeSpan.TotalHours < 24.0 && timeSpan.TotalMilliseconds > 0.0)
			{
				return Wrap((timeSpan.TotalHours < 1.0) ? $"{(int)timeSpan.TotalMinutes} minutes ago" : $"{(int)timeSpan.TotalHours} hours ago", theme.Time);
			}
			if (timeSpan.TotalDays < 30.0)
			{
				string text = Wrap(time.ToString("HH:mm"), theme.Time);
				string text2 = Wrap($"{(int)timeSpan.TotalDays,2} days ago", theme.Days);
				return text + " " + text2;
			}
			if (time.Year == now.Year)
			{
				string text3 = Wrap(time.ToString("HH:mm"), theme.Time);
				string text4 = Wrap(time.ToString("dd MMM"), theme.Date);
				return text3 + " " + text4;
			}
			string text5 = Wrap(time.ToString("MMM dd"), theme.Date);
			string text6 = Wrap(time.Year.ToString(), theme.Year);
			return text5 + " " + text6;
		}

		private static string Wrap(string text, ItemStyle style)
		{
			if (style.Color.IsOverride)
			{
				text = text.AddColor(style.Color.Value);
			}
			if (style.Size.IsOverride)
			{
				text = text.AddSize(style.Size.Value);
			}
			return text;
		}
	}
}
