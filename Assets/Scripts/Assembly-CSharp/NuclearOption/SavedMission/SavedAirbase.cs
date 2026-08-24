using System;
using System.Collections.Generic;
using System.Linq;
using JamesFrowen.ScriptableVariables;
using RoadPathfinding;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedAirbase : ISaveableReference, IHasFaction, IHasPlacementType
	{
		private static readonly string noNameStr = "<no name>".AddColor(new Color(0.5f, 0.5f, 0.5f));

		public static readonly string builtInStr = "(built in) ".AddColor(new Color(0.8f, 0.8f, 1f)).AddSize(0.7f);

		public static readonly string overrideStr = "(override) ".AddColor(new Color(0.8f, 1f, 0.8f)).AddSize(0.7f);

		private static readonly string attachedStr = "(attached) ".AddColor(new Color(1f, 0.8f, 0.8f)).AddSize(0.7f);

		[HideInInspector]
		public bool IsOverride;

		[FactionField]
		public string faction = "";

		public string UniqueName = "";

		public string DisplayName = "";

		public bool Disabled;

		public bool Capturable = true;

		public float CaptureDefense = 10f;

		public float CaptureRange = 1000f;

		[HideInInspector]
		public GlobalPosition Center;

		[HideInInspector]
		public GlobalPosition SelectionPosition;

		[SavedBuildingField]
		[ReadOnly]
		[Tooltip("Edit MapTower instead")]
		public string Tower = "";

		[HideInInspector]
		public List<GlobalPosition> VerticalLandingPoints = new List<GlobalPosition>();

		[HideInInspector]
		public List<GlobalPosition> ServicePoints = new List<GlobalPosition>();

		[HideInInspector]
		public RoadNetwork roads = new RoadNetwork();

		[HideInInspector]
		public List<SavedRunway> runways = new List<SavedRunway>();

		[NonSerialized]
		public SavedBuilding TowerRef;

		[NonSerialized]
		public readonly List<SavedBuilding> BuildingsRef = new List<SavedBuilding>();

		[NonSerialized]
		public Airbase Airbase;

		[NonSerialized]
		public bool SavedInMission;

		[NonSerialized]
		public ValueWrapperGlobalPosition CenterWrapper = new ValueWrapperGlobalPosition();

		[NonSerialized]
		public ValueWrapperGlobalPosition SelectionPositionWrapper = new ValueWrapperGlobalPosition();

		[NonSerialized]
		public List<ValueWrapperGlobalPosition> VerticalLandingPointsWrappers = new List<ValueWrapperGlobalPosition>();

		[NonSerialized]
		public List<ValueWrapperGlobalPosition> ServicePointsWrappers = new List<ValueWrapperGlobalPosition>();

		string IHasFaction.FactionName => faction;

		string ISaveableReference.UniqueName => UniqueName;

		bool ISaveableReference.Destroyed { get; set; }

		bool ISaveableReference.CanBeReference => true;

		bool ISaveableReference.CanBeSorted => false;

		PlacementType IHasPlacementType.PlacementType => CalculatePlacementType();

		bool IHasPlacementType.CanBeAttached => true;

		public event RenamedDelegate OnRenamed;

		public bool IsAttached()
		{
			if (Airbase != null)
			{
				return Airbase.AttachedAirbase;
			}
			return UniqueName.StartsWith("<UNIT_AIRBASE>++");
		}

		public bool TryGetAirbase(out Airbase airbase)
		{
			if (Airbase != null)
			{
				airbase = Airbase;
				return true;
			}
			if (string.IsNullOrEmpty(UniqueName))
			{
				airbase = null;
				return false;
			}
			return FactionRegistry.airbaseLookup.TryGetValue(UniqueName, out airbase);
		}

		public SavedAirbase()
		{
			AfterLoadEditor(null, null);
		}

		public static SavedAirbase CreateOverride(SavedAirbase other)
		{
			return new SavedAirbase(other)
			{
				IsOverride = true
			};
		}

		private SavedAirbase(SavedAirbase other)
		{
			AfterLoadEditor(null, null);
			faction = other.faction;
			UniqueName = other.UniqueName;
			DisplayName = other.DisplayName;
			Disabled = other.Disabled;
			Capturable = other.Capturable;
			CaptureDefense = other.CaptureDefense;
			CaptureRange = other.CaptureRange;
			Center = other.Center;
			SelectionPosition = other.SelectionPosition;
			Tower = other.Tower;
			TowerRef = other.TowerRef;
			BuildingsRef = new List<SavedBuilding>(other.BuildingsRef);
			CenterWrapper = other.CenterWrapper;
			SelectionPositionWrapper = other.SelectionPositionWrapper;
		}

		public void Rename(string newName)
		{
			if (!(UniqueName == newName))
			{
				string uniqueName = UniqueName;
				UniqueName = newName;
				this.OnRenamed?.Invoke(this, uniqueName, newName);
			}
		}

		public void BeforeSave()
		{
			Tower = ((TowerRef != null) ? TowerRef.UniqueName : "");
			VerticalLandingPoints.Clear();
			foreach (ValueWrapperGlobalPosition verticalLandingPointsWrapper in VerticalLandingPointsWrappers)
			{
				VerticalLandingPoints.Add(verticalLandingPointsWrapper.Value);
			}
			ServicePoints.Clear();
			foreach (ValueWrapperGlobalPosition servicePointsWrapper in ServicePointsWrappers)
			{
				ServicePoints.Add(servicePointsWrapper.Value);
			}
		}

		public void AfterLoadEditor(Airbase buildInAirbase, IReadOnlyList<SavedBuilding> allBuildings)
		{
			CenterWrapper.SetValue(Center, this);
			SelectionPositionWrapper.SetValue(SelectionPosition, this);
			CenterWrapper.RegisterOnChange(this, delegate(GlobalPosition v)
			{
				Center = v;
			});
			SelectionPositionWrapper.RegisterOnChange(this, delegate(GlobalPosition v)
			{
				SelectionPosition = v;
			});
			if (buildInAirbase != null)
			{
				TowerRef = ((buildInAirbase.MapTower != null) ? ((SavedBuilding)buildInAirbase.MapTower.SavedUnit) : null);
				TowerRef?.SetAirbase(this);
			}
			else if (allBuildings != null)
			{
				if (!string.IsNullOrEmpty(Tower))
				{
					SavedBuilding savedBuilding = allBuildings.FirstOrDefault((SavedBuilding b) => b.UniqueName == Tower);
					if (savedBuilding != null)
					{
						TowerRef = savedBuilding;
						TowerRef.SetAirbase(this);
					}
					else
					{
						Debug.LogWarning("Failed to find tower with named " + Tower);
					}
				}
				else
				{
					TowerRef = null;
				}
			}
			VerticalLandingPointsWrappers.Clear();
			foreach (GlobalPosition verticalLandingPoint in VerticalLandingPoints)
			{
				VerticalLandingPointsWrappers.Add(new ValueWrapperGlobalPosition(verticalLandingPoint));
			}
			ServicePointsWrappers.Clear();
			foreach (GlobalPosition servicePoint in ServicePoints)
			{
				ServicePointsWrappers.Add(new ValueWrapperGlobalPosition(servicePoint));
			}
		}

		public string ToUIString(bool oneLine = false)
		{
			string text = UIStringFirstLine();
			if (oneLine)
			{
				return text;
			}
			string text2 = ((!(Airbase != null)) ? FactionHelper.ToUIString(faction) : FactionHelper.ToUIString(Airbase.CurrentHQ));
			return text + "\nFaction:" + text2;
		}

		private string UIStringFirstLine()
		{
			string text = (string.IsNullOrEmpty(DisplayName) ? noNameStr : DisplayName);
			string text2 = UniqueName.AddColor(new Color(0.7f, 0.7f, 0.7f));
			if (Airbase != null)
			{
				if (Airbase.AttachedAirbase && IsOverride)
				{
					return attachedStr + overrideStr + text;
				}
				if (Airbase.AttachedAirbase)
				{
					return attachedStr + text;
				}
				if (IsOverride)
				{
					return overrideStr + text + " - [" + text2 + "]";
				}
				if (Airbase.BuiltIn)
				{
					return builtInStr + text + " - [" + text2 + "]";
				}
			}
			return text + " - [" + text2 + "]";
		}

		private PlacementType CalculatePlacementType()
		{
			if (Airbase != null)
			{
				if (Airbase.AttachedAirbase)
				{
					return PlacementType.Attached;
				}
				if (IsOverride)
				{
					return PlacementType.Override;
				}
				if (Airbase.BuiltIn)
				{
					return PlacementType.BuiltIn;
				}
			}
			return PlacementType.Custom;
		}
	}
}
