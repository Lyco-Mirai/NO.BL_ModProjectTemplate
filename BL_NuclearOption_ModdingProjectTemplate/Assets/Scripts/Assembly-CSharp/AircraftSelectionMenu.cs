using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts.Buttons;
using NuclearOption.Networking;
using NuclearOption.Social;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AircraftSelectionMenu : MonoBehaviour
{
	[SerializeField]
	private TMP_Text aircraftName;

	[SerializeField]
	private TMP_Text info;

	[SerializeField]
	private TMP_Text airbaseName;

	[SerializeField]
	private TMP_Text playerName;

	[SerializeField]
	private TMP_Text playerRank;

	[SerializeField]
	private TMP_Text playerAllocation;

	[SerializeField]
	private TMP_Text loadoutValue;

	[SerializeField]
	private TMP_Text loadoutWeight;

	[SerializeField]
	private TMP_Text fuelPercentage;

	[SerializeField]
	private TMP_Text fuelWeight;

	[SerializeField]
	private TMP_Text grossWeight;

	[SerializeField]
	private TMP_Text RCS;

	[SerializeField]
	private TMP_Text TWR;

	[SerializeField]
	private TMP_Text TWRLabel;

	[SerializeField]
	private TMP_Text warheads;

	[SerializeField]
	private TMP_Text warheadsAtAirbase;

	[SerializeField]
	private TMP_Text factionName;

	[SerializeField]
	private TMP_Text factionFunds;

	[SerializeField]
	private TMP_Text factionScore;

	[SerializeField]
	private TMP_Text escalationText;

	[SerializeField]
	private TMP_Text weaponSeeker;

	[SerializeField]
	private TMP_Text weaponRange;

	[SerializeField]
	private TMP_Text weaponAP;

	[SerializeField]
	private TMP_Text weaponHE;

	[SerializeField]
	private TMP_Text weaponRCS;

	[SerializeField]
	private TMP_Text weaponCost;

	[SerializeField]
	private Button flyButton;

	[SerializeField]
	private Button contributeButton;

	[SerializeField]
	private ShowHoverText warheadsHoverText;

	[SerializeField]
	private Slider fuelLevel;

	[SerializeField]
	private Transform weaponSelectionPanel;

	[SerializeField]
	private Transform reserveNoticePosition;

	[SerializeField]
	private Transform infoPanel;

	[SerializeField]
	private Transform conventionalStart;

	[SerializeField]
	private Transform tacticalStart;

	[SerializeField]
	private Transform strategicStart;

	[SerializeField]
	private Transform strategicEnd;

	[SerializeField]
	private GameObject weaponSelectionPrefab;

	[SerializeField]
	private GameObject groundSupportEquipmentPrefab;

	[SerializeField]
	private GameObject insufficientFunds;

	[SerializeField]
	private GameObject weaponImageArea;

	[SerializeField]
	private GameObject weaponInfoArea;

	[SerializeField]
	private GameObject reserveNotice;

	[SerializeField]
	private GameObject contributePanel;

	[SerializeField]
	private Image weaponImage;

	[SerializeField]
	private Image loadoutUnaffordable;

	[SerializeField]
	private Image overWeight;

	[SerializeField]
	private Image insufficientRank;

	[SerializeField]
	private Image insufficientWarheads;

	[SerializeField]
	private Image factionLogo;

	[SerializeField]
	private Image escalationPointer;

	[SerializeField]
	private Image escalationUnderline;

	[SerializeField]
	private Light selectionLight;

	[SerializeField]
	private GameObject noHangarsPanel;

	[SerializeField]
	private GameObject warheadsAtAirbasePanel;

	[SerializeField]
	private AircraftInventoryMenu aircraftInventoryMenu;

	[SerializeField]
	private LoadoutSelector loadoutSelector;

	private GameObject groundSupportEquipment;

	private WeaponManager weaponManager;

	private Airbase airbase;

	[SerializeField]
	private Transform[] aircraftPreview;

	private List<AircraftDefinition> aircraftSelection = new List<AircraftDefinition>();

	private AircraftDefinition selectedType;

	[NonSerialized]
	public List<WeaponSelector> weaponSelectors = new List<WeaponSelector>();

	private int selectionIndex;

	private Aircraft previewAircraft;

	private AircraftInfo aircraftInfo;

	private float loadoutCost;

	private float cachedGrossWeight;

	private readonly List<Rigidbody> previewRBs = new List<Rigidbody>();

	private Color escalationBaseColor = Color.grey;

	private Color conventionalColor = Color.green;

	private Color tacticalColor = Color.yellow;

	private Color strategicColor = Color.red;

	private Player localPlayer;

	public event Action<AircraftDefinition> OnSelectedAircraftChange;

	public event Action<AircraftDefinition> OnAircraftOwnedChange;

	private void OnEnable()
	{
		SceneSingleton<DynamicMap>.i.DeselectAllIcons();
		DynamicMap.AllowedToOpen = false;
		GameplayUI.AllowPauseKeybind = false;
		SceneSingleton<CameraStateManager>.i.SetFollowingUnit(null);
		SceneSingleton<CameraStateManager>.i.Illuminator.enabled = true;
		MessageUI.SetFixedBoxSize();
		SceneSingleton<AllocationDisplay>.i.SetVisible(visible: false);
	}

	private void SelectionMenu_OnAirbaseCapture()
	{
		ReturnToMap();
	}

	public void Initialize(Player localPlayer, Airbase airbase)
	{
		this.localPlayer = localPlayer;
		aircraftInventoryMenu.Initialize(localPlayer);
		Refresh(airbase);
		loadoutSelector.onLoadoutChange += AircraftSelectionMenu_OnChange;
		loadoutSelector.OnWeaponInfoInspected += DisplayInfo;
		aircraftInventoryMenu.onChange += AircraftSelectionMenu_OnChange;
	}

	public void Refresh(Airbase airbase)
	{
		ClearEvents();
		SceneSingleton<ReserveReport>.i.SetPosition(reserveNoticePosition.position);
		fuelLevel.SetValueWithoutNotify(1f);
		airbase.onLostControl += SelectionMenu_OnAirbaseCapture;
		SceneSingleton<CameraStateManager>.i.FocusAirbase(airbase, allowMoveToDropFocus: false);
		airbase.ShowSlectionObjects(show: true);
		airbaseName.text = airbase.SavedAirbase.DisplayName;
		playerName.text = localPlayer.GetDisplayName(PlayerNameContext.Other);
		playerRank.text = $"Rank {localPlayer.PlayerRank}";
		SceneSingleton<GameplayUI>.i.HideSelectAirbase();
		factionLogo.sprite = airbase.CurrentHQ.faction.factionColorLogo;
		factionName.text = airbase.CurrentHQ.faction.factionExtendedName;
		selectionIndex = 0;
		this.airbase = airbase;
		RichPresenceManager.SetAirbase(airbase.SavedAirbase.DisplayName, PresenceActivity.SelectingAircraft);
		if (aircraftSelection == null)
		{
			aircraftSelection = new List<AircraftDefinition>();
		}
		if (aircraftSelection.Count > 0)
		{
			aircraftSelection.Clear();
		}
		DestroyPreviewAircraft();
		foreach (AircraftDefinition item in airbase.GetAvailableAircraft())
		{
			if (!airbase.CurrentHQ.restrictedAircraft.Contains(item.jsonKey))
			{
				aircraftSelection.Add(item);
			}
		}
		if (aircraftSelection.Count > 0)
		{
			aircraftSelection.Sort((AircraftDefinition a, AircraftDefinition b) => a.aircraftParameters.rankRequired.CompareTo(b.aircraftParameters.rankRequired));
			aircraftName.text = aircraftSelection[selectionIndex].unitName ?? "";
			info.text = aircraftSelection[selectionIndex].description;
			SpawnPreview();
		}
		else
		{
			aircraftName.text = "No aircraft available";
			info.text = "There are no operational hangars at this airbase";
			flyButton.interactable = false;
			contributePanel.SetActive(value: false);
		}
		SceneSingleton<GameplayUI>.i.menuCanvas.enabled = true;
		SceneSingleton<GameplayUI>.i.gameplayCanvas.enabled = true;
		CursorManager.SetFlag(CursorFlags.SelectionMenu, value: true);
		aircraftInventoryMenu.SetSelectedType(selectedType);
		DelayedUpdateStats().Forget();
	}

	public void GoToNextAirbase()
	{
		if (airbase.CurrentHQ == null)
		{
			return;
		}
		airbase.ShowSlectionObjects(show: false);
		List<Airbase> list = airbase.CurrentHQ.GetAirbases().ToList();
		if (list.Count != 1)
		{
			int num = list.IndexOf(airbase);
			num++;
			if (num > list.Count - 1)
			{
				num = 0;
			}
			Refresh(list[num]);
			airbase.ShowSlectionObjects(show: true);
		}
	}

	public void GoToPreviousAirbase()
	{
		if (airbase.CurrentHQ == null)
		{
			return;
		}
		airbase.ShowSlectionObjects(show: false);
		List<Airbase> list = airbase.CurrentHQ.GetAirbases().ToList();
		if (list.Count != 1)
		{
			int num = list.IndexOf(airbase);
			num--;
			if (num < 0)
			{
				num = list.Count - 1;
			}
			Refresh(list[num]);
			airbase.ShowSlectionObjects(show: true);
		}
	}

	private void ShowTWR(float thrust, float weight)
	{
		TWR.text = $"{thrust / (weight * 9.81f):F2}";
		TWRLabel.text = "T/W";
	}

	private void ShowPWR(float power, float weight)
	{
		TWR.text = UnitConverter.PowerToWeightReading(power * 0.001f / weight);
		TWRLabel.text = "P/W";
	}

	public void DisplayInfo(WeaponInfo weaponInfo)
	{
		if (weaponInfo == null)
		{
			weaponImageArea.SetActive(value: false);
			weaponInfoArea.SetActive(value: false);
			info.text = ((previewAircraft != null) ? aircraftSelection[selectionIndex].description : "Nothing Selected");
			return;
		}
		if (weaponInfo.weaponIcon != null)
		{
			weaponImageArea.SetActive(value: true);
			weaponImage.sprite = weaponInfo.weaponIcon;
			weaponInfoArea.SetActive(value: true);
			if (!weaponInfo.cargo && !weaponInfo.troops)
			{
				weaponSeeker.text = ((weaponInfo.weaponPrefab != null) ? weaponInfo.weaponPrefab.GetComponent<MissileSeeker>().GetSeekerType() : (weaponInfo.energy ? "Laser" : "Gun"));
				weaponRange.text = "R: " + UnitConverter.DistanceReading(weaponInfo.targetRequirements.maxRange);
				weaponAP.text = ((weaponInfo.weaponPrefab != null) ? $"AP: {weaponInfo.weaponPrefab.GetComponent<Missile>().GetPierce()}" : $"AP: {weaponInfo.pierceDamage}");
				weaponHE.text = ((weaponInfo.weaponPrefab != null) ? ("HE: " + UnitConverter.YieldReading(weaponInfo.weaponPrefab.GetComponent<Missile>().GetYield())) : ("HE: " + UnitConverter.YieldReading(weaponInfo.blastDamage)));
				weaponRCS.text = ((weaponInfo.weaponPrefab != null) ? $"RCS: {weaponInfo.weaponPrefab.GetComponent<Missile>().definition.radarSize}" : "-");
				weaponCost.text = ((weaponInfo.weaponPrefab != null) ? ("C: " + UnitConverter.ValueReading(weaponInfo.costPerRound)) : "-");
			}
			else
			{
				weaponSeeker.text = "Cargo";
				weaponRange.text = "-";
				weaponAP.text = "-";
				weaponHE.text = "-";
				weaponRCS.text = "M: " + UnitConverter.WeightReading(weaponInfo.massPerRound);
				weaponCost.text = "C: " + UnitConverter.ValueReading(weaponInfo.costPerRound);
			}
		}
		else
		{
			weaponImageArea.SetActive(value: false);
			weaponInfoArea.SetActive(value: false);
		}
		info.text = weaponInfo.description;
	}

	private void DisplayEscalationPointer()
	{
		float currentEscalation = NetworkSceneSingleton<MissionManager>.i.currentEscalation;
		float tacticalThreshold = NetworkSceneSingleton<MissionManager>.i.tacticalThreshold;
		float strategicThreshold = NetworkSceneSingleton<MissionManager>.i.strategicThreshold;
		if (currentEscalation < tacticalThreshold)
		{
			float t = Mathf.InverseLerp(0f, tacticalThreshold, currentEscalation);
			escalationPointer.transform.position = Vector3.Lerp(conventionalStart.position, tacticalStart.position, t);
			escalationPointer.color = Color.Lerp(escalationBaseColor, tacticalColor, t);
			escalationText.color = escalationPointer.color;
			escalationUnderline.color = escalationPointer.color;
			escalationText.text = "NON-NUCLEAR";
		}
		else if (currentEscalation < strategicThreshold)
		{
			float t2 = Mathf.InverseLerp(tacticalThreshold, strategicThreshold, currentEscalation);
			escalationPointer.transform.position = Vector3.Lerp(tacticalStart.position, strategicStart.position, t2);
			escalationPointer.color = Color.Lerp(tacticalColor, strategicColor, t2);
			escalationText.color = escalationPointer.color;
			escalationUnderline.color = escalationPointer.color;
			escalationText.text = "TACTICAL NUCLEAR";
		}
		else
		{
			float t3 = Mathf.InverseLerp(strategicThreshold, 2f * strategicThreshold, currentEscalation);
			escalationPointer.transform.position = Vector3.Lerp(strategicStart.position, strategicEnd.position, t3);
			escalationPointer.color = strategicColor;
			escalationText.color = strategicColor;
			escalationUnderline.color = strategicColor;
			escalationText.text = "STRATEGIC NUCLEAR";
		}
	}

	private void SpawnPreview()
	{
		if (!Physics.Linecast(airbase.aircraftSelectionTransform.position + Vector3.up * 5f, airbase.aircraftSelectionTransform.position - Vector3.up * 100f, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
		{
			return;
		}
		warheadsHoverText.SetText("Available warheads at this airbase");
		Transform[] componentsInChildren;
		if (groundSupportEquipment == null)
		{
			groundSupportEquipment = UnityEngine.Object.Instantiate(groundSupportEquipmentPrefab, hitInfo.point, Quaternion.LookRotation(airbase.aircraftSelectionTransform.forward, hitInfo.normal));
			groundSupportEquipment.transform.SetParent(hitInfo.collider.transform);
			componentsInChildren = groundSupportEquipment.GetComponentsInChildren<Transform>();
			foreach (Transform transform in componentsInChildren)
			{
				if (Physics.Linecast(transform.position + transform.up, transform.position - transform.up, out var hitInfo2))
				{
					transform.SetPositionAndRotation(hitInfo2.point, Quaternion.LookRotation(transform.forward, hitInfo2.normal));
				}
			}
		}
		AircraftDefinition aircraftDefinition = aircraftSelection[selectionIndex];
		Vector3 velocity = ((hitInfo.collider.attachedRigidbody != null) ? hitInfo.collider.attachedRigidbody.GetPointVelocity(hitInfo.point) : Vector3.zero);
		GameObject gameObject = UnityEngine.Object.Instantiate(aircraftDefinition.unitPrefab, hitInfo.point + Vector3.up * aircraftDefinition.spawnOffset.y, airbase.aircraftSelectionTransform.rotation * Quaternion.Euler(aircraftDefinition.restRotation));
		gameObject.GetComponent<Rigidbody>().velocity = velocity;
		previewAircraft = gameObject.GetComponentInChildren<Aircraft>();
		previewAircraft.networked = false;
		//previewAircraft.NetworkHQ = localPlayer.HQ;
		weaponManager = previewAircraft.weaponManager;
		aircraftPreview = gameObject.GetComponentsInChildren<Transform>();
		previewAircraft.SetComplexPhysics();
		previewAircraft.SetGear(LandingGear.GearState.LockedExtended);
		previewAircraft.rb.interpolation = RigidbodyInterpolation.Interpolate;
		Pilot[] pilots = previewAircraft.pilots;
		for (int i = 0; i < pilots.Length; i++)
		{
			pilots[i].TogglePilotVisibility(enabled: false);
		}
		SceneSingleton<CameraStateManager>.i.selectionState.SetPreviewAircraft(previewAircraft);
		selectedType = previewAircraft.definition;
		aircraftInfo = selectedType.aircraftInfo;
		loadoutSelector.AssignAircraft(previewAircraft, airbase.CurrentHQ, airbase);
		DisplayInfo(null);
		previewRBs.Clear();
		componentsInChildren = aircraftPreview;
		foreach (Transform obj in componentsInChildren)
		{
			int ignoreCollisions = PhysicsLayers.IgnoreCollisions;
			obj.gameObject.layer = ignoreCollisions;
			Rigidbody component = obj.GetComponent<Rigidbody>();
			if (!(component == null) && !previewRBs.Contains(component))
			{
				previewRBs.Add(component);
			}
		}
		previewAircraft.OpenCanopies();
		previewAircraft.ShowGroundEquipment();
		info.text = aircraftSelection[selectionIndex].description;
		this.OnSelectedAircraftChange?.Invoke(selectedType);
	}

	public void FlyAircraft()
	{
		FlyAircraftAsync().Forget();
	}

	private async UniTask FlyAircraftAsync()
	{
		if (airbase == null)
		{
			return;
		}
		AircraftDefinition definition = aircraftSelection[selectionIndex];
		if (!airbase.CanSpawnAircraft(definition))
		{
			noHangarsPanel.SetActive(value: true);
			return;
		}
		loadoutSelector.GenerateLoadoutFromDropdowns();
		if (await NetworkSceneSingleton<Spawner>.i.RequestSpawnAtAirbase(airbase, definition, loadoutSelector.CurrentLivery, loadoutSelector.GenerateLoadoutFromDropdowns(), fuelLevel.value))
		{
			HideSelection();
		}
		else
		{
			noHangarsPanel.SetActive(value: true);
		}
	}

	private void DestroyPreviewAircraft()
	{
		if (aircraftPreview.Length == 0)
		{
			return;
		}
		Transform[] array = aircraftPreview;
		foreach (Transform transform in array)
		{
			if (transform != null)
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
	}

	private void HideSelection()
	{
		DestroyPreviewAircraft();
		DynamicMap.AllowedToOpen = true;
		if (this != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void AircraftSelectionMenu_OnChange()
	{
		DelayedUpdateStats().Forget();
	}

	private async UniTask DelayedUpdateStats()
	{
		await UniTask.Yield();
		UpdateReadouts();
	}

	private void UpdateReadouts()
	{
		if (previewAircraft == null || CameraStateManager.cameraMode != CameraMode.selection)
		{
			flyButton.interactable = false;
			return;
		}
		RCS.text = $"{previewAircraft.RCS:F4}";
		loadoutCost = weaponManager.GetCurrentValue(includeCargo: true);
		loadoutValue.text = UnitConverter.ValueReading(loadoutCost);
		loadoutWeight.text = UnitConverter.WeightReading(weaponManager.GetCurrentMass());
		fuelWeight.text = UnitConverter.WeightReading(previewAircraft.GetFuelQuantity()) ?? "";
		if (selectedType != null)
		{
			playerAllocation.text = $"${localPlayer.Allocation:F2}m";
			insufficientFunds.SetActive(localPlayer.Allocation < selectedType.value);
			loadoutUnaffordable.enabled = localPlayer.Allocation < loadoutCost;
			insufficientRank.enabled = localPlayer.PlayerRank < selectedType.aircraftParameters.rankRequired;
			CheckWarheads();
			cachedGrossWeight = 0f;
			foreach (UnitPart allPart in previewAircraft.GetAllParts())
			{
				cachedGrossWeight += allPart.mass;
			}
			grossWeight.text = UnitConverter.WeightReading(cachedGrossWeight) + " / " + UnitConverter.WeightReading(aircraftInfo.maxWeight);
			aircraftName.text = ((selectionIndex < aircraftSelection.Count) ? aircraftSelection[selectionIndex].unitName : "None");
			TWR.text = "0";
			float maxThrust;
			if (previewAircraft.GetMaxPower(out var maxPower))
			{
				ShowPWR(maxPower, cachedGrossWeight);
			}
			else if (previewAircraft.GetMaxThrust(out maxThrust))
			{
				ShowTWR(maxThrust, cachedGrossWeight);
			}
		}
		else
		{
			aircraftName.text = "No aircraft available";
			loadoutUnaffordable.enabled = false;
			overWeight.enabled = false;
			insufficientRank.enabled = false;
		}
		selectionLight.enabled = NetworkSceneSingleton<LevelInfo>.i.timeOfDay < 6f || NetworkSceneSingleton<LevelInfo>.i.timeOfDay > 18f;
		selectionLight.transform.position = previewAircraft.transform.position + Vector3.up * 20f + Vector3.forward * 10f;
		selectionLight.transform.LookAt(previewAircraft.transform.position);
	}

	private void Update()
	{
		if (airbase.UnitDestroyed())
		{
			ReturnToMap();
			return;
		}
		contributeButton.interactable = localPlayer.Allocation > 0f;
		previewAircraft.GetInputs().brake = 1f;
		int num = airbase.GetWarheads();
		bool active = NetworkSceneSingleton<MissionManager>.i.currentEscalation >= NetworkSceneSingleton<MissionManager>.i.tacticalThreshold;
		warheads.text = $"{airbase.CurrentHQ.GetWarheadStockpile()}";
		warheadsAtAirbasePanel.SetActive(active);
		warheadsAtAirbase.text = $"   x {num}";
		factionFunds.text = UnitConverter.ValueReading(airbase.CurrentHQ.factionFunds) ?? "";
		factionScore.text = $"{airbase.CurrentHQ.factionScore:F1}";
		overWeight.enabled = cachedGrossWeight > aircraftInfo.maxWeight;
		if (localPlayer.OwnsAirframe(selectedType, includeReserved: true))
		{
			flyButton.interactable = !loadoutUnaffordable.enabled && !insufficientWarheads.enabled;
		}
		else
		{
			flyButton.interactable = false;
		}
		DisplayEscalationPointer();
		/*if (GameManager.playerInput.GetButton("Pause") || Input.GetKeyDown(KeyCode.Escape) || localPlayer.HQ != airbase.CurrentHQ)
		{
			ReturnToMap();
		}*/
	}

	private void CheckWarheads()
	{
		int currentWarheads = previewAircraft.weaponManager.GetCurrentWarheads();
		warheadsHoverText.SetText("Number of warheads available to this faction");
		insufficientWarheads.enabled = false;
		if (!airbase.HasStorage())
		{
			warheadsHoverText.SetText("No warheads storage at this airbase");
		}
		else if (airbase.GetWarheads() == 0)
		{
			warheadsHoverText.SetText("No warheads available at this airbase");
		}
		else if (currentWarheads > 0 && airbase.GetWarheads() < currentWarheads)
		{
			warheadsHoverText.SetText("Not enough warheads available at this airbase");
			insufficientWarheads.enabled = true;
		}
		else if (MissionManager.AllowTactical() && localPlayer.PlayerRank < MissionManager.CurrentMission.missionSettings.minRankTacticalWarhead)
		{
			warheadsHoverText.SetText($"Minimum rank {MissionManager.CurrentMission.missionSettings.minRankTacticalWarhead} required");
		}
		else if (MissionManager.AllowStrategic() && localPlayer.PlayerRank < MissionManager.CurrentMission.missionSettings.minRankStrategicWarhead)
		{
			warheadsHoverText.SetText("Only tactical warhead allowed at your rank");
		}
	}

	public void ReturnToMap()
	{
		RichPresenceManager.SetActivity(PresenceActivity.Preparing);
		HideSelection();
		SceneSingleton<CameraStateManager>.i.transform.position = airbase.aircraftSelectionTransform.transform.position + Vector3.up * 2f;
		aircraftPreview = new Transform[0];
		SceneSingleton<CameraStateManager>.i.SwitchState(SceneSingleton<CameraStateManager>.i.freeState);
		SceneSingleton<DynamicMap>.i.mapBackground.gameObject.SetActive(value: true);
		SceneSingleton<DynamicMap>.i.Maximize();
	}

	private void OnDestroy()
	{
		ClearEvents();
		UnityEngine.Object.Destroy(groundSupportEquipment);
		SceneSingleton<ReserveReport>.i.SetPosition(SceneSingleton<GameplayUI>.i.topPanelTransform.position - Vector3.up * 50f);
		MessageUI.SetDynamicBoxSize();
		SceneSingleton<AllocationDisplay>.i.SetVisible(visible: true);
		GameplayUI.AllowPauseKeybind = true;
		CursorManager.SetFlag(CursorFlags.SelectionMenu, value: false);
		airbase.ShowSlectionObjects(show: false);
		if (SceneSingleton<CameraStateManager>.i.Illuminator != null)
		{
			SceneSingleton<CameraStateManager>.i.Illuminator.enabled = false;
		}
		if (SceneSingleton<CameraStateManager>.i.mainCamera != null)
		{
			SceneSingleton<CameraStateManager>.i.mainCamera.nearClipPlane = 1f;
		}
	}

	private void ClearEvents()
	{
		if (airbase != null)
		{
			airbase.onLostControl -= SelectionMenu_OnAirbaseCapture;
		}
	}

	public void SetSelectedType(AircraftDefinition definition)
	{
		selectionIndex = aircraftSelection.IndexOf(definition);
		DestroyPreviewAircraft();
		aircraftPreview = new Transform[0];
		SpawnPreview();
	}

	public void AircraftManagementToggle()
	{
		aircraftInventoryMenu.gameObject.SetActive(!aircraftInventoryMenu.gameObject.activeSelf);
	}

	public AircraftDefinition GetSelectedType()
	{
		return selectedType;
	}

	public bool CanFlyAircraft(AircraftDefinition definition)
	{
		if (definition == null)
		{
			return false;
		}
		return aircraftSelection.Contains(definition);
	}
}
