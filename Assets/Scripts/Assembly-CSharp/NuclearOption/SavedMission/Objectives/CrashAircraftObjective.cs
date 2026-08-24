using System.Collections.Generic;
using Mirage;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class CrashAircraftObjective : Objective
	{
		private static readonly ProfilerMarker updateAndCheckMarker = new ProfilerMarker("CrashAircraftObjectiveUpdateAndCheck");

		private readonly ValueWrapperInt livesPerPlayer = new ValueWrapperInt();

		private readonly ValueWrapperInt extraLives = new ValueWrapperInt();

		private readonly ValueWrapperBool includeDestroy = new ValueWrapperBool();

		private readonly ValueWrapperBool includeEject = new ValueWrapperBool();

		private int crashes;

		private NetworkWorld world;

		private readonly List<int> networkList = new List<int>();

		public override string FactionLabelOverride => "Both";

		public override float CompletePercent
		{
			get
			{
				int totalLives = GetTotalLives();
				if (totalLives == 0)
				{
					return 0f;
				}
				return crashes / totalLives;
			}
		}

		public CrashAircraftSavedObjective Saved => (CrashAircraftSavedObjective)SavedObjective;

		public CrashAircraftObjective(CrashAircraftSavedObjective savedObjective)
			: base(savedObjective)
		{
		}

		private int GetTotalLives()
		{
			return (int)extraLives + (int)livesPerPlayer * MissionManager.Server.AuthenticatedPlayers.Count;
		}

		public override void ReceiveNetworkData(List<int> data)
		{
			if (data.Count > 0)
			{
				crashes = data[0];
			}
		}

		private void UpdateNetworkData()
		{
			if (MissionManager.IsServer)
			{
				networkList.Clear();
				networkList.Add(crashes);
				MissionManager.UpdateNetworkData(this, networkList);
			}
		}

		public override void CopyFrom(Objective original)
		{
			base.CopyFrom(original);
			CrashAircraftObjective crashAircraftObjective = (CrashAircraftObjective)original;
			livesPerPlayer.SetValue(crashAircraftObjective.livesPerPlayer.Value, this);
			extraLives.SetValue(crashAircraftObjective.extraLives.Value, this);
			includeDestroy.SetValue(crashAircraftObjective.includeDestroy.Value, this);
			includeEject.SetValue(crashAircraftObjective.includeEject.Value, this);
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			livesPerPlayer.SetValue(Saved.livesPerPlayer, this);
			extraLives.SetValue(Saved.extraLives, this);
			includeDestroy.SetValue(Saved.includeDestroy, this);
			includeEject.SetValue(Saved.includeEject, this);
		}

		public override void Save()
		{
			base.Save();
			Saved.livesPerPlayer = livesPerPlayer.Value;
			Saved.extraLives = extraLives.Value;
			Saved.includeDestroy = includeDestroy.Value;
			Saved.includeEject = includeEject.Value;
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
			if (!identity.TryGetComponent<Aircraft>(out var component) || !(component.Player != null) || (!(base.FactionHQ == null) && !(component.NetworkHQ == base.FactionHQ)))
			{
				return;
			}
			Aircraft a = component;
			if (includeDestroy.Value)
			{
				component.onDisableUnit += Aircraft_onDisableUnit;
			}
			if (includeEject.Value)
			{
				component.onEject += delegate
				{
					Aircraft_onEject(a);
				};
			}
		}

		private void Aircraft_onDisableUnit(Unit obj)
		{
			Aircraft aircraft = (Aircraft)obj;
			DebugWhere("Disable", aircraft);
			if (!LandingNearAirbase(aircraft))
			{
				crashes++;
				UpdateNetworkData();
			}
		}

		private void Aircraft_onEject(Aircraft aircraft)
		{
			DebugWhere("Eject", aircraft);
			crashes++;
			UpdateNetworkData();
		}

		private void DebugWhere(string what, Aircraft aircraft)
		{
			bool flag = aircraft.IsLanded();
			bool flag2 = NearAirbase(aircraft);
			Debug.LogWarning($"[CrashAircraftObjective] {what} landed:{flag} nearAirbase:{flag2}");
		}

		private bool LandingNearAirbase(Aircraft aircraft)
		{
			if (!aircraft.IsLanded())
			{
				return false;
			}
			return NearAirbase(aircraft);
		}

		private bool NearAirbase(Aircraft aircraft)
		{
			Airbase airbase;
			if (aircraft.NetworkHQ != null)
			{
				return aircraft.NetworkHQ.AnyNearAirbase(aircraft.transform.position, out airbase);
			}
			return false;
		}

		public override void ClientOnlyUpdate()
		{
		}

		public override bool UpdateAndCheck()
		{
			using (updateAndCheckMarker.Auto())
			{
				int totalLives = GetTotalLives();
				return crashes >= totalLives;
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab).Setup("Number of Crashes", extraLives);
			drawer.InstantiateWithParent(drawer.Prefabs.FloatFieldPrefab).Setup("Extra Crash per player", livesPerPlayer);
			drawer.DrawHeader("Trigger Conditions");
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Destroy", includeDestroy);
			drawer.InstantiateWithParent(drawer.Prefabs.BoolFieldPrefab).Setup("Eject", includeEject);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphIntFieldData
			{
				PinId = new PinId("Number of Crashes"),
				DisplayName = "Number of Crashes",
				ValueWrapper = extraLives
			});
			data.InputElements.Add(new GraphIntFieldData
			{
				PinId = new PinId("Extra Crash per player"),
				DisplayName = "Extra Crash per player",
				ValueWrapper = livesPerPlayer
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Destroy"),
				DisplayName = "Destroy",
				ValueWrapper = includeDestroy
			});
			data.InputElements.Add(new GraphBoolFieldData
			{
				PinId = new PinId("Eject"),
				DisplayName = "Eject",
				ValueWrapper = includeEject
			});
		}
	}
}
