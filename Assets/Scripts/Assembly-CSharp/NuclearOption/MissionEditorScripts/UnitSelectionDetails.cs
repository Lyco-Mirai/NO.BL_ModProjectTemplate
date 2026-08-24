using System;
using NuclearOption.SavedMission;
using RuntimeHandle;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class UnitSelectionDetails : SingleSelectionDetails
	{
		public readonly Unit Unit;

		public readonly SavedUnit SavedUnit;

		public override string DisplayName
		{
			get
			{
				if (SavedUnit == null)
				{
					return Unit.unitName;
				}
				return SavedUnit.UniqueName;
			}
		}

		public override bool IsDestroyed => Unit == null;

		public Faction Faction
		{
			get
			{
				if (!(Unit.NetworkHQ != null))
				{
					return null;
				}
				return Unit.NetworkHQ.faction;
			}
		}

		public override HandleAxes AllowedPositionAxes
		{
			get
			{
				UnitDefinition definition = Unit.definition;
				if (definition.maxEditorHeight != definition.minEditorHeight)
				{
					return HandleAxes.XYZ;
				}
				return HandleAxes.XZ;
			}
		}

		public override bool TryGetFaction(out Faction faction)
		{
			faction = Faction;
			return true;
		}

		public UnitSelectionDetails(Unit unit)
			: base(unit, (unit.SavedUnit.PlacementType == PlacementType.Custom) ? unit.SavedUnit.PositionWrapper : null, (unit.SavedUnit.PlacementType == PlacementType.Custom) ? unit.SavedUnit.RotationWrapper : null)
		{
			if (unit == null)
			{
				throw new ArgumentNullException("Unit");
			}
			Unit = unit;
			SavedUnit = unit.SavedUnit;
		}

		public override void Focus()
		{
			SceneSingleton<CameraStateManager>.i.SetFollowingUnit(Unit);
		}

		public override bool Delete()
		{
			SceneSingleton<MissionEditor>.i.RemoveUnit(Unit);
			return true;
		}

		public GlobalPosition ClampPosition(GlobalPosition position)
		{
			UnitDefinition definition = Unit.definition;
			float num = GetTerrainPosition(position).y + definition.spawnOffset.y;
			float min = num + definition.minEditorHeight;
			float max = num + definition.maxEditorHeight;
			position.y = Mathf.Clamp(position.y, min, max);
			return position;
		}

		private GlobalPosition GetTerrainPosition(GlobalPosition position)
		{
			GlobalPosition? globalPosition = FindHighestTerrainPoint(position);
			if (!globalPosition.HasValue)
			{
				GlobalPosition value = position;
				value.y = -100f;
				globalPosition = value;
			}
			return globalPosition.Value;
		}

		private static GlobalPosition? FindHighestTerrainPoint(GlobalPosition position)
		{
			int num = Physics.RaycastNonAlloc(position.ToLocalPosition() + Vector3.up * 10000f, Vector3.down, UnitSelection.hitCache, 20000f, PhysicsLayers.StaticsMask);
			if (num == 0)
			{
				return null;
			}
			Vector3? vector = null;
			for (int i = 0; i < num; i++)
			{
				if (!(UnitSelection.hitCache[i].collider.GetComponentInParent<Unit>() != null))
				{
					Vector3 point = UnitSelection.hitCache[i].point;
					if (!vector.HasValue || vector.Value.y < point.y)
					{
						vector = point;
					}
				}
			}
			if (!vector.HasValue)
			{
				return null;
			}
			return vector.Value.ToGlobalPosition();
		}

		public override string ToString()
		{
			return "Selection(" + Unit.UniqueName + ")";
		}
	}
}
