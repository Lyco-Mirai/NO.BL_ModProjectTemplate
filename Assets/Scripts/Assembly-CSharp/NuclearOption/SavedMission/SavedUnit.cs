using System;
using Newtonsoft.Json;
using NuclearOption.Effects;
using UnityEngine;
using UnityEngine.Serialization;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public abstract class SavedUnit : ISaveableReference, IHasFaction, IHasPlacementType
	{
		public string type = "";

		public string faction = "";

		[SerializeField]
		[FormerlySerializedAs("UniqueName")]
		[FormerlySerializedAs("_uniqueName")]
		[JsonProperty("UniqueName")]
		public string private_uniqueName = "";

		public GlobalPosition globalPosition;

		public Quaternion rotation = Quaternion.identity;

		public Override<float> CaptureStrength;

		public Override<float> CaptureDefense;

		public SavedInventory inventory;

		[NonSerialized]
		public PlacementType PlacementType;

		[NonSerialized]
		public Unit Unit;

		[NonSerialized]
		public readonly ValueWrapperGlobalPosition PositionWrapper = new ValueWrapperGlobalPosition();

		[NonSerialized]
		public readonly ValueWrapperQuaternion RotationWrapper = new ValueWrapperQuaternion();

		[NonSerialized]
		public bool HasSpawned;

		public string UniqueName => private_uniqueName;

		string IHasFaction.FactionName => faction;

		string ISaveableReference.UniqueName => UniqueName;

		bool ISaveableReference.Destroyed { get; set; }

		bool ISaveableReference.CanBeReference => true;

		bool ISaveableReference.CanBeSorted => false;

		PlacementType IHasPlacementType.PlacementType => PlacementType;

		bool IHasPlacementType.CanBeAttached => false;

		public event RenamedDelegate OnRenamed;

		public SavedUnit()
		{
		}

		public SavedUnit(string uniqueName)
		{
			private_uniqueName = uniqueName;
		}

		public void AfterCreate(Unit unit)
		{
			PlacementType = PlacementType.Custom;
			UnitDefinition definition = unit.definition;
			type = definition.jsonKey;
			CaptureStrength = default(Override<float>);
			unit.NetworkUniqueName = UniqueName;
			if (unit.NetworkHQ != null)
			{
				faction = unit.NetworkHQ.faction.factionName;
			}
			SetPosition(unit.transform, this);
		}

		public virtual void AfterLoadEditor()
		{
			PositionWrapper.SetValue(globalPosition, this);
			RotationWrapper.SetValue(rotation, this);
			TerrainHeightMapBlocker blocker = null;
			bool hasBlocker = Unit != null && Unit.TryGetComponent<TerrainHeightMapBlocker>(out blocker);
			PositionWrapper.RegisterOnChange(this, delegate(GlobalPosition v)
			{
				globalPosition = v;
				if (Unit != null)
				{
					Unit.transform.position = v.ToLocalPosition();
				}
				if (hasBlocker && blocker != null)
				{
					blocker.EditorUpdatePosition();
				}
			});
			RotationWrapper.RegisterOnChange(this, delegate(Quaternion v)
			{
				rotation = v;
				if (Unit != null)
				{
					Unit.transform.rotation = v;
				}
				if (hasBlocker && blocker != null)
				{
					blocker.EditorUpdatePosition();
				}
			});
		}

		public virtual void AfterAddOverride(Unit unit)
		{
			SetOverrideDefaultValues(unit);
		}

		protected virtual void SetOverrideDefaultValues(Unit unit)
		{
			faction = ((unit.MapHQ != null) ? unit.MapHQ.faction.factionName : "");
			CaptureStrength = default(Override<float>);
			CaptureDefense = default(Override<float>);
		}

		public void Rename(string newName)
		{
			if (string.IsNullOrEmpty(newName))
			{
				ColorLog<SavedUnit>.LogError("Can't rename to null or empty string");
			}
			else if (!(private_uniqueName == newName))
			{
				string oldName = private_uniqueName;
				private_uniqueName = newName;
				this.OnRenamed?.Invoke(this, oldName, newName);
			}
		}

		public void SetPosition(Transform target, object source)
		{
			target.GetPositionAndRotation(out var position, out var quaternion);
			SetPosition(position.ToGlobalPosition(), quaternion, source);
		}

		public void SetPosition(GlobalPosition globalPosition, Quaternion rotation, object source)
		{
			this.globalPosition = globalPosition;
			this.rotation = rotation;
			PositionWrapper.SetValue(globalPosition, source);
			RotationWrapper.SetValue(rotation, source);
		}

		public string ToUIString(bool oneLine = false)
		{
			string text = UniqueName ?? "<no name>";
			if (text.StartsWith("<MAP_UNIT>++"))
			{
				text = text.Substring("<MAP_UNIT>++".Length);
			}
			string text2 = text.AddColor(new Color(0.7f, 0.7f, 0.7f));
			string obj = (string.IsNullOrEmpty(type) ? "<missing type>" : type);
			Color color = ColorLog.ColorFromName(obj, 0.2f, 1f);
			string text3 = obj.AddColor(color);
			string text4 = PlacementType switch
			{
				PlacementType.BuiltIn => SavedAirbase.builtInStr, 
				PlacementType.Override => SavedAirbase.overrideStr, 
				_ => "", 
			};
			string text5 = text4 + "[" + text2 + "] - Type:" + text3;
			if (oneLine)
			{
				return text5;
			}
			int num = SaveHelper.CountSpawnedBy(MissionManager.CurrentMission.RuntimeObjectives, this);
			string arg = FactionHelper.ToUIString(faction);
			return $"{text5}\nRef:{num} - Faction:{arg}";
		}
	}
}
