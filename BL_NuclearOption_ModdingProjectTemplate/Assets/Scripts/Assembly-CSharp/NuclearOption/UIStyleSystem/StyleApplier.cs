using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	public abstract class StyleApplier<T> : StyleApplierBase where T : class
	{
		[SerializeField]
		protected StyleLabel styleLabel;

		private bool isInitialized;

		private void Start()
		{
			if (styleLabel == null)
			{
				Debug.LogWarning("StyleLabel is null on " + base.gameObject.name);
			}
			ThemeManager.ThemeGroupChanged += Apply;
			Apply();
			isInitialized = true;
		}

		private void OnDestroy()
		{
			if (isInitialized)
			{
				ThemeManager.ThemeGroupChanged -= Apply;
			}
		}

		protected abstract void Apply();

		protected T GetStyle()
		{
			Theme theme = ((!(ThemeManager.Active == null)) ? (Context switch
			{
				ThemeManager.ThemeContext.Menu => ThemeManager.Active.MenuTheme, 
				ThemeManager.ThemeContext.TacScreen => ThemeManager.Active.TacScreenTheme, 
				ThemeManager.ThemeContext.HUD => ThemeManager.Active.HudTheme, 
				_ => null, 
			}) : null);
			if ((object)theme == null)
			{
				return null;
			}
			return theme.GetStyle<T>(styleLabel);
		}
	}
}
