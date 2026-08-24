using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class SidePanel
	{
		private readonly EditorTabs editorTabs;

		public SidePanel Child;

		public SidePanel Parent;

		private GameObject panelGO;

		private ISidePanel panel;

		private bool hasPanel;

		public SidePanel(EditorTabs editorTabs, ISidePanel startingPanel = null)
		{
			this.editorTabs = editorTabs;
			panel = startingPanel;
		}

		public T Create<T>(T prefab) where T : Component, ISidePanel
		{
			Destroy();
			T val = (T)(panel = editorTabs.AddPanel(prefab));
			panelGO = val.gameObject;
			val.Panel = this;
			hasPanel = true;
			return val;
		}

		public void Refresh()
		{
			panel.PanelRefresh();
		}

		public void Destroy()
		{
			if (hasPanel)
			{
				Child?.Destroy();
				editorTabs.DestroyPanel(panelGO);
				hasPanel = false;
			}
		}
	}
}
