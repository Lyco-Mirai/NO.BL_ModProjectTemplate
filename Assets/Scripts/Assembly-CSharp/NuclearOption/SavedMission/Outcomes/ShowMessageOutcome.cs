using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Outcomes
{
	public class ShowMessageOutcome : Outcome
	{
		public string Message;

		public readonly ValueWrapperBool PlaySound = new ValueWrapperBool();

		public readonly ValueWrapperBool ObjectiveFactionOnly = new ValueWrapperBool();

		private readonly ValueWrapperDelegate<string> messageWrapper;

		public ShowMessageSavedOutcome Saved => (ShowMessageSavedOutcome)SavedOutcome;

		public ShowMessageOutcome(ShowMessageSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
			messageWrapper = new ValueWrapperDelegate<string>(() => Message, delegate(string v)
			{
				Message = v;
			});
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			ShowMessageOutcome showMessageOutcome = (ShowMessageOutcome)original;
			Message = showMessageOutcome.Message;
			PlaySound.SetValue(showMessageOutcome.PlaySound.Value, this);
			ObjectiveFactionOnly.SetValue(showMessageOutcome.ObjectiveFactionOnly.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			Message = Saved.Message;
			PlaySound.SetValue(Saved.PlaySound, this);
			ObjectiveFactionOnly.SetValue(Saved.ObjectiveFactionOnly, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.Message = Message;
			Saved.PlaySound = PlaySound.Value;
			Saved.ObjectiveFactionOnly = ObjectiveFactionOnly.Value;
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void Complete(Objective completedObjective)
		{
			MissionMessages.ShowMessage(faction: (!ObjectiveFactionOnly) ? null : completedObjective.FactionHQ, message: Message, playsound: PlaySound, sendToClients: true);
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.InstantiateWithParent(drawer.Prefabs.StringFieldPrefab).Setup("Message", Message, delegate(string v)
			{
				Message = v;
			}, 5);
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Play Sound", PlaySound);
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Faction Only", ObjectiveFactionOnly);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Play Sound"),
				DisplayName = "Play Sound",
				ValueWrapper = PlaySound
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Faction Only"),
				DisplayName = "Faction Only",
				ValueWrapper = ObjectiveFactionOnly
			});
			data.InputElements.Add(new GraphStringFieldData
			{
				PinId = new PinId("Message"),
				DisplayName = "Message",
				ValueWrapper = messageWrapper
			});
		}
	}
}
