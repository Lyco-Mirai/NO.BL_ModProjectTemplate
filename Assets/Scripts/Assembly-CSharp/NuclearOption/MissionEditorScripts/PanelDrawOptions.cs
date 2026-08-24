using System;
using NuclearOption.SavedMission;

namespace NuclearOption.MissionEditorScripts
{
	public struct PanelDrawOptions
	{
		public PanelDrawContext Context;

		public Action<ISaveableReference> OnRequestSelectAndFocus;

		private PanelDrawOptions(PanelDrawContext context, Action<ISaveableReference> onRequestSelectAndFocus)
		{
			Context = context;
			OnRequestSelectAndFocus = onRequestSelectAndFocus;
		}

		public static PanelDrawOptions Standard()
		{
			return default(PanelDrawOptions);
		}

		public static PanelDrawOptions Graph(Action<ISaveableReference> onRequestSelectAndFocus)
		{
			return new PanelDrawOptions(PanelDrawContext.GraphEditor, onRequestSelectAndFocus);
		}
	}
}
