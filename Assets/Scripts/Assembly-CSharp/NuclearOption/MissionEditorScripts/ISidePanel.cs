namespace NuclearOption.MissionEditorScripts
{
	public interface ISidePanel
	{
		SidePanel Panel { get; set; }

		void PanelRefresh();
	}
}
