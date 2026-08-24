using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	internal class WaitTimeObjective : Objective
	{
		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("WaitTimeObjectiveUpdateAndCheck");

		private const int NETWORK_ACCURACY = 10000;

		private readonly ValueWrapperFloat seconds = new ValueWrapperFloat();

		private float startTime;

		private float duration;

		public float Timer
		{
			get
			{
				if (GameManager.gameState != GameState.SinglePlayer)
				{
					return (float)MissionManager.NetworkTime.Time;
				}
				return Time.timeSinceLevelLoad;
			}
		}

		public override float CompletePercent
		{
			get
			{
				if (startTime == 0f)
				{
					return 0f;
				}
				return duration / (float)seconds;
			}
		}

		public WaitTimeSavedObjective Saved => (WaitTimeSavedObjective)SavedObjective;

		public WaitTimeObjective(WaitTimeSavedObjective savedObjective)
			: base(savedObjective)
		{
		}

		public override void ReceiveNetworkData(List<int> data)
		{
			int num = data[0];
			startTime = (float)num / 10000f;
		}

		public override void CopyFrom(Objective original)
		{
			base.CopyFrom(original);
			WaitTimeObjective waitTimeObjective = (WaitTimeObjective)original;
			seconds.SetValue(waitTimeObjective.seconds.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			seconds.SetValue(Saved.seconds, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.seconds = seconds.Value;
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void OnStart()
		{
			if (MissionManager.IsServer)
			{
				int num = (int)((startTime = Timer) * 10000f);
				if (startTime == 0f)
				{
					startTime = 0.0001f;
				}
				if (num == 0)
				{
					num = 1;
				}
				MissionManager.UpdateNetworkData(this, new List<int> { num });
			}
		}

		public override void ClientOnlyUpdate()
		{
			duration = Timer - startTime;
		}

		public override bool UpdateAndCheck()
		{
			using (updateAndCheckMarker.Auto())
			{
				duration = Timer - startTime;
				return duration > (float)seconds;
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab).Setup("Seconds", seconds);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Seconds"),
				DisplayName = "Seconds",
				ValueWrapper = seconds
			});
		}
	}
}
