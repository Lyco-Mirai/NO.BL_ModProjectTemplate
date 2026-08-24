using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using Unity.Profiling;

namespace NuclearOption.SavedMission.Objectives
{
	public class DialogueBoxObjective : Objective
	{
		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("DialogueBoxObjectiveUpdateAndCheck");

		private string title;

		private string body;

		private string button;

		private readonly ValueWrapperBool factionOnly = new ValueWrapperBool();

		private readonly ValueWrapperDelegate<string> titleWrapper;

		private readonly ValueWrapperDelegate<string> buttonWrapper;

		private bool dialogueComplete;

		public override bool NeedsFaction => factionOnly.Value;

		public override float CompletePercent => 0f;

		public DialogueBoxSavedObjective Saved => (DialogueBoxSavedObjective)SavedObjective;

		public DialogueBoxObjective(DialogueBoxSavedObjective savedObjective)
			: base(savedObjective)
		{
			titleWrapper = new ValueWrapperDelegate<string>(() => title, delegate(string v)
			{
				title = v;
			});
			buttonWrapper = new ValueWrapperDelegate<string>(() => button, delegate(string v)
			{
				button = v;
			});
		}

		public override void CopyFrom(Objective original)
		{
			base.CopyFrom(original);
			DialogueBoxObjective dialogueBoxObjective = (DialogueBoxObjective)original;
			title = dialogueBoxObjective.title;
			body = dialogueBoxObjective.body;
			button = dialogueBoxObjective.button;
			factionOnly.SetValue(dialogueBoxObjective.factionOnly.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			title = Saved.title;
			body = Saved.body;
			button = Saved.button;
			factionOnly.SetValue(Saved.factionOnly, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.title = title;
			Saved.body = body;
			Saved.button = button;
			Saved.factionOnly = factionOnly.Value;
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void OnStart()
		{
			if (MissionManager.IsServer)
			{
				FactionHQ filterFaction = ((!factionOnly) ? null : base.FactionHQ);
				NetworkSceneSingleton<MissionMessages>.i.ShowDialogue(title, body, button, filterFaction, delegate
				{
					dialogueComplete = true;
				});
			}
		}

		public override void ClientOnlyUpdate()
		{
		}

		public override bool UpdateAndCheck()
		{
			using (updateAndCheckMarker.Auto())
			{
				return dialogueComplete;
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.InstantiateWithParent(drawer.Prefabs.StringFieldPrefab).Setup("Title", title, delegate(string v)
			{
				title = v;
			});
			drawer.InstantiateWithParent(drawer.Prefabs.StringFieldPrefab).Setup("Body", body, delegate(string v)
			{
				body = v;
			}, 5);
			drawer.InstantiateWithParent(drawer.Prefabs.StringFieldPrefab).Setup("Ok Button", button, delegate(string v)
			{
				button = v;
			});
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Faction Only", factionOnly);
			drawer.TrackChanges(factionOnly);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Faction Only"),
				DisplayName = "Faction Only",
				ValueWrapper = factionOnly
			});
			data.InputElements.Add(new GraphStringFieldData
			{
				PinId = new PinId("Title"),
				DisplayName = "Title",
				ValueWrapper = titleWrapper
			});
			data.InputElements.Add(new GraphStringFieldData
			{
				PinId = new PinId("Ok Button"),
				DisplayName = "Ok Button",
				ValueWrapper = buttonWrapper
			});
		}
	}
}
