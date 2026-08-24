using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class V2LoadoutMap
	{
		[Serializable]
		public class UnitLoadout
		{
			public string UnitKey;

			public List<HardPoint> HardPoints = new List<HardPoint>();
		}

		[Serializable]
		public class HardPoint
		{
			public List<string> Options = new List<string>();
		}

		public List<UnitLoadout> Units = new List<UnitLoadout>();

		public static string JSON = "{\"Units\":[{\"UnitKey\":\"EW1\",\"HardPoints\":[{\"Options\":[\"\",\"laser_EW1\"]},{\"Options\":[\"\",\"ARM1_internalx2\",\"CruiseMissile1_internal\",\"AAM1_double_internal\",\"AShM1_internal\"]},{\"Options\":[\"\",\"Radome1\"]},{\"Options\":[\"\",\"JammingPod1\",\"ARM1_single\",\"bomb_glide1_triple\",\"AShM1_single\",\"AIR-2_Genie_single\"]},{\"Options\":[\"\",\"ARM1_double\",\"ARM1_single\",\"JammingPod1\",\"AAM1_single\",\"AShM1_single\",\"SpecialFlarePod\"]}]},{\"UnitKey\":\"SmallFighter1\",\"HardPoints\":[{\"Options\":[\"\",\"gun_20mm_internal_500\"]},{\"Options\":[\"\",\"AAM1_double_internal\",\"AAM2_single_internal\",\"AGM_heavy_internal\",\"bomb_500_internal\",\"bomb_250_internal\",\"nuclearBomb1_internal\",\"nuclearBomb1_strategic_internal\",\"bomb_125_internalx4\",\"bomb_250_internalx2\"]},{\"Options\":[\"\",\"AAM2_single_internal\",\"bomb_250_internal\",\"AAM1_single_internal\",\"bomb_125_internalx2_line\"]},{\"Options\":[\"\",\"AGM_heavyx2\",\"AGM_heavy_single\",\"AAM2_single\",\"AAM2_double\",\"AAM1_single\",\"AAM1_double\",\"bomb_glide1_triple\",\"bomb_250_triple\",\"bomb_500_single\",\"bomb_500_double\",\"bomb_penetrator1_mount\",\"ARM1_single\",\"AShM2_single\",\"AShM2_double\",\"AIR-2_Genie_single\",\"bomb_125_quad\"]},{\"Options\":[\"\",\"AAM2_single\",\"AAM1_single\",\"bomb_glide1_single\",\"bomb_250_single\",\"bomb_500_single\",\"AGM_heavy_single\",\"AShM2_single\",\"SpecialFlarePod\",\"bomb_125_quad\"]}]},{\"UnitKey\":\"Darkreach\",\"HardPoints\":[{\"Options\":[\"\",\"AGM_heavy_internalx8\",\"bomb_250_internalx18\",\"bomb_500_internalx8\",\"bomb_penetrator1_internalx3\",\"bomb_demolition_internal\",\"CruiseMissile1_internalx6\",\"nuclearBomb1_internal\",\"nuclearBomb1_internalx2\",\"nuclearBomb1_internalx4\",\"nuclearBomb1_strategic_internal\",\"nuclearBomb1_strategic_internalx2\",\"nuclearBomb1_strategic_internalx4\",\"CruiseMissile20kt_internalx2\",\"AShM1_internalx4_flat\",\"AShM1_internalx6\",\"bomb_500_internalx12\"]},{\"Options\":[\"\",\"bomb_250_internalx8\",\"bomb_500_internalx4_stack\",\"bomb_500_internalx6\",\"AGM_heavy_internalx4\",\"AGM_heavy_internalx6\",\"nuclearBomb1_internal\",\"nuclearBomb1_internalx2\",\"nuclearBomb1_strategic_internal\",\"nuclearBomb1_strategic_internalx2\",\"CruiseMissile1_internalx2\",\"CruiseMissile1_internalx4\",\"AShM1_internalx2\",\"AShM1_internalx4_stack\"]},{\"Options\":[\"\",\"bomb_250_internalx8\",\"bomb_500_internalx4_stack\",\"bomb_500_internalx6\",\"AGM_heavy_internalx4\",\"AGM_heavy_internalx6\",\"nuclearBomb1_internal\",\"nuclearBomb1_internalx2\",\"nuclearBomb1_strategic_internal\",\"nuclearBomb1_strategic_internalx2\",\"CruiseMissile1_internalx2\",\"CruiseMissile1_internalx4\",\"AShM1_internalx2\",\"AShM1_internalx4_stack\"]},{\"Options\":[\"\",\"AGM_heavy_internalx4\",\"bomb_250_internalx18\",\"bomb_500_internalx4_flat\",\"bomb_penetrator1_internalx2\",\"CruiseMissile1_internalx2\",\"nuclearBomb1_internal\",\"nuclearBomb1_internalx2\",\"nuclearBomb1_internalx4\",\"nuclearBomb1_strategic_internal\",\"nuclearBomb1_strategic_internalx2\",\"nuclearBomb1_strategic_internalx4\",\"AShM1_internalx2\",\"bomb_500_internalx8\",\"SpecialFlarePod\"]}]},{\"UnitKey\":\"COIN\",\"HardPoints\":[{\"Options\":[\"\",\"12.7mm_internal\"]},{\"Options\":[\"\",\"RocketPod1_single\",\"Gunpod12.7mm\",\"AGM1_single\",\"gunpod_20mm\",\"AGM_heavy_single\",\"bomb_250_single\",\"bomb_125_double\",\"AGM1_double\",\"AGM1_triple\",\"AIR-2_Genie_single\",\"bomb_125_quad\"]},{\"Options\":[\"\",\"AGM1_double\",\"AGM1_single\",\"AGM_heavy_single\",\"Gunpod12.7mm\",\"AAM1_single\",\"RocketPod1_single\",\"bomb_250_single\",\"bomb_125_double\",\"AGM1_triple\",\"IRMS1_double\"]},{\"Options\":[\"\",\"IRMS1_single\",\"AGM1_single\",\"Gunpod12.7mm\",\"RocketPod1_single\",\"AGM1_double\",\"bomb_125_single\",\"AAM1_single\",\"SpecialFlarePod\"]}]},{\"UnitKey\":\"Multirole1\",\"HardPoints\":[{\"Options\":[\"\",\"autocannon_27mm_internal\"]},{\"Options\":[\"\",\"AAM2_triple_internal\",\"AAM1_internalx3\",\"bomb_250_internalx3\",\"bomb_500_internalx2\",\"nuclearBomb1_internal\",\"nuclearBomb1_internalx2\",\"nuclearBomb1_strategic_internal\",\"nuclearBomb1_strategic_internalx2\",\"bomb_125_internalx6\",\"AGM_heavy_internalx2\"]},{\"Options\":[\"\",\"AAM2_triple_internal\",\"AAM1_internalx3\",\"bomb_250_internalx3\",\"bomb_500_internalx2\",\"nuclearBomb1_internal\",\"nuclearBomb1_internalx2\",\"nuclearBomb1_strategic_internal\",\"nuclearBomb1_strategic_internalx2\",\"bomb_125_internalx6\",\"AGM_heavy_internalx2\"]},{\"Options\":[\"\",\"AAM1_double\",\"AAM1_single\",\"AAM2_single\",\"AAM2_double\",\"bomb_glide1_triple\",\"bomb_250_triple\",\"bomb_500_single\",\"bomb_500_double\",\"bomb_penetrator1_mount\",\"AGM_heavyx2\",\"ARM1_single\",\"AShM1_single\",\"bomb_125_quad\",\"AIR-2_Genie_single\"]},{\"Options\":[\"\",\"AAM1_double\",\"AAM1_single\",\"AAM2_single\",\"AAM2_double\",\"bomb_glide1_triple\",\"bomb_250_triple\",\"bomb_500_single\",\"bomb_500_double\",\"AGM_heavyx2\",\"AShM1_single\",\"bomb_125_quad\",\"SpecialFlarePod\"]},{\"Options\":[\"\",\"TailHook_Multirole1\"]}]},{\"UnitKey\":\"AttackHelo1\",\"HardPoints\":[{\"Options\":[\"\",\"turret_30mmHE_750\"]},{\"Options\":[\"\",\"AGM1_quad_internal\"]},{\"Options\":[\"\",\"RocketPod1_triple\",\"AGM1_quad\",\"AGM_heavyx2\",\"Gunpod_25mm_flex\",\"IRMS1_triple\",\"AIR-2_Genie_single\"]},{\"Options\":[\"\",\"IRMS1_double\",\"AAM1_single\",\"AGM1_double_compact\",\"SpecialFlarePod\"]}]},{\"UnitKey\":\"QuadVTOL1\",\"HardPoints\":[{\"Options\":[\"\",\"LightTruck1x2\",\"MunitionsContainerx2\",\"NavalSupplyContainerx2\",\"6x6_1_APCx1\",\"6x6_1_AAx1\",\"6x6_1_ATx1\",\"6x6_1_IFVx1\",\"HLT-Rx1\"]},{\"Options\":[\"\",\"76mmSideMount\"]},{\"Options\":[\"\",\"AGM1_floorLauncher_QuadVTOL1\"]},{\"Options\":[\"\",\"12.7mmRotaryTurret\"]},{\"Options\":[\"\",\"IRMS1_double\",\"Gunpod_25mm_flex\",\"SpecialFlarePod\",\"AIR-2_Genie_single\"]}]},{\"UnitKey\":\"trainer\",\"HardPoints\":[{\"Options\":[\"\",\"gunpod_25mm_trainer\",\"bomb_500_internal\",\"AGM_heavy_internal\",\"AAM1_double_internal\",\"nuclearBomb1_internal\",\"nuclearBomb1_strategic_internal\",\"bomb_250_internalx2\",\"bomb_125_internalx4\"]},{\"Options\":[\"\",\"AGM_heavy_single\",\"AGM1_single\",\"AGM1_double\",\"AAM1_single\",\"AAM1_double\",\"AAM2_single\",\"bomb_500_single\",\"gunpod_20mm\",\"RocketPod1_single\",\"bomb_250_single\",\"bomb_250_triple\",\"bomb_glide1_single\",\"bomb_glide1_triple\",\"AGM1_triple\",\"bomb_125_quad\",\"AIR-2_Genie_single\"]},{\"Options\":[\"\",\"AGM1_double\",\"AGM_heavy_single\",\"AGM1_single\",\"AAM1_single\",\"bomb_500_single\",\"RocketPod1_single\",\"bomb_250_single\",\"bomb_glide1_single\",\"AGM1_triple\",\"bomb_glide1_double\",\"bomb_125_triple\",\"IRMS1_double\"]},{\"Options\":[\"\",\"AAM1_single\",\"AGM1_single\",\"bomb_250_single\",\"bomb_glide1_single\",\"bomb_125_double\",\"IRMS1_double\",\"SpecialFlarePod\"]},{\"Options\":[\"\",\"TailHook_Trainer\"]}]},{\"UnitKey\":\"Fighter1\",\"HardPoints\":[{\"Options\":[\"\",\"gun_20mm_internal\"]},{\"Options\":[\"\",\"AAM2_double_internal\",\"AAM1_double_internal\",\"AGM_heavy_internal\",\"bomb_500_internal\",\"nuclearBomb1_internal\",\"nuclearBomb1_strategic_internal\",\"bomb_250_internalx2\",\"bomb_125_internalx4\"]},{\"Options\":[\"\",\"AAM1_single\",\"AAM1_double\",\"AAM2_single\",\"bomb_glide1_triple\",\"bomb_250_triple\",\"bomb_500_single\",\"bomb_500_double\",\"bomb_penetrator1_mount\",\"AGM_heavy_single\",\"AGM_heavyx2\",\"ARM1_single\",\"AShM2_single\",\"AShM2_double\",\"AAM1_triple\",\"AAM2_double\",\"AAM2_triple\",\"bomb_125_quad\",\"AIR-2_Genie_single\"]},{\"Options\":[\"\",\"AAM2_single\",\"AAM1_single\",\"SpecialFlarePod\"]}]}]}";

		private V2LoadoutMap()
		{
		}

		public static V2LoadoutMap Load()
		{
			V2LoadoutMap v2LoadoutMap = JsonUtility.FromJson<V2LoadoutMap>(JSON);
			Debug.Log($"Loaded V2LoadoutMap with {v2LoadoutMap.Units.Count} units");
			return v2LoadoutMap;
		}

		public static string Export(List<AircraftDefinition> units)
		{
			V2LoadoutMap v2LoadoutMap = new V2LoadoutMap();
			v2LoadoutMap.Units = new List<UnitLoadout>();
			foreach (AircraftDefinition unit in units)
			{
				UnitLoadout unitLoadout = new UnitLoadout();
				unitLoadout.UnitKey = unit.jsonKey;
				v2LoadoutMap.Units.Add(unitLoadout);
				WeaponManager weaponManager = unit.unitPrefab.GetComponent<Aircraft>().weaponManager;
				for (int i = 0; i < weaponManager.hardpointSets.Length; i++)
				{
					HardPoint hardPoint = new HardPoint();
					unitLoadout.HardPoints.Add(hardPoint);
					for (int j = 0; j < weaponManager.hardpointSets[i].weaponOptions.Count; j++)
					{
						WeaponMount weaponMount = weaponManager.hardpointSets[i].weaponOptions[j];
						hardPoint.Options.Add(weaponMount?.jsonKey ?? "");
					}
				}
			}
			return JsonUtility.ToJson(v2LoadoutMap);
		}
	}
}
