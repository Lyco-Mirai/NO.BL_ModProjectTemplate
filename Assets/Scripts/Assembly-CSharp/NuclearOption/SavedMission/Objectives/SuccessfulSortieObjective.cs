using System.Collections.Generic;
using Mirage;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class SuccessfulSortieObjective : Objective
	{
		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("SuccessfulSortieObjectiveUpdateAndCheck");

		private const float NETWORK_FACTOR = 0.1f;

		private readonly ValueWrapperFloat minimumScore = new ValueWrapperFloat();

		private readonly ValueWrapperBool additive = new ValueWrapperBool();

		private NetworkWorld world;

		private float score;

		private readonly List<int> networkList = new List<int>();

		public override string FactionLabelOverride => "Both";

		public override float CompletePercent
		{
			get
			{
				if (minimumScore.Value == 0f)
				{
					return (score > 0f) ? 1 : 0;
				}
				return score / minimumScore.Value;
			}
		}

		public SuccessfulSortieSavedObjective Saved => (SuccessfulSortieSavedObjective)SavedObjective;

		public SuccessfulSortieObjective(SuccessfulSortieSavedObjective savedObjective)
			: base(savedObjective)
		{
		}

		public override void ReceiveNetworkData(List<int> data)
		{
			if (data.Count > 0)
			{
				score = (float)data[0] * 0.1f;
			}
		}

		private void UpdateNetworkData()
		{
			if (MissionManager.IsServer)
			{
				networkList.Clear();
				networkList.Add((int)(score / 0.1f));
				MissionManager.UpdateNetworkData(this, networkList);
			}
		}

		public override void CopyFrom(Objective original)
		{
			base.CopyFrom(original);
			SuccessfulSortieObjective successfulSortieObjective = (SuccessfulSortieObjective)original;
			minimumScore.SetValue(successfulSortieObjective.minimumScore.Value, this);
			additive.SetValue(successfulSortieObjective.additive.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			minimumScore.SetValue(Saved.minimumScore, this);
			additive.SetValue(Saved.additive, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.minimumScore = minimumScore.Value;
			Saved.additive = additive.Value;
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void OnStart()
		{
			if (MissionManager.IsServer)
			{
				world = MissionManager.Server.World;
				world.AddAndInvokeOnSpawn(OnSpawn);
			}
		}

		public override void Cleanup()
		{
			if (world != null)
			{
				world.onSpawn -= OnSpawn;
			}
		}

		private void OnSpawn(NetworkIdentity identity)
		{
			if (identity.TryGetComponent<Aircraft>(out var component) && component.Player != null && (base.FactionHQ == null || component.NetworkHQ == base.FactionHQ))
			{
				component.onSortieSuccessful += Aircraft_onSortieSuccessful;
			}
		}

		private void Aircraft_onSortieSuccessful(float score)
		{
			Debug.LogWarning($"[CrashAircraftObjective] sortie = {score}");
			if (additive.Value)
			{
				this.score += score;
				UpdateNetworkData();
			}
			else if (score > this.score)
			{
				this.score = score;
				UpdateNetworkData();
			}
		}

		public override void ClientOnlyUpdate()
		{
		}

		public override bool UpdateAndCheck()
		{
			using (updateAndCheckMarker.Auto())
			{
				if (minimumScore.Value == 0f)
				{
					return true;
				}
				return score >= minimumScore.Value;
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab).Setup("Minimum Score", minimumScore);
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Additive", additive);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphFloatFieldData
			{
				PinId = new PinId("Minimum Score"),
				DisplayName = "Minimum Score",
				ValueWrapper = minimumScore
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Additive"),
				DisplayName = "Additive",
				ValueWrapper = additive
			});
		}
	}
}
