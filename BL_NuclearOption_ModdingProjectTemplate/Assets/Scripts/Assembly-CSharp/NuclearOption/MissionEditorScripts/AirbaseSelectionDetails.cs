using System;
using NuclearOption.SavedMission;

namespace NuclearOption.MissionEditorScripts
{
	public class AirbaseSelectionDetails : SingleSelectionDetails
	{
		public readonly Airbase Airbase;

		public override string DisplayName
		{
			get
			{
				if (Airbase.TryGetAttachedUnit(out var attachedUnit))
				{
					return attachedUnit.UniqueName + " Airbase";
				}
				if (string.IsNullOrEmpty(Airbase.SavedAirbase.UniqueName))
				{
					return Airbase.name;
				}
				return Airbase.SavedAirbase.UniqueName;
			}
		}

		public override bool PositionHandleAllowed => false;

		public override bool IsDestroyed => false;

		public AirbaseSelectionDetails(Airbase airbase)
			: base(airbase, GetAirbasePosition(airbase), null)
		{
			if (airbase == null)
			{
				throw new ArgumentNullException("Airbase");
			}
			Airbase = airbase;
		}

		private static ValueWrapperGlobalPosition GetAirbasePosition(Airbase airbase)
		{
			if (airbase.TryGetAttachedUnit(out var attachedUnit))
			{
				return attachedUnit.SavedUnit.PositionWrapper;
			}
			return airbase.SavedAirbase.CenterWrapper;
		}

		public override void Focus()
		{
			SceneSingleton<CameraStateManager>.i.FocusAirbase(Airbase, allowMoveToDropFocus: true);
		}

		public override bool Delete()
		{
			return MissionEditor.RemoveAirbase(Airbase);
		}

		public override bool TryGetFaction(out Faction faction)
		{
			faction = ((Airbase.CurrentHQ != null) ? Airbase.CurrentHQ.faction : null);
			return true;
		}
	}
}
