using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using RoadPathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class RoadEditor : MonoBehaviour, IPlacingMenu
	{
		private enum RoadType
		{
			road = 0,
			seaLane = 1,
			airbase = 2
		}

		private class Pool
		{
			private readonly GameObject prefab;

			private readonly Stack<GameObject> pool = new Stack<GameObject>();

			private readonly List<GameObject> outOfPool = new List<GameObject>();

			public Pool(GameObject prefab)
			{
				this.prefab = prefab;
			}

			public GameObject Get(Transform parent)
			{
				if (pool.TryPop(out var result))
				{
					result.transform.SetParent(parent, worldPositionStays: false);
					result.SetActive(value: true);
				}
				else
				{
					result = Object.Instantiate(prefab, parent);
				}
				outOfPool.Add(result);
				return result;
			}

			public void ReturnAll()
			{
				foreach (GameObject item in outOfPool)
				{
					item.transform.SetParent(Datum.origin, worldPositionStays: false);
					item.SetActive(value: false);
					pool.Push(item);
				}
				outOfPool.Clear();
			}

			public void Dispose()
			{
				foreach (GameObject item in outOfPool)
				{
					Object.Destroy(item);
				}
				outOfPool.Clear();
				foreach (GameObject item2 in pool)
				{
					Object.Destroy(item2);
				}
				pool.Clear();
			}
		}

		public const string ROADS = "Roads";

		public const string SEA_LANES = "Sea Lanes";

		public const string LEVEL_INFO_ROADS = "LEVEL_INFO_Roads";

		public const string LEVEL_INFO_SEA_LANES = "LEVEL_INFO_Sea Lanes";

		private RoadType roadType;

		[SerializeField]
		private TMP_Dropdown networkSelector;

		[SerializeField]
		private Button placeRoadButton;

		[SerializeField]
		private Button deleteRoadButton;

		[SerializeField]
		private GameObject pointPrefab;

		[SerializeField]
		private GameObject segmentPrefab;

		[SerializeField]
		private GameObject nodeInspectorPrefab;

		[SerializeField]
		private Toggle bridgeToggle;

		private RoadNetwork roadNetwork;

		private GameObject submenu;

		private GameObject placingSegment;

		private Road selectedRoad;

		private Road constructingRoad;

		[SerializeField]
		private Material visMat;

		[SerializeField]
		private Material visMatBridge;

		private Material visMatSelected;

		private Material visMatNonEditable;

		private Material nodeVisMat;

		private Material nodeVisMatSelected;

		private float visWidth;

		private readonly List<string> networkSelectorKeys = new List<string>();

		private readonly Dictionary<Road, RoadView> roadViews = new Dictionary<Road, RoadView>();

		private readonly Dictionary<Node, RoadNodeMarker> nodeMarkers = new Dictionary<Node, RoadNodeMarker>();

		private readonly Dictionary<int, RoadNodeMarker> pointMarkers = new Dictionary<int, RoadNodeMarker>();

		private bool placingRoad;

		private bool topologyDirty;

		private Pool pointPrefabPool;

		private Pool segmentPrefabPool;

		private RoadNetworkSO SceneRoadNetworkSO => NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.RoadNetwork;

		private RoadNetworkSO SceneSeaLanesSO => NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.SeaLanes;

		private void Awake()
		{
			pointPrefabPool = new Pool(pointPrefab);
			segmentPrefabPool = new Pool(segmentPrefab);
			networkSelector.onValueChanged.AddListener(SelectNetwork);
			placeRoadButton.onClick.AddListener(EnterPlacingMode);
			deleteRoadButton.onClick.AddListener(DeleteSelected);
			CreateSelectorList();
			visMat.color = new Color(0f, 1f, 0f, 0.5f);
			visMatSelected = new Material(visMat);
			visMatSelected.color = new Color(1f, 1f, 1f, 0.5f);
			visMatNonEditable = new Material(visMat);
			visMatNonEditable.color = new Color(1f, 1f, 1f, 0.2f);
			nodeVisMat = new Material(visMat);
			nodeVisMat.color = new Color(0.25f, 0.25f, 1f, 0.7f);
			nodeVisMatSelected = new Material(visMat);
			nodeVisMatSelected.color = new Color(1f, 0.5f, 0f, 0.9f);
			SceneSingleton<UnitSelection>.i.ClearSelection();
			SceneSingleton<UnitSelection>.i.SelectionFilter = IsRoadEditorSelectable;
			SceneSingleton<UnitSelection>.i.OnSelect += UnitSelection_OnSelect;
			MissionManager.onMissionLoad += MissionManager_onMissionLoad;
		}

		private void OnDestroy()
		{
			StopPlacingMode();
			MissionManager.onMissionLoad -= MissionManager_onMissionLoad;
			SceneSingleton<UnitSelection>.i.OnSelect -= UnitSelection_OnSelect;
			SceneSingleton<UnitSelection>.i.SelectionFilter = null;
			SceneSingleton<UnitSelection>.i.ClearSelection();
			ClearVis();
			pointPrefabPool?.Dispose();
			segmentPrefabPool?.Dispose();
			Object.Destroy(submenu);
			Object.Destroy(visMatSelected);
			Object.Destroy(visMatNonEditable);
			Object.Destroy(nodeVisMat);
			Object.Destroy(nodeVisMatSelected);
		}

		private void MissionManager_onMissionLoad(Mission mission)
		{
			string text = ((networkSelector.value < networkSelectorKeys.Count) ? networkSelectorKeys[networkSelector.value] : "Roads");
			CreateSelectorList();
			SelectNetwork(networkSelectorKeys.Contains(text) ? text : "Roads", focusAirbase: false);
		}

		private void CreateSelectorList()
		{
			networkSelectorKeys.Clear();
			networkSelector.options.Clear();
			AddOption("Roads");
			AddOption("Sea Lanes");
			if (Application.isEditor)
			{
				AddOption("LEVEL_INFO_Roads");
				AddOption("LEVEL_INFO_Sea Lanes");
			}
			foreach (Airbase item in FactionRegistry.airbaseLookup.Values.OrderByDescending((Airbase x) => x.IsCustom))
			{
				if (!item.AttachedAirbase)
				{
					string uniqueName = item.SavedAirbase.UniqueName;
					string display = item.SavedAirbase.ToUIString(oneLine: true);
					AddOption(uniqueName, display);
				}
			}
			void AddOption(string key, string text = null)
			{
				networkSelectorKeys.Add(key);
				networkSelector.options.Add(new TMP_Dropdown.OptionData(text ?? key));
			}
		}

		private void Start()
		{
			if (roadNetwork == null)
			{
				SelectNetwork("Roads");
			}
		}

		public void EnterPlacingMode()
		{
			if (!placingRoad)
			{
				placingRoad = true;
				placeRoadButton.interactable = false;
				SceneSingleton<UnitSelection>.i.StartPlaceUnit(this);
				RefreshSelectionColliders();
			}
		}

		private void StopPlacingMode()
		{
			if (placingRoad)
			{
				placingRoad = false;
				placeRoadButton.interactable = true;
				SceneSingleton<UnitSelection>.i.StopPlacingUnit(this);
				RefreshSelectionColliders();
			}
		}

		public void DeleteSelected()
		{
			DeleteRoad(selectedRoad);
		}

		private bool DeleteRoad(Road road)
		{
			if (road == null || !road.IsEditable() || !roadNetwork.roads.Remove(road))
			{
				return false;
			}
			Rebuild(null);
			return true;
		}

		public void SetBridge()
		{
			if (selectedRoad != null && selectedRoad.IsEditable())
			{
				selectedRoad.SetBridge(bridgeToggle.isOn);
				RefreshRoadMaterial(selectedRoad);
			}
		}

		public void SelectNetwork(Airbase airbase, bool focus)
		{
			SelectNetwork(airbase.SavedAirbase.UniqueName, focus);
		}

		private void SelectNetwork(int index)
		{
			SelectNetwork(networkSelectorKeys[index]);
		}

		public void SelectNetwork()
		{
			SelectNetwork(networkSelectorKeys[networkSelector.value], focusAirbase: false);
		}

		public void SelectNetwork(string networkName, bool focusAirbase = true)
		{
			StopPlacingMode();
			constructingRoad = null;
			placingSegment = null;
			topologyDirty = false;
			SceneSingleton<UnitSelection>.i.ClearSelection();
			int num = networkSelectorKeys.IndexOf(networkName);
			if (num != -1)
			{
				networkSelector.SetValueWithoutNotify(num);
			}
			bool flag;
			if (FactionRegistry.airbaseLookup.TryGetValue(networkName, out var value))
			{
				if (focusAirbase)
				{
					SceneSingleton<CameraStateManager>.i.FocusAirbase(value, allowMoveToDropFocus: true, 200f, 40f);
				}
				SetNetwork(value.GetTaxiNetwork(), RoadType.airbase, 10f, Color.green);
				flag = value.IsCustom;
			}
			else
			{
				flag = true;
				switch (networkName)
				{
				case "Roads":
					SetNetwork(MissionManager.CurrentMission.missionSettings.missionRoads, RoadType.road, 10f, Color.green);
					break;
				case "Sea Lanes":
					SetNetwork(MissionManager.CurrentMission.missionSettings.missionSeaLanes, RoadType.seaLane, 300f, Color.cyan);
					break;
				case "LEVEL_INFO_Roads":
					if (SceneRoadNetworkSO == null)
					{
						Debug.LogError($"Map {NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings} has no road scriptable object. Create one to edit the roads in mission editor");
						return;
					}
					SetNetwork(SceneRoadNetworkSO.RoadNetwork, RoadType.road, 10f, Color.green);
					break;
				case "LEVEL_INFO_Sea Lanes":
					if (SceneSeaLanesSO == null)
					{
						Debug.LogError($"Map {NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings} has no road scriptable object. Create one to edit the roads in mission editor");
						return;
					}
					SetNetwork(SceneSeaLanesSO.RoadNetwork, RoadType.seaLane, 300f, Color.cyan);
					break;
				default:
					ColorLog<RoadEditor>.LogError("Could not find road network with name " + networkName);
					return;
				}
			}
			deleteRoadButton.interactable = false;
			bridgeToggle.gameObject.SetActive(value: false);
			placeRoadButton.interactable = flag;
			VisualizeNetworks(flag);
		}

		private void SetNetwork(RoadNetwork network, RoadType type, float width, Color color)
		{
			roadNetwork = network;
			roadType = type;
			visWidth = width;
			color.a = 0.5f;
			visMat.color = color;
		}

		private void PlacePoint(GlobalPosition position)
		{
			if (constructingRoad == null)
			{
				constructingRoad = new Road();
			}
			placingSegment = segmentPrefabPool.Get(Datum.origin);
			placingSegment.GetComponent<Renderer>().material = visMat;
			placingSegment.GetComponent<Collider>().enabled = false;
			constructingRoad.AddPoint(position);
		}

		private Node GetNodeUnderCursor()
		{
			if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hitInfo, 100000f))
			{
				return null;
			}
			return hitInfo.collider.GetComponentInParent<RoadNodeMarker>()?.node;
		}

		public (bool placeMore, IEditorSelectable placedObject) Place(bool shift)
		{
			if (placingSegment != null && placingSegment.transform.localScale.z < 10f)
			{
				return (placeMore: true, placedObject: null);
			}
			GlobalPosition position = SceneSingleton<UnitSelection>.i.placementTransform.GlobalPosition();
			Node nodeUnderCursor = GetNodeUnderCursor();
			if (nodeUnderCursor != null)
			{
				position = nodeUnderCursor.position;
			}
			PlacePoint(position);
			return (placeMore: true, placedObject: null);
		}

		public void CancelPlace()
		{
			StopPlacingMode();
			constructingRoad = null;
			placingSegment = null;
			VisualizeNetworks();
		}

		public void MoveCursor(Transform placementTransform)
		{
		}

		private void Update()
		{
			if (topologyDirty && !EditorHandle.DraggingHandle)
			{
				Rebuild(selectedRoad, GetSelectedPointIndices());
			}
			if ((!placingRoad && Input.GetKeyDown(KeyCode.Delete) && DeleteSelectedPoints()) || constructingRoad == null)
			{
				return;
			}
			if (placingSegment != null)
			{
				Transform obj = placingSegment.transform;
				List<GlobalPosition> points = constructingRoad.points;
				obj.localPosition = points[points.Count - 1].AsVector3();
				placingSegment.transform.LookAt(SceneSingleton<UnitSelection>.i.placementTransform);
				placingSegment.transform.localScale = new Vector3(visWidth, visWidth * 0.1f, Vector3.Distance(placingSegment.transform.position, SceneSingleton<UnitSelection>.i.placementTransform.position));
			}
			if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
			{
				StopPlacingMode();
				Road road = null;
				if (constructingRoad.points.Count > 1)
				{
					constructingRoad.CalcLength();
					roadNetwork.roads.Add(constructingRoad);
					constructingRoad.CheckIntersection(roadNetwork);
					road = constructingRoad;
				}
				constructingRoad = null;
				placingSegment = null;
				VisualizeNetworks();
				if (road != null)
				{
					SelectRoad(road);
				}
			}
		}

		public void ClearVis()
		{
			ClearPointHandles();
			pointPrefabPool.ReturnAll();
			segmentPrefabPool.ReturnAll();
			foreach (RoadView value in roadViews.Values)
			{
				Object.Destroy(value.gameObject);
			}
			roadViews.Clear();
			nodeMarkers.Clear();
		}

		private void VisualizeNetworks(bool editable = true)
		{
			ClearVis();
			if (roadType == RoadType.road && SceneRoadNetworkSO != null && roadNetwork != SceneRoadNetworkSO.RoadNetwork)
			{
				VisualizeNetwork(SceneRoadNetworkSO.RoadNetwork, editable: false);
			}
			else if (roadType == RoadType.seaLane && SceneSeaLanesSO != null && roadNetwork != SceneSeaLanesSO.RoadNetwork)
			{
				VisualizeNetwork(SceneSeaLanesSO.RoadNetwork, editable: false);
			}
			VisualizeNetwork(roadNetwork, editable);
			RefreshPointHandles();
			Physics.SyncTransforms();
		}

		private void VisualizeNetwork(RoadNetwork network, bool editable)
		{
			network.RegenerateNetwork();
			foreach (Road road in network.roads)
			{
				road.SetEditable(editable);
				VisualizeRoad(road);
			}
			VisualizeNodes(network);
		}

		public void VisualizeRoad(Road road)
		{
			GameObject obj = new GameObject("Road View");
			obj.transform.SetParent(Datum.origin, worldPositionStays: false);
			RoadView roadView = obj.AddComponent<RoadView>();
			roadView.Setup(this, road);
			roadViews.Add(road, roadView);
			for (int i = 0; i < road.points.Count - 1; i++)
			{
				GameObject gameObject = segmentPrefabPool.Get(roadView.transform);
				SetSegmentTransform(gameObject.transform, road, i);
				gameObject.GetComponent<Collider>().enabled = road.IsEditable() && !placingRoad;
			}
			RefreshRoadMaterial(road);
		}

		private void SetSegmentTransform(Transform segment, Road road, int index)
		{
			GlobalPosition globalPosition = road.points[index];
			GlobalPosition globalPosition2 = road.points[index + 1];
			Vector3 vector = globalPosition2 - globalPosition;
			segment.localPosition = globalPosition.AsVector3();
			segment.rotation = ((vector == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(vector));
			segment.localScale = new Vector3(visWidth, visWidth * 0.1f, FastMath.Distance(globalPosition, globalPosition2));
		}

		private void RefreshRoadSegments(Road road)
		{
			if (roadViews.TryGetValue(road, out var value))
			{
				for (int i = 0; i < road.points.Count - 1; i++)
				{
					SetSegmentTransform(value.transform.GetChild(i), road, i);
				}
			}
		}

		private void RefreshRoadMaterial(Road road)
		{
			if (road != null && roadViews.TryGetValue(road, out var value))
			{
				Material material = ((road == selectedRoad) ? visMatSelected : ((!road.IsEditable()) ? visMatNonEditable : ((!road.IsBridge()) ? visMat : visMatBridge)));
				for (int i = 0; i < value.transform.childCount; i++)
				{
					value.transform.GetChild(i).GetComponent<Renderer>().material = material;
				}
			}
		}

		public void VisualizeNodes(RoadNetwork network)
		{
			foreach (Node node in network.nodes)
			{
				GameObject obj = pointPrefabPool.Get(Datum.origin);
				obj.GetComponent<Renderer>().material = nodeVisMat;
				RoadNodeMarker component = obj.GetComponent<RoadNodeMarker>();
				component.SetupNode(this, node);
				nodeMarkers[node] = component;
				obj.transform.localPosition = node.position.AsVector3();
				obj.transform.localScale = Vector3.one * visWidth * 0.25f;
			}
		}

		private void RefreshPointHandles()
		{
			ClearPointHandles();
			if (selectedRoad != null && selectedRoad.IsEditable())
			{
				SetupEndpoint(selectedRoad.startNode, 0);
				SetupEndpoint(selectedRoad.endNode, selectedRoad.points.Count - 1);
				for (int i = 1; i < selectedRoad.points.Count - 1; i++)
				{
					GameObject obj = Object.Instantiate(pointPrefab, Datum.origin);
					obj.GetComponent<Renderer>().material = nodeVisMat;
					RoadNodeMarker component = obj.GetComponent<RoadNodeMarker>();
					component.SetupPoint(this, selectedRoad, i);
					obj.transform.localPosition = selectedRoad.points[i].AsVector3();
					obj.transform.localScale = Vector3.one * visWidth * 0.1f;
					obj.GetComponent<Collider>().enabled = true;
					pointMarkers[i] = component;
				}
			}
			RefreshSelectionColliders();
			void SetupEndpoint(Node node, int pointIndex)
			{
				if (node != null && nodeMarkers.TryGetValue(node, out var value))
				{
					value.SetupPoint(this, selectedRoad, pointIndex, node);
					pointMarkers[pointIndex] = value;
				}
			}
		}

		private void ClearPointHandles()
		{
			foreach (RoadNodeMarker value in pointMarkers.Values)
			{
				if (value != null && value.node == null)
				{
					Object.Destroy(value.gameObject);
				}
			}
			pointMarkers.Clear();
			foreach (RoadNodeMarker value2 in nodeMarkers.Values)
			{
				value2.ClearPoint();
				value2.GetComponent<Renderer>().material = nodeVisMat;
			}
		}

		private void RefreshSelectionColliders()
		{
			foreach (KeyValuePair<Road, RoadView> roadView in roadViews)
			{
				for (int i = 0; i < roadView.Value.transform.childCount; i++)
				{
					roadView.Value.transform.GetChild(i).GetComponent<Collider>().enabled = !placingRoad && roadView.Key.IsEditable();
				}
			}
			foreach (RoadNodeMarker value in nodeMarkers.Values)
			{
				value.GetComponent<Collider>().enabled = placingRoad || value.IsRoadPoint;
			}
		}

		public void MoveRoadPoint(RoadNodeMarker marker, GlobalPosition position)
		{
			Road road = marker.Road;
			int pointIndex = marker.PointIndex;
			if (marker.node != null)
			{
				Node node = marker.node;
				node.position = position;
				foreach (Road key in node.connectionsLookup.Keys)
				{
					if (key.startNode == node)
					{
						key.points[0] = position;
					}
					if (key.endNode == node)
					{
						List<GlobalPosition> points = key.points;
						points[points.Count - 1] = position;
					}
					key.UpdateBB();
					key.CalcLength();
					RefreshRoadSegments(key);
				}
				topologyDirty = true;
			}
			else
			{
				road.points[pointIndex] = position;
				road.UpdateBB();
				road.CalcLength();
				RefreshRoadSegments(road);
			}
			marker.transform.localPosition = position.AsVector3();
			Physics.SyncTransforms();
		}

		private void Rebuild(Road road, List<int> selectedPoints = null)
		{
			topologyDirty = false;
			SceneSingleton<UnitSelection>.i.ClearSelection();
			VisualizeNetworks();
			if (road == null || !roadNetwork.roads.Contains(road))
			{
				return;
			}
			SelectRoad(road);
			if (selectedPoints == null || selectedPoints.Count == 0)
			{
				return;
			}
			List<RoadNodeMarker> list = new List<RoadNodeMarker>();
			foreach (int selectedPoint in selectedPoints)
			{
				if (pointMarkers.TryGetValue(selectedPoint, out var value) && !list.Contains(value))
				{
					list.Add(value);
				}
			}
			if (list.Count > 0)
			{
				SceneSingleton<UnitSelection>.i.ReplaceSelection(list);
			}
		}

		public void InsertPoint(Road road, int segmentIndex)
		{
			if (road != null && road.IsEditable() && segmentIndex >= 0 && segmentIndex < road.points.Count - 1)
			{
				Vector3 vector = road.points[segmentIndex].AsVector3();
				Vector3 vector2 = road.points[segmentIndex + 1].AsVector3();
				int num = segmentIndex + 1;
				road.points.Insert(num, new GlobalPosition((vector + vector2) * 0.5f));
				road.UpdateBB();
				road.CalcLength();
				Rebuild(road, new List<int> { num });
			}
		}

		private bool DeleteSelectedPoints()
		{
			if (selectedRoad == null || !selectedRoad.IsEditable())
			{
				return false;
			}
			List<int> selectedPointIndices = GetSelectedPointIndices();
			if (selectedPointIndices.Count == 0)
			{
				return false;
			}
			Road road = selectedRoad;
			if (road.points.Count - selectedPointIndices.Count < 2)
			{
				return DeleteRoad(road);
			}
			foreach (int item in selectedPointIndices.OrderByDescending((int x) => x))
			{
				road.points.RemoveAt(item);
			}
			road.UpdateBB();
			road.CalcLength();
			Rebuild(road);
			return true;
		}

		private List<int> GetSelectedPointIndices()
		{
			List<int> result = new List<int>();
			if (SceneSingleton<UnitSelection>.i.SelectionDetails is SingleSelectionDetails singleSelectionDetails)
			{
				Add(singleSelectionDetails.Source);
			}
			else if (SceneSingleton<UnitSelection>.i.SelectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				foreach (SingleSelectionDetails item in multiSelectSelectionDetails.Items)
				{
					Add(item.Source);
				}
			}
			return result;
			void Add(IEditorSelectable selectable)
			{
				if (selectable is RoadNodeMarker roadNodeMarker && roadNodeMarker.Editor == this && roadNodeMarker.Road == selectedRoad)
				{
					result.Add(roadNodeMarker.PointIndex);
				}
			}
		}

		private bool IsRoadEditorSelectable(IEditorSelectable selectable)
		{
			if (selectable is RoadView roadView)
			{
				if (!MissionEditorInput.IsShift && roadView.Editor == this)
				{
					return roadView.Road.IsEditable();
				}
				return false;
			}
			if (selectable is RoadNodeMarker roadNodeMarker)
			{
				if (roadNodeMarker.Editor == this)
				{
					return roadNodeMarker.Road == selectedRoad;
				}
				return false;
			}
			return false;
		}

		private void UnitSelection_OnSelect(SelectionDetails details)
		{
			Road road = null;
			if (details is SingleSelectionDetails singleSelectionDetails)
			{
				if (singleSelectionDetails.Source is RoadView roadView && roadView.Editor == this)
				{
					road = roadView.Road;
				}
				else if (singleSelectionDetails.Source is RoadNodeMarker roadNodeMarker && roadNodeMarker.Editor == this)
				{
					road = roadNodeMarker.Road;
				}
			}
			else if (details is MultiSelectSelectionDetails multiSelectSelectionDetails && multiSelectSelectionDetails.Items.Count > 0 && multiSelectSelectionDetails.Items[0].Source is RoadNodeMarker roadNodeMarker2 && roadNodeMarker2.Editor == this)
			{
				road = roadNodeMarker2.Road;
			}
			SetSelectedRoad(road);
			RefreshPointSelectionVisuals(details);
		}

		private void SetSelectedRoad(Road road)
		{
			if (selectedRoad != road)
			{
				Road road2 = selectedRoad;
				selectedRoad = road;
				RefreshRoadMaterial(road2);
				RefreshRoadMaterial(selectedRoad);
				RefreshPointHandles();
				bool flag = selectedRoad != null && selectedRoad.IsEditable();
				deleteRoadButton.interactable = flag;
				bridgeToggle.gameObject.SetActive(flag);
				if (flag)
				{
					bridgeToggle.SetIsOnWithoutNotify(selectedRoad.IsBridge());
				}
			}
		}

		private void RefreshPointSelectionVisuals(SelectionDetails details)
		{
			foreach (RoadNodeMarker value in pointMarkers.Values)
			{
				value.GetComponent<Renderer>().material = nodeVisMat;
			}
			if (details is SingleSelectionDetails singleSelectionDetails)
			{
				Highlight(singleSelectionDetails.Source);
			}
			else
			{
				if (!(details is MultiSelectSelectionDetails multiSelectSelectionDetails))
				{
					return;
				}
				foreach (SingleSelectionDetails item in multiSelectSelectionDetails.Items)
				{
					Highlight(item.Source);
				}
			}
			void Highlight(IEditorSelectable selectable)
			{
				if (selectable is RoadNodeMarker roadNodeMarker && roadNodeMarker.Editor == this && roadNodeMarker.IsRoadPoint)
				{
					roadNodeMarker.GetComponent<Renderer>().material = nodeVisMatSelected;
				}
			}
		}

		private void SelectRoad(Road road)
		{
			if (road != null && roadViews.TryGetValue(road, out var value))
			{
				SceneSingleton<UnitSelection>.i.SetSelection(value);
			}
		}
	}
}
