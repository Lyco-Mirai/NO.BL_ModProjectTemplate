using NuclearOption.MissionEditorScripts;
using Unity.Profiling;

namespace NuclearOption.SavedMission.Objectives
{
	public class NoObjective : Objective
	{
		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("NoObjectiveUpdateAndCheck");

		public NoObjective(NoSavedObjective savedObjective)
			: base(savedObjective)
		{
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void OnStart()
		{
		}

		public override void ClientOnlyUpdate()
		{
		}

		public override bool UpdateAndCheck()
		{
			using (updateAndCheckMarker.Auto())
			{
				return true;
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.Nothing();
		}
	}
}
