using System;
using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class UnitCopyPaste : SceneSingleton<UnitCopyPaste>
	{
		public SavedUnit Clipboard;

		public static void CopyPaste(Mission mission, SavedUnit copyFrom, Unit pasteToUnit, SavedUnit pasteTo)
		{
			if (UnitPanel.CanHaveFaction(copyFrom) && UnitPanel.CanHaveFaction(pasteTo))
			{
				UnitPanel.SetUnitFaction(pasteTo, copyFrom.faction);
			}
			if (UnitPanel.CanCapture(copyFrom) && UnitPanel.CanCapture(pasteTo))
			{
				pasteTo.CaptureStrength = copyFrom.CaptureStrength;
				pasteTo.CaptureDefense = copyFrom.CaptureDefense;
			}
			if (copyFrom.type == pasteTo.type)
			{
				CopyPasteInventory(copyFrom, pasteTo);
			}
			if (copyFrom is SavedAircraft savedAircraft && pasteTo is SavedAircraft savedAircraft2)
			{
				if (copyFrom.type == pasteTo.type)
				{
					if (savedAircraft.savedLoadout != null)
					{
						SavedAircraft savedAircraft3 = savedAircraft2;
						if (savedAircraft3.savedLoadout == null)
						{
							savedAircraft3.savedLoadout = new SavedLoadout();
						}
						CopyPasteListValue(savedAircraft.savedLoadout.Selected, ref savedAircraft2.savedLoadout.Selected);
					}
					else
					{
						savedAircraft2.savedLoadout = null;
					}
					savedAircraft2.livery = savedAircraft.livery;
				}
				savedAircraft2.fuel = savedAircraft.fuel;
				float maxSpeed = ((Aircraft)savedAircraft2.Unit).GetAircraftParameters().maxSpeed;
				savedAircraft2.startingSpeed = Mathf.Min(maxSpeed, savedAircraft.startingSpeed);
			}
			if (copyFrom is SavedBuilding savedBuilding && pasteTo is SavedBuilding savedBuilding2)
			{
				if (copyFrom.Unit.TryGetComponent<Factory>(out var component) && pasteToUnit.TryGetComponent<Factory>(out var component2) && component.factoryType == component2.factoryType)
				{
					savedBuilding2.factoryOptions = (SavedBuilding.FactoryOptions)(savedBuilding.factoryOptions?.Clone());
				}
				savedBuilding2.capturable = savedBuilding.capturable;
				if (savedBuilding2.Airbase != savedBuilding.Airbase)
				{
					savedBuilding2.SetOrRemoveAirbase(savedBuilding.AirbaseRef);
				}
			}
			if (copyFrom is SavedVehicle savedVehicle)
			{
				if (pasteTo is SavedVehicle savedVehicle2)
				{
					savedVehicle2.holdPosition = savedVehicle.holdPosition;
					savedVehicle2.skill = savedVehicle.skill;
					CopyPasteListClone(savedVehicle.waypoints, ref savedVehicle2.waypoints);
				}
				else if (pasteTo is SavedShip savedShip)
				{
					savedShip.holdPosition = savedVehicle.holdPosition;
					savedShip.skill = savedVehicle.skill;
				}
			}
			if (copyFrom is SavedShip savedShip2)
			{
				if (pasteTo is SavedShip savedShip3)
				{
					savedShip3.holdPosition = savedShip2.holdPosition;
					savedShip3.skill = savedShip2.skill;
					CopyPasteListClone(savedShip2.waypoints, ref savedShip3.waypoints);
				}
				else if (pasteTo is SavedVehicle savedVehicle3)
				{
					savedVehicle3.holdPosition = savedShip2.holdPosition;
					savedVehicle3.skill = savedShip2.skill;
				}
			}
		}

		private static void CopyPasteInventory(SavedUnit copyFrom, SavedUnit pasteTo)
		{
			if (copyFrom.inventory != null && copyFrom.inventory.StoredList.Count > 0)
			{
				if (pasteTo.inventory == null)
				{
					pasteTo.inventory = new SavedInventory();
				}
				CopyPasteListClone(copyFrom.inventory.StoredList, ref pasteTo.inventory.StoredList);
				if (pasteTo.inventory.StoredList.Count <= 0)
				{
					pasteTo.inventory = null;
				}
			}
			else
			{
				pasteTo.inventory = null;
			}
		}

		public static void CopyPasteListClone<T>(List<T> fromList, ref List<T> toList) where T : class, ICloneable
		{
			if (toList == null)
			{
				toList = new List<T>();
			}
			else
			{
				toList.Clear();
			}
			IEnumerable<T> collection = fromList.Select((T x) => (T)x.Clone());
			toList.AddRange(collection);
		}

		public static void CopyPasteListValue<T>(List<T> fromList, ref List<T> toList) where T : struct
		{
			if (toList == null)
			{
				toList = new List<T>();
			}
			else
			{
				toList.Clear();
			}
			toList.AddRange(fromList);
		}
	}
}
