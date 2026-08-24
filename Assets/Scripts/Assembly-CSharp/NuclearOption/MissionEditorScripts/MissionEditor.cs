using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.Networking;
using NuclearOption.NodeGraph;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditor : SceneSingleton<MissionEditor>
	{
		public struct ReloadConfig
		{
			public bool useNewMissionConfig;

			public Mission mission;

			public NewMissionConfig newMissionConfig;

			public ReloadConfig(Mission mission)
			{
				this = default(ReloadConfig);
				this.mission = mission;
				useNewMissionConfig = false;
			}

			public ReloadConfig(NewMissionConfig newMissionConfig)
			{
				this = default(ReloadConfig);
				this.newMissionConfig = newMissionConfig;
				useNewMissionConfig = true;
			}
		}

		private static PositionRotation playFromEditorCameraPosition;

		[NonSerialized]
		public string stickyFaction;

		[Header("References")]
		[SerializeField]
		private MapLoader mapLoader;

		[SerializeField]
		private GameObject cameraNavigator;

		[SerializeField]
		private EditorTabs tabs;

		[SerializeField]
		private UnitSelection unitSelection;

		[SerializeField]
		private UnitCopyPaste copyPaste;

		[SerializeField]
		private MissionLoadErrorPanel loadErrorPanel;

		[SerializeField]
		private GameObject airbaseFlagPrefab;

		[SerializeField]
		private GameObject airbaseRadiusPrefab;

		[SerializeField]
		private LoadingFade loadingFade;

		[SerializeField]
		private GraphEditor graphEditor;

		[Header("Camera Clipping")]
		public bool allowCameraClip;

		[SerializeField]
		private Button cameraClipToggleButton;

		[SerializeField]
		private Image cameraClipImage;

		[SerializeField]
		private Sprite cameraClip;

		[SerializeField]
		private Sprite cameraNoClip;

		[Header("Terrain Following")]
		public bool allowTerrainFollowing;

		[SerializeField]
		private Button terrainFollowToggleButton;

		[SerializeField]
		private Image terrainFollowImage;

		[SerializeField]
		private Sprite terrainUp;

		[SerializeField]
		private Sprite terrainFollow;

		[Header("Input Fields")]
		[SerializeField]
		private TextMeshProUGUI missionNameText;

		private readonly HashSet<Unit> editorUnits = new HashSet<Unit>();

		public EditorTabs Tabs => tabs;

		public static void CheckAutoSave(EditorMissionGroup.AutoSaveType type)
		{
			try
			{
				Mission currentMission = MissionManager.CurrentMission;
				MissionAutoSaveSettings orLoad = MissionAutoSaveSettings.GetOrLoad();
				if (type != EditorMissionGroup.AutoSaveType.Timed || orLoad.CheckTimed())
				{
					MissionKey newKey;
					bool flag = EditorMissionGroup.AutoSave(orLoad, currentMission, type, out newKey);
					orLoad.SetNextSavedTime(flag);
					if (flag)
					{
						currentMission.LoadKey = newKey;
					}
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"AutoSave Exception:{arg}");
			}
		}

		protected override void Awake()
		{
			base.Awake();
			base.gameObject.AddComponent<InputFieldChecker>();
			cameraClipToggleButton.onClick.AddListener(ToggleCameraClip);
			allowCameraClip = PlayerPrefs.GetInt("EditorAllowCameraClip", allowCameraClip ? 1 : 0) == 1;
			SetCameraClip(allowCameraClip);
			terrainFollowToggleButton.onClick.AddListener(ToggleTerrainFollow);
			MissionSaveLoad.AfterSave += UpdateLoadErrorPanel;
		}

		private void OnDestroy()
		{
			MissionSaveLoad.AfterSave -= UpdateLoadErrorPanel;
		}

		private void UpdateLoadErrorPanel(MissionKey key)
		{
			Mission currentMission = MissionManager.CurrentMission;
			LoadErrors saveErrors = currentMission.SaveErrors;
			if (saveErrors != null && saveErrors.ErrorAndExceptionsCount + saveErrors.Warnings.Count > 0)
			{
				saveErrors.LogAllErrors(currentMission.Name);
			}
			loadErrorPanel.SetErrors(saveErrors);
		}

		private void Start()
		{
			MissionAutoSaveSettings.GetOrLoad().SetNextSavedTime(didSave: false);
			TimeScaleManager.Scale = 0f;
		}

		private void Update()
		{
			CheckAutoSave(EditorMissionGroup.AutoSaveType.Timed);
		}

		public void RemoveUnit(Unit unit)
		{
			if (unit == null || !editorUnits.Contains(unit))
			{
				return;
			}
			SavedUnit savedUnit = unit.SavedUnit;
			Mission currentMission = MissionManager.CurrentMission;
			if (!(unit is Aircraft))
			{
				if (!(unit is GroundVehicle))
				{
					if (!(unit is Ship))
					{
						if (!(unit is Building))
						{
							if (!(unit is Scenery))
							{
								if (!(unit is Container))
								{
									if (!(unit is Missile))
									{
										if (unit is PilotDismounted)
										{
											currentMission.pilots.Remove((SavedPilot)savedUnit);
										}
									}
									else
									{
										currentMission.missiles.Remove((SavedMissile)savedUnit);
									}
								}
								else
								{
									currentMission.containers.Remove((SavedContainer)savedUnit);
								}
							}
							else
							{
								currentMission.scenery.Remove((SavedScenery)savedUnit);
							}
						}
						else
						{
							currentMission.buildings.Remove((SavedBuilding)savedUnit);
						}
					}
					else
					{
						currentMission.ships.Remove((SavedShip)savedUnit);
					}
				}
				else
				{
					currentMission.vehicles.Remove((SavedVehicle)savedUnit);
				}
			}
			else
			{
				currentMission.aircraft.Remove((SavedAircraft)savedUnit);
			}
			if (unit.TryGetComponent<Airbase>(out var component))
			{
				string airbaseName = component.SavedAirbase.UniqueName;
				currentMission.RuntimeObjectives.ReferenceDestroyed(component.SavedAirbase);
				int num = currentMission.airbases.FindIndex((SavedAirbase x) => x.UniqueName == airbaseName);
				if (num != -1)
				{
					currentMission.airbases.RemoveAt(num);
				}
			}
			if (savedUnit.PlacementType == PlacementType.Custom)
			{
				currentMission.RuntimeObjectives.ReferenceDestroyed(savedUnit);
			}
			editorUnits.Remove(unit);
			if (savedUnit.PlacementType == PlacementType.Custom)
			{
				unit.DisableUnit();
				UnityEngine.Object.Destroy(unit.gameObject);
				unitSelection.ClearIfSelected(unit);
			}
			if (savedUnit.PlacementType == PlacementType.Override)
			{
				unit.RemoveSavedUnitOverride(fullCleanup: false);
			}
		}

		public void RemoveUnit(SavedUnit saved)
		{
			RemoveUnit(saved?.Unit);
		}

		public static bool RemoveAirbase(Airbase airbase)
		{
			if (!airbase.SavedAirbaseOverride)
			{
				return false;
			}
			SavedAirbase savedAirbase = airbase.SavedAirbase;
			Mission currentMission = MissionManager.CurrentMission;
			currentMission.RuntimeObjectives.ReferenceDestroyed(savedAirbase);
			currentMission.airbases.Remove(savedAirbase);
			airbase.UnlinkSavedAirbase();
			if (savedAirbase != null)
			{
				foreach (SavedBuilding item in savedAirbase.BuildingsRef.ToList())
				{
					item.RemoveAirbase();
				}
			}
			if (!airbase.BuiltIn && !airbase.AttachedAirbase)
			{
				UnityEngine.Object.Destroy(airbase.gameObject);
			}
			return true;
		}

		public void ToggleCameraClip()
		{
			SetCameraClip(!allowCameraClip);
		}

		public void SetCameraClip(bool value)
		{
			if (allowCameraClip != value)
			{
				PlayerPrefs.SetInt("EditorAllowCameraClip", value ? 1 : 0);
			}
			allowCameraClip = value;
			cameraClipImage.sprite = (allowCameraClip ? cameraClip : cameraNoClip);
		}

		public void ToggleTerrainFollow()
		{
			SetTerrainFollow(!allowTerrainFollowing);
		}

		public void SetTerrainFollow(bool value)
		{
			allowTerrainFollowing = value;
			terrainFollowImage.sprite = (allowTerrainFollowing ? terrainFollow : terrainUp);
		}

		public static async UniTask LoadEditor(NewMissionConfig config)
		{
			MissionManager.NewMission(config);
			await LoadEditor(MissionManager.CurrentMission);
		}

		public static async UniTask LoadEditor(Mission mission)
		{
			if (GameManager.gameState == GameState.Menu)
			{
				HostOptions options = new HostOptions(SocketType.Offline, GameState.Editor, mission.MapKey);
				await NetworkManagerNuclearOption.i.StartHostAsync(options);
				SceneSingleton<MissionEditor>.i.OnLoadMission(mission);
			}
			else
			{
				await LoadMissionAsync(mission);
			}
		}

		private static bool TryGetReload(out ReloadConfig reloadConfig)
		{
			if (MissionManager.CurrentMission.LoadKey.HasValue)
			{
				if (!MissionManager.CurrentMission.LoadKey.Value.TryLoad(out var mission, out var error))
				{
					Debug.LogError("Failed to reload mission " + error);
					reloadConfig = default(ReloadConfig);
					return false;
				}
				reloadConfig = new ReloadConfig(mission);
				return true;
			}
			if (MissionManager.CurrentMission.NewMissionConfig.HasValue)
			{
				reloadConfig = new ReloadConfig(MissionManager.CurrentMission.NewMissionConfig.Value);
				return true;
			}
			Debug.LogError("Mission did not have load key or New Config, No way to run PlayFromEditor");
			reloadConfig = default(ReloadConfig);
			return false;
		}

		private static Mission ReloadMission(ReloadConfig config)
		{
			if (config.useNewMissionConfig)
			{
				MissionManager.NewMission(config.newMissionConfig);
			}
			else
			{
				MissionManager.SetMission(config.mission, checkIfSame: false);
			}
			return MissionManager.CurrentMission;
		}

		public static async UniTask PlayFromEditor()
		{
			CheckAutoSave(EditorMissionGroup.AutoSaveType.OnPlay);
			if (TryGetReload(out var reloadConfig))
			{
				SceneSingleton<CameraStateManager>.i.GetCameraPosition(out playFromEditorCameraPosition);
				LoadingScreen loadingScreen = LoadingScreen.GetLoadingScreen();
				loadingScreen.ShowLoadingScreen();
				loadingScreen.SetProgressRange(0f, 0.3f);
				await ExitEditor();
				await UniTask.Yield();
				Mission mission = ReloadMission(reloadConfig);
				HostOptions options = new HostOptions(SocketType.Offline, GameState.SinglePlayer, mission.MapKey);
				UniTask uniTask = NetworkManagerNuclearOption.i.StartHostAsync(options);
				GameManager.IsPlayingFromEditor = true;
				loadingScreen.SetProgressRange(0.3f, 1f);
				await uniTask;
				if (SceneSingleton<CameraStateManager>.i.currentState is CameraFreeState)
				{
					SceneSingleton<CameraStateManager>.i.SetCameraPosition(playFromEditorCameraPosition);
				}
				loadingScreen.HideLoadingScreen();
			}
		}

		public static async UniTask ReturnToEditor()
		{
			if (TryGetReload(out var reloadConfig))
			{
				LoadingScreen loadingScreen = LoadingScreen.GetLoadingScreen();
				loadingScreen.ShowLoadingScreen();
				loadingScreen.SetProgressRange(0f, 0.3f);
				TimeScaleManager.Scale = 0f;
				await NetworkManagerNuclearOption.i.StopAsync(setDisconnectReason: true);
				Mission mission = ReloadMission(reloadConfig);
				loadingScreen.SetProgressRange(0.3f, 1f);
				await LoadEditor(mission);
				GameManager.ResetGameResolution();
				if (SceneSingleton<CameraStateManager>.i.currentState is CameraFreeState)
				{
					SceneSingleton<CameraStateManager>.i.SetCameraPosition(playFromEditorCameraPosition);
				}
				loadingScreen.HideLoadingScreen();
			}
		}

		public static async UniTask ExitEditor()
		{
			CheckAutoSave(EditorMissionGroup.AutoSaveType.OnExit);
			TimeScaleManager.Scale = 1f;
			await NetworkManagerNuclearOption.i.StopAsync(setDisconnectReason: true);
		}

		private static async UniTask LoadMissionAsync(Mission missionToLoad)
		{
			switch (await SceneSingleton<MissionEditor>.i.mapLoader.Load(missionToLoad.MapKey, null, (SceneSingleton<MissionEditor>.i != null) ? new LoadingFade?(SceneSingleton<MissionEditor>.i.loadingFade) : ((LoadingFade?)null)))
			{
			default:
				Debug.LogError("Failed to load mission");
				return;
			case MapLoader.LoadResult.ChangedWorldPrefab:
			case MapLoader.LoadResult.AlreadyLoaded:
				ColorLog<MissionEditor>.Info("Cleaning up existing scene");
				await SceneSingleton<MissionEditor>.i.CleanupCurrentMission();
				break;
			case MapLoader.LoadResult.ChangedScene:
				ColorLog<MissionEditor>.Info("Setting up new MissionEditor after scene change");
				NetworkManagerNuclearOption.i.Host_SceneLoaded();
				break;
			}
			await UniTask.DelayFrame(2);
			SceneSingleton<MissionEditor>.i.OnLoadMission(missionToLoad);
		}

		public void SetMissionNameText(Mission mission)
		{
			missionNameText.text = mission.Name;
		}

		private void OnLoadMission(Mission missionToLoad)
		{
			MissionObjectivesFactory.AssertSceneAirbaseRegistered();
			SetMissionNameText(missionToLoad);
			MissionManager.SetMission(missionToLoad, checkIfSame: true);
			LoadErrors loadErrors;
			try
			{
				missionToLoad.OnSceneLoaded(NetworkSceneSingleton<MissionManager>.i);
				loadErrors = missionToLoad.LoadErrors;
			}
			catch (MissionLoadException ex)
			{
				Debug.LogError("Failed to load mission");
				loadErrors = ex.LoadErrors;
			}
			loadErrors.LogAllErrors(missionToLoad.Name);
			loadErrorPanel.SetErrors(loadErrors);
			NetworkSceneSingleton<Spawner>.i.SpawnFromMissionInEditor(missionToLoad, RegisterUnit);
			NetworkSceneSingleton<LevelInfo>.i.LoadFromMission(missionToLoad);
			tabs.HideTab(clearUnit: true);
			foreach (Airbase value in FactionRegistry.airbaseLookup.Values)
			{
				CreateFlagForAirbase(value);
			}
			MissionSaveLoad.TryLoadObjectiveGraphLayout(missionToLoad);
		}

		public static AirbaseEditorFlag CreateFlag(Color color, float scale)
		{
			return AirbaseEditorFlag.Create(null, SceneSingleton<MissionEditor>.i.airbaseFlagPrefab, color, scale);
		}

		public static void CreateFlagForAirbase(Airbase airbase)
		{
			Color colorOrGray = airbase.CurrentHQ.GetColorOrGray();
			AirbaseEditorFlag componentInChildren = airbase.center.GetComponentInChildren<AirbaseEditorFlag>();
			if (componentInChildren != null)
			{
				componentInChildren.SetColor(colorOrGray);
			}
			else
			{
				AirbaseEditorFlag airbaseEditorFlag = AirbaseEditorFlag.Create(airbase.center, SceneSingleton<MissionEditor>.i.airbaseFlagPrefab, colorOrGray, 5f);
				EditorSelectableProxy.Add(airbase, airbaseEditorFlag.gameObject);
			}
			AirbaseEditorRadius airbaseEditorRadius = airbase.center.GetComponentInChildren<AirbaseEditorRadius>();
			if (airbaseEditorRadius == null)
			{
				airbaseEditorRadius = AirbaseEditorRadius.Create(airbase.center, SceneSingleton<MissionEditor>.i.airbaseRadiusPrefab);
			}
			airbaseEditorRadius.Setup(colorOrGray, airbase.GetRadius());
		}

		private async UniTask CleanupCurrentMission()
		{
			if (graphEditor != null)
			{
				graphEditor.gameObject.SetActive(value: false);
			}
			bool flag = false;
			foreach (Unit editorUnit in editorUnits)
			{
				if (!editorUnit.BuiltIn)
				{
					flag = true;
					NetworkManagerNuclearOption.i.ServerObjectManager.Destroy(editorUnit.Identity, !editorUnit.Identity.IsSceneObject);
				}
			}
			editorUnits.Clear();
			Airbase[] array = FactionRegistry.airbaseLookup.Values.ToArray();
			foreach (Airbase airbase in array)
			{
				if (!(airbase == null))
				{
					if (!airbase.BuiltIn && !airbase.AttachedAirbase)
					{
						flag = true;
						NetworkManagerNuclearOption.i.ServerObjectManager.Destroy(airbase.Identity, !airbase.Identity.IsSceneObject);
					}
					else if (airbase.SavedAirbaseOverride)
					{
						airbase.UnlinkSavedAirbase();
					}
				}
			}
			if (flag)
			{
				await UniTask.Yield();
			}
			foreach (Unit allUnit in UnitRegistry.allUnits)
			{
				allUnit.EditorMapCleanup();
			}
			MissionObjectivesFactory.AssertSceneAirbaseRegistered();
		}

		private void RegisterUnit(Unit unit, SavedUnit savedUnit)
		{
			editorUnits.Add(unit);
			if (unit.SavedUnit != savedUnit)
			{
				unit.LinkSavedUnit(savedUnit);
			}
			savedUnit.AfterLoadEditor();
		}

		public SavedUnit RegisterNewUnit(Unit unit, string uniqueName)
		{
			SavedUnit savedUnit = CreateSavedUnit(unit, uniqueName);
			savedUnit.AfterCreate(unit);
			RegisterUnit(unit, savedUnit);
			return savedUnit;
		}

		public void AddUnitOverride(Unit unit, SavedUnit savedUnit)
		{
			AddSavedUnit(savedUnit);
			editorUnits.Add(unit);
			savedUnit.AfterAddOverride(unit);
		}

		private SavedUnit CreateSavedUnit(Unit unit, string uniqueName)
		{
			if (!(unit is Aircraft))
			{
				if (!(unit is GroundVehicle))
				{
					if (!(unit is Ship))
					{
						if (!(unit is Building))
						{
							if (!(unit is Scenery))
							{
								if (!(unit is Container))
								{
									if (!(unit is Missile))
									{
										if (unit is PilotDismounted)
										{
											SavedPilot savedPilot = new SavedPilot(uniqueName);
											MissionManager.CurrentMission.pilots.Add(savedPilot);
											return savedPilot;
										}
										throw new ArgumentException($"Can't create saved unit for unit type: {unit?.GetType()}");
									}
									SavedMissile savedMissile = new SavedMissile(uniqueName);
									MissionManager.CurrentMission.missiles.Add(savedMissile);
									return savedMissile;
								}
								SavedContainer savedContainer = new SavedContainer(uniqueName);
								MissionManager.CurrentMission.containers.Add(savedContainer);
								return savedContainer;
							}
							SavedScenery savedScenery = new SavedScenery(uniqueName);
							MissionManager.CurrentMission.scenery.Add(savedScenery);
							return savedScenery;
						}
						SavedBuilding savedBuilding = new SavedBuilding(uniqueName);
						MissionManager.CurrentMission.buildings.Add(savedBuilding);
						return savedBuilding;
					}
					SavedShip savedShip = new SavedShip(uniqueName);
					MissionManager.CurrentMission.ships.Add(savedShip);
					return savedShip;
				}
				SavedVehicle savedVehicle = new SavedVehicle(uniqueName);
				MissionManager.CurrentMission.vehicles.Add(savedVehicle);
				return savedVehicle;
			}
			SavedAircraft savedAircraft = new SavedAircraft(uniqueName);
			MissionManager.CurrentMission.aircraft.Add(savedAircraft);
			return savedAircraft;
		}

		private void AddSavedUnit(SavedUnit saved)
		{
			if (!(saved is SavedAircraft item))
			{
				if (!(saved is SavedVehicle item2))
				{
					if (!(saved is SavedShip item3))
					{
						if (!(saved is SavedBuilding item4))
						{
							if (!(saved is SavedScenery item5))
							{
								if (!(saved is SavedContainer item6))
								{
									if (!(saved is SavedMissile item7))
									{
										if (!(saved is SavedPilot item8))
										{
											throw new ArgumentException($"Can't add SavedUnit of type {saved?.GetType()}");
										}
										MissionManager.CurrentMission.pilots.Add(item8);
									}
									else
									{
										MissionManager.CurrentMission.missiles.Add(item7);
									}
								}
								else
								{
									MissionManager.CurrentMission.containers.Add(item6);
								}
							}
							else
							{
								MissionManager.CurrentMission.scenery.Add(item5);
							}
						}
						else
						{
							MissionManager.CurrentMission.buildings.Add(item4);
						}
					}
					else
					{
						MissionManager.CurrentMission.ships.Add(item3);
					}
				}
				else
				{
					MissionManager.CurrentMission.vehicles.Add(item2);
				}
			}
			else
			{
				MissionManager.CurrentMission.aircraft.Add(item);
			}
		}

		public List<Unit> DuplicateUnits(List<SavedUnit> sources, Vector3 offset)
		{
			List<Unit> list = new List<Unit>();
			foreach (SavedUnit source in sources)
			{
				Unit unit = DuplicateUnit(source, offset, syncPhysics: false);
				if (unit != null)
				{
					list.Add(unit);
				}
			}
			Physics.SyncTransforms();
			return list;
		}

		public Unit DuplicateUnit(SavedUnit source, Vector3 offset, bool syncPhysics = true)
		{
			if (source?.Unit == null)
			{
				Debug.LogError("Can't Duplicate unit because SavedUnit.Unit is null");
				return null;
			}
			Unit unit = source.Unit;
			unit.transform.GetPositionAndRotation(out var position, out var rotation);
			string uniqueName = (string.IsNullOrEmpty(source.UniqueName) ? source.type : source.UniqueName);
			using (AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take())
			{
				List<SavedUnit> item = wrapper.Item;
				MissionManager.GetAllSavedUnitsNonAlloc(item, includeBuiltIn: true);
				SaveHelper.MakeUnique(ref uniqueName, item, warn: false);
			}
			Unit unit2 = NetworkSceneSingleton<Spawner>.i.SpawnFromUnitDefinitionInEditor(unit.definition, new GlobalPosition(position.ToGlobalPosition().AsVector3() + offset), rotation, FactionRegistry.HqFromName(source.faction), uniqueName);
			SavedUnit savedUnit = RegisterNewUnit(unit2, uniqueName);
			UnitCopyPaste.CopyPaste(MissionManager.CurrentMission, source, unit2, savedUnit);
			unit2.NetworkHQ = FactionRegistry.HqFromName(savedUnit.faction);
			if (unit2.TryGetComponent<Airbase>(out var component))
			{
				CreateFlagForAirbase(component);
			}
			if (syncPhysics)
			{
				Physics.SyncTransforms();
			}
			return unit2;
		}
	}
}
