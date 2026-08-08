using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	[CreateAssetMenu(fileName = "NewThemeGroup", menuName = "UI Style System/Theme Group")]
	public class ThemeGroup : ScriptableObject
	{
		public enum ThemeOrigin
		{
			Scriptable_Object = 0,
			JSON = 1
		}

		[SerializeField]
		private string id;

		[Header("Color Theme")]
		[SerializeField]
		private ColorTheme colorTheme;

		[Header("Menu Theme")]
		[SerializeField]
		private Theme menuTheme;

		[Header("TacScreen Theme")]
		[SerializeField]
		private Theme tacScreenTheme;

		[Header("HUD Theme")]
		[SerializeField]
		private Theme hudTheme;

		[Header("Origin")]
		[SerializeField]
		private ThemeOrigin themeOrigin;

		public string Id => id;

		public ColorTheme ColorTheme => colorTheme;

		public Theme MenuTheme => menuTheme;

		public Theme TacScreenTheme => tacScreenTheme;

		public Theme HudTheme => hudTheme;

		public ThemeOrigin Origin
		{
			get
			{
				return themeOrigin;
			}
			set
			{
				themeOrigin = value;
			}
		}

		public void SetThemeGroup(string newId, string newName, ColorTheme newColorTheme, Theme newMenuTheme, Theme newTacScreenTheme, Theme newHudTheme, ThemeOrigin newThemeOrigin = ThemeOrigin.JSON)
		{
			id = newId;
			base.name = newName;
			colorTheme = newColorTheme;
			menuTheme = newMenuTheme;
			tacScreenTheme = newTacScreenTheme;
			hudTheme = newHudTheme;
			themeOrigin = newThemeOrigin;
		}
	}
}
