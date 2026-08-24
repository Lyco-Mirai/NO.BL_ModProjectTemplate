using System;
using System.Collections.Generic;
using NuclearOption.NodeGraph;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Outcomes;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts.ObjectiveGraph
{
	public class ObjectiveOutcomeGraph : MonoBehaviour
	{
		public static class Ids
		{
			private const string ObjectivePrefix = "Objective_";

			private const string OutcomePrefix = "Outcome_";

			public static readonly PinId StartObjectiveInput = new PinId("StartObjectiveInput");

			public static readonly PinId CompleteObjectiveInput = new PinId("CompleteObjectiveInput");

			public static readonly PinId Completed = new PinId("Completed");

			public static readonly PinId Execute = new PinId("Execute");

			public static readonly PinId StartOutcomeOutput = new PinId("StartOutcomeOutput");

			public static readonly PinId CompleteOutcomeOutput = new PinId("CompleteOutcomeOutput");

			public static readonly PinId Faction = new PinId("Faction");

			public static readonly PinType StartObjectiveInputType = new PinType("StartObjectiveInput");

			public static readonly PinType CompleteObjectiveInputType = new PinType("CompleteObjectiveInput");

			public static readonly PinType ObjectiveOutput = new PinType("ObjectiveOutput");

			public static readonly PinType OutcomeInput = new PinType("OutcomeInput");

			public static readonly PinType StartOutcomeOutputType = new PinType("StartOutcomeOutput");

			public static readonly PinType CompleteOutcomeOutputType = new PinType("CompleteOutcomeOutput");

			public static readonly NodeType Objective = new NodeType("Objective");

			public static readonly NodeType Outcome = new NodeType("Outcome");

			public static NodeId ObjectiveNodeId(string uniqueName)
			{
				return new NodeId("Objective_" + uniqueName);
			}

			public static NodeId OutcomeNodeId(string uniqueName)
			{
				return new NodeId("Outcome_" + uniqueName);
			}

			public static string StripPrefixObjective(string idValue)
			{
				return StripPrefix(idValue, "Objective_");
			}

			public static string StripPrefixOutcome(string idValue)
			{
				return StripPrefix(idValue, "Outcome_");
			}

			private static string StripPrefix(string idValue, string prefix)
			{
				if (idValue.StartsWith(prefix))
				{
					return idValue.Substring(prefix.Length);
				}
				ColorLog<ObjectiveOutcomeGraph>.LogError(idValue + " did not start with " + prefix);
				return idValue;
			}
		}

		[SerializeField]
		private GraphEditor graphEditor;

		[SerializeField]
		private Vector2 duplicateOffset = new Vector2(30f, -30f);

		[SerializeField]
		private EditObjectivePanel editObjectivePanelPrefab;

		[SerializeField]
		private EditOutcomePanel editOutcomePanelPrefab;

		private Mission currentMission;

		private readonly Dictionary<ISaveableReference, GraphNode> nodeLookup = new Dictionary<ISaveableReference, GraphNode>();

		private EditorTabs editorTabs;

		private SidePanel selectedPanel;

		private void OnValidate()
		{
		}

		private void OnEnable()
		{
			editorTabs = GetComponentInParent<EditorTabs>();
			selectedPanel = new SidePanel(editorTabs);
			editorTabs.OnPanelsVisibilityChanged += HandlePanelsVisibilityChanged;
			graphEditor.OnConnectPins += HandleConnectPins;
			graphEditor.OnDisconnectPins += HandleDisconnectPins;
			graphEditor.OnDeleteNode += HandleDeleteNode;
			graphEditor.OnDuplicateNode += HandleDuplicateNode;
			graphEditor.OnSelectionChanged += HandleSelectionChanged;
			MissionManager.onMissionLoad += MissionManager_OnMissionLoaded;
			SetupContextMenu();
			SetSceneRenderingState(!editorTabs.IsPanelsVisible);
			MissionManager_OnMissionLoaded(MissionManager.CurrentMission);
		}

		private void OnDisable()
		{
			editorTabs.OnPanelsVisibilityChanged -= HandlePanelsVisibilityChanged;
			SetSceneRenderingState(enabled: true);
			graphEditor.OnConnectPins -= HandleConnectPins;
			graphEditor.OnDisconnectPins -= HandleDisconnectPins;
			graphEditor.OnDeleteNode -= HandleDeleteNode;
			graphEditor.OnDuplicateNode -= HandleDuplicateNode;
			graphEditor.OnSelectionChanged -= HandleSelectionChanged;
			MissionManager.onMissionLoad -= MissionManager_OnMissionLoaded;
			graphEditor.CurrentLayout?.SyncLayout?.Invoke();
			graphEditor.CurrentLayout = null;
			foreach (ISaveableReference key in nodeLookup.Keys)
			{
				key.OnRenamed -= HandleNodeRenamed;
			}
			selectedPanel?.Destroy();
			nodeLookup.Clear();
		}

		private void HandlePanelsVisibilityChanged(bool visible)
		{
			SetSceneRenderingState(!visible);
		}

		private void SetSceneRenderingState(bool enabled)
		{
			if (SceneSingleton<CameraStateManager>.i != null && SceneSingleton<CameraStateManager>.i.mainCamera != null)
			{
				SceneSingleton<CameraStateManager>.i.mainCamera.enabled = enabled;
			}
		}

		private void MissionManager_OnMissionLoaded(Mission mission)
		{
			currentMission = mission;
			graphEditor.CurrentLayout = mission.ObjectiveGraphLayout ?? new GraphLayoutJson();
			mission.ObjectiveGraphLayout = graphEditor.CurrentLayout;
			graphEditor.DestroyAllNodes();
			graphEditor.ApplyLayoutPanZoom();
			RebuildGraph(mission, graphEditor.CurrentLayout);
			graphEditor.PruneLayout();
		}

		private void RebuildGraph(Mission mission, GraphLayoutJson layout)
		{
			foreach (ISaveableReference key in nodeLookup.Keys)
			{
				key.OnRenamed -= HandleNodeRenamed;
			}
			nodeLookup.Clear();
			if (mission.RuntimeObjectives == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.InfoWarn("Skipping graph rebuild because mission.RuntimeObjectives is null for mission: " + mission.Name);
				return;
			}
			GraphSmartLayout.Compute(mission.RuntimeObjectives.AllObjectives, mission.RuntimeObjectives.AllOutcomes, layout, 350f, 160f);
			foreach (Objective allObjective in mission.RuntimeObjectives.AllObjectives)
			{
				NodeId nodeId = Ids.ObjectiveNodeId(allObjective.SavedObjective.UniqueName);
				if (!layout.TryGetNodePosition(nodeId, out var position))
				{
					position = Vector2.zero;
					ColorLog<ObjectiveOutcomeGraph>.LogError($"Layout missing position for objective node: {nodeId}");
				}
				CreateObjectiveNode(allObjective, position);
			}
			foreach (Outcome allOutcome in mission.RuntimeObjectives.AllOutcomes)
			{
				NodeId nodeId2 = Ids.OutcomeNodeId(allOutcome.SavedOutcome.UniqueName);
				if (!layout.TryGetNodePosition(nodeId2, out var position2))
				{
					position2 = Vector2.zero;
					ColorLog<ObjectiveOutcomeGraph>.LogError($"Layout missing position for outcome node: {nodeId2}");
				}
				CreateOutcomeNode(allOutcome, position2);
			}
			foreach (Objective allObjective2 in mission.RuntimeObjectives.AllObjectives)
			{
				if (!nodeLookup.TryGetValue(allObjective2, out var value))
				{
					ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find node in lookup for objective: " + allObjective2.SavedObjective.UniqueName);
					continue;
				}
				if (!value.TryGetPin(Ids.Completed, PinDirection.Output, out var pin))
				{
					ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find Completed output pin for objective: " + allObjective2.SavedObjective.UniqueName);
					continue;
				}
				foreach (Outcome outcome in allObjective2.Outcomes)
				{
					GraphPin pin2;
					if (!nodeLookup.TryGetValue(outcome, out var value2))
					{
						ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find node in lookup for outcome: " + outcome.SavedOutcome.UniqueName);
					}
					else if (!value2.TryGetPin(Ids.Execute, PinDirection.Input, out pin2))
					{
						ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find Execute input pin for outcome: " + outcome.SavedOutcome.UniqueName);
					}
					else
					{
						graphEditor.ConnectVisual(pin, pin2);
					}
				}
			}
			foreach (Outcome allOutcome2 in mission.RuntimeObjectives.AllOutcomes)
			{
				if (!nodeLookup.TryGetValue(allOutcome2, out var value3))
				{
					ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find node in lookup for outcome: " + allOutcome2.SavedOutcome.UniqueName);
					continue;
				}
				PinId pinId;
				PinId pinId2;
				List<Objective> objectivesToStart;
				if (!(allOutcome2 is StartObjectiveOutcome startObjectiveOutcome))
				{
					if (!(allOutcome2 is CompleteObjectiveOutcome completeObjectiveOutcome))
					{
						continue;
					}
					pinId = Ids.CompleteOutcomeOutput;
					pinId2 = Ids.CompleteObjectiveInput;
					objectivesToStart = completeObjectiveOutcome.objectivesToStart;
				}
				else
				{
					pinId = Ids.StartOutcomeOutput;
					pinId2 = Ids.StartObjectiveInput;
					objectivesToStart = startObjectiveOutcome.objectivesToStart;
				}
				if (!value3.TryGetPin(pinId, PinDirection.Output, out var pin3))
				{
					ColorLog<ObjectiveOutcomeGraph>.LogError($"Failed to find {pinId} output pin for outcome: {allOutcome2.SavedOutcome.UniqueName}");
					continue;
				}
				foreach (Objective item in objectivesToStart)
				{
					GraphNode value4;
					GraphPin pin4;
					if (item.SavedObjective.UniqueName == MissionObjectivesFactory.MissionStartName)
					{
						ColorLog<ObjectiveOutcomeGraph>.LogError("MissionStart objective should not be the target of any outcome");
					}
					else if (!nodeLookup.TryGetValue(item, out value4))
					{
						ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find node in lookup for target objective: " + item.SavedObjective.UniqueName);
					}
					else if (!value4.TryGetPin(pinId2, PinDirection.Input, out pin4))
					{
						ColorLog<ObjectiveOutcomeGraph>.LogError($"Failed to find {pinId2} input pin for target objective: {item.SavedObjective.UniqueName}");
					}
					else
					{
						graphEditor.ConnectVisual(pin3, pin4);
					}
				}
			}
		}

		private void SetupContextMenu()
		{
			GraphContextMenu menu = graphEditor.ContextMenu;
			if (menu == null)
			{
				return;
			}
			List<ContextMenuOption> list = new List<ContextMenuOption>();
			foreach (ObjectiveType value in Enum.GetValues(typeof(ObjectiveType)))
			{
				if (value != ObjectiveType.None)
				{
					List<ContextMenuPinSetup> compatibilityPins = new List<ContextMenuPinSetup>
					{
						new ContextMenuPinSetup
						{
							pinId = Ids.StartObjectiveInput,
							pinType = Ids.StartObjectiveInputType,
							direction = PinDirection.Input,
							allowedConnectionTypes = new List<PinType> { Ids.StartOutcomeOutputType }
						},
						new ContextMenuPinSetup
						{
							pinId = Ids.CompleteObjectiveInput,
							pinType = Ids.CompleteObjectiveInputType,
							direction = PinDirection.Input,
							allowedConnectionTypes = new List<PinType> { Ids.CompleteOutcomeOutputType }
						},
						new ContextMenuPinSetup
						{
							pinId = Ids.Completed,
							pinType = Ids.ObjectiveOutput,
							direction = PinDirection.Output,
							allowedConnectionTypes = new List<PinType> { Ids.OutcomeInput }
						}
					};
					list.Add(new ContextMenuOption
					{
						optionId = new ContextMenuOptionId("Objectives", (int)value),
						label = value.ToNicifyString(),
						compatibilityPins = compatibilityPins,
						onClick = delegate(ContextMenuOptionId optId)
						{
							SpawnObjectiveNode((ObjectiveType)optId.id, menu.SpawnPosition);
						}
					});
				}
			}
			foreach (OutcomeType value2 in Enum.GetValues(typeof(OutcomeType)))
			{
				if (value2 != OutcomeType.None)
				{
					OutcomeType outcomeType2 = value2;
					List<ContextMenuPinSetup> list2 = new List<ContextMenuPinSetup>
					{
						new ContextMenuPinSetup
						{
							pinId = Ids.Execute,
							pinType = Ids.OutcomeInput,
							direction = PinDirection.Input,
							allowedConnectionTypes = new List<PinType> { Ids.ObjectiveOutput }
						}
					};
					switch (outcomeType2)
					{
					case OutcomeType.StartObjective:
						list2.Add(new ContextMenuPinSetup
						{
							pinId = Ids.StartOutcomeOutput,
							pinType = Ids.StartOutcomeOutputType,
							direction = PinDirection.Output,
							allowedConnectionTypes = new List<PinType> { Ids.StartObjectiveInputType }
						});
						break;
					case OutcomeType.StopOrCompleteObjective:
						list2.Add(new ContextMenuPinSetup
						{
							pinId = Ids.CompleteOutcomeOutput,
							pinType = Ids.CompleteOutcomeOutputType,
							direction = PinDirection.Output,
							allowedConnectionTypes = new List<PinType> { Ids.CompleteObjectiveInputType }
						});
						break;
					}
					list.Add(new ContextMenuOption
					{
						optionId = new ContextMenuOptionId("Outcomes", (int)outcomeType2),
						label = outcomeType2.ToNicifyString(),
						compatibilityPins = list2,
						onClick = delegate(ContextMenuOptionId optId)
						{
							SpawnOutcomeNode((OutcomeType)optId.id, menu.SpawnPosition);
						}
					});
				}
			}
			graphEditor.SetBackgroundMenuConfig(new ContextMenuConfig
			{
				Title = "Create Node",
				Options = list
			});
		}

		private void SpawnObjectiveNode(ObjectiveType objType, Vector2 screenPosition)
		{
			Objective objective = SavedObjective.Create(SavedObjective.CreateSavedObjective(objType, objType.ToString()));
			currentMission.RuntimeObjectives.AddNewObjective(objective);
			Vector2 position = graphEditor.ScreenToLocalPosition(screenPosition);
			GraphNode graphNode = CreateObjectiveNode(objective, position);
			if (graphEditor.ContextMenu.ContextMenuArgs.TryGetPin(out var resultPin))
			{
				foreach (GraphPin pin in graphNode.Pins)
				{
					if (pin.IsCompatibleWith(resultPin))
					{
						graphEditor.Connect(resultPin, pin);
						break;
					}
				}
			}
			graphNode.SelectIndividually();
		}

		private void SpawnOutcomeNode(OutcomeType outType, Vector2 screenPosition)
		{
			Outcome outcome = SavedOutcome.Create(SavedOutcome.CreateSaved(outType, outType.ToString()));
			currentMission.RuntimeObjectives.AddNewOutcome(outcome);
			Vector2 position = graphEditor.ScreenToLocalPosition(screenPosition);
			GraphNode graphNode = CreateOutcomeNode(outcome, position);
			if (graphEditor.ContextMenu.ContextMenuArgs.TryGetPin(out var resultPin))
			{
				foreach (GraphPin pin in graphNode.Pins)
				{
					if (pin.IsCompatibleWith(resultPin))
					{
						graphEditor.Connect(resultPin, pin);
						break;
					}
				}
			}
			graphNode.SelectIndividually();
		}

		private void HandleConnectPins(GraphPin outputPin, GraphPin inputPin)
		{
			NodeType nodeType = outputPin.Node.Data.NodeType;
			NodeType nodeType2 = inputPin.Node.Data.NodeType;
			if (nodeType == Ids.Objective && nodeType2 == Ids.Outcome)
			{
				HandleConnectPinsObjectiveToOutcome(outputPin, inputPin);
			}
			else if (nodeType == Ids.Outcome && nodeType2 == Ids.Objective)
			{
				HandleConnectPinsOutcomeToObjective(outputPin, inputPin);
			}
			else
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError($"Unexpected node connection pair: {nodeType} -> {nodeType2}");
			}
		}

		private void HandleConnectPinsObjectiveToOutcome(GraphPin outputPin, GraphPin inputPin)
		{
			string outName = Ids.StripPrefixObjective(outputPin.Node.Data.ID.Value);
			string inName = Ids.StripPrefixOutcome(inputPin.Node.Data.ID.Value);
			Objective objective = currentMission.RuntimeObjectives.AllObjectives.Find((Objective o) => o.SavedObjective.UniqueName == outName);
			Outcome outcome = currentMission.RuntimeObjectives.AllOutcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == inName);
			if (objective == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find parent objective in model: " + outName);
			}
			if (outcome == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find outcome in model: " + inName);
			}
			if (objective != null && outcome != null && !objective.Outcomes.Contains(outcome))
			{
				currentMission.RuntimeObjectives.AddExistingOutcome(outcome, objective);
			}
		}

		private void HandleConnectPinsOutcomeToObjective(GraphPin outputPin, GraphPin inputPin)
		{
			string outName = Ids.StripPrefixOutcome(outputPin.Node.Data.ID.Value);
			string inName = Ids.StripPrefixObjective(inputPin.Node.Data.ID.Value);
			Outcome outcome = currentMission.RuntimeObjectives.AllOutcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == outName);
			Objective objective = currentMission.RuntimeObjectives.AllObjectives.Find((Objective o) => o.SavedObjective.UniqueName == inName);
			if (outcome == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find outcome in model: " + outName);
			}
			if (objective == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find target objective in model: " + inName);
			}
			if (outcome == null || objective == null)
			{
				return;
			}
			if (outcome is StartObjectiveOutcome startObjectiveOutcome)
			{
				StartObjectiveOutcome startObjectiveOutcome2 = startObjectiveOutcome;
				if (startObjectiveOutcome2.objectivesToStart == null)
				{
					startObjectiveOutcome2.objectivesToStart = new List<Objective>();
				}
				if (!startObjectiveOutcome.objectivesToStart.Contains(objective))
				{
					startObjectiveOutcome.objectivesToStart.Add(objective);
				}
			}
			else if (outcome is CompleteObjectiveOutcome completeObjectiveOutcome)
			{
				CompleteObjectiveOutcome completeObjectiveOutcome2 = completeObjectiveOutcome;
				if (completeObjectiveOutcome2.objectivesToStart == null)
				{
					completeObjectiveOutcome2.objectivesToStart = new List<Objective>();
				}
				if (!completeObjectiveOutcome.objectivesToStart.Contains(objective))
				{
					completeObjectiveOutcome.objectivesToStart.Add(objective);
				}
			}
		}

		private void HandleDisconnectPins(GraphPin outputPin, GraphPin inputPin)
		{
			NodeType nodeType = outputPin.Node.Data.NodeType;
			NodeType nodeType2 = inputPin.Node.Data.NodeType;
			if (nodeType == Ids.Objective && nodeType2 == Ids.Outcome)
			{
				HandleDisconnectPinsObjectiveToOutcome(outputPin, inputPin);
			}
			else if (nodeType == Ids.Outcome && nodeType2 == Ids.Objective)
			{
				HandleDisconnectPinsOutcomeToObjective(outputPin, inputPin);
			}
			else
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError($"Unexpected node disconnection pair: {nodeType} -> {nodeType2}");
			}
		}

		private void HandleDisconnectPinsObjectiveToOutcome(GraphPin outputPin, GraphPin inputPin)
		{
			string outName = Ids.StripPrefixObjective(outputPin.Node.Data.ID.Value);
			string inName = Ids.StripPrefixOutcome(inputPin.Node.Data.ID.Value);
			Objective objective = currentMission.RuntimeObjectives.AllObjectives.Find((Objective o) => o.SavedObjective.UniqueName == outName);
			Outcome outcome = currentMission.RuntimeObjectives.AllOutcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == inName);
			if (objective == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find parent objective in model: " + outName);
			}
			if (outcome == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find outcome in model: " + inName);
			}
			if (objective != null && outcome != null)
			{
				objective.Outcomes.Remove(outcome);
			}
		}

		private void HandleDisconnectPinsOutcomeToObjective(GraphPin outputPin, GraphPin inputPin)
		{
			string outName = Ids.StripPrefixOutcome(outputPin.Node.Data.ID.Value);
			string inName = Ids.StripPrefixObjective(inputPin.Node.Data.ID.Value);
			Outcome outcome = currentMission.RuntimeObjectives.AllOutcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == outName);
			Objective objective = currentMission.RuntimeObjectives.AllObjectives.Find((Objective o) => o.SavedObjective.UniqueName == inName);
			if (outcome == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find outcome in model: " + outName);
			}
			if (objective == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find target objective in model: " + inName);
			}
			if (outcome != null && objective != null)
			{
				if (outcome is StartObjectiveOutcome startObjectiveOutcome)
				{
					startObjectiveOutcome.objectivesToStart?.Remove(objective);
				}
				else if (outcome is CompleteObjectiveOutcome completeObjectiveOutcome)
				{
					completeObjectiveOutcome.objectivesToStart?.Remove(objective);
				}
			}
		}

		private void HandleSelectNode(GraphNode node)
		{
			if (node.Data.NodeType == Ids.Objective)
			{
				HandleSelectNodeObjective(node);
			}
			else if (node.Data.NodeType == Ids.Outcome)
			{
				HandleSelectNodeOutcome(node);
			}
			else
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError($"Unexpected node type for selection: {node.Data.NodeType}");
			}
		}

		private void HandleSelectNodeObjective(GraphNode node)
		{
			string name = Ids.StripPrefixObjective(node.Data.ID.Value);
			Objective objective = currentMission.RuntimeObjectives.AllObjectives.Find((Objective o) => o.SavedObjective.UniqueName == name);
			if (objective == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find objective in model: " + name);
			}
			else
			{
				selectedPanel.Create(editObjectivePanelPrefab).SetObjective(objective, PanelDrawOptions.Graph(SelectAndFocusNode));
			}
		}

		private void HandleSelectNodeOutcome(GraphNode node)
		{
			string name = Ids.StripPrefixOutcome(node.Data.ID.Value);
			Outcome outcome = currentMission.RuntimeObjectives.AllOutcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == name);
			if (outcome == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find outcome in model: " + name);
			}
			else
			{
				selectedPanel.Create(editOutcomePanelPrefab).SetOutcome(outcome, null, PanelDrawOptions.Graph(SelectAndFocusNode));
			}
		}

		private void HandleDeleteNode(GraphNode node)
		{
			if (node.Data.NodeType == Ids.Objective)
			{
				HandleDeleteNodeObjective(node);
			}
			else if (node.Data.NodeType == Ids.Outcome)
			{
				HandleDeleteNodeOutcome(node);
			}
			else
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError($"Unexpected node type for deletion: {node.Data.NodeType}");
			}
		}

		private void HandleDeleteNodeObjective(GraphNode node)
		{
			string name = Ids.StripPrefixObjective(node.Data.ID.Value);
			if (name == MissionObjectivesFactory.MissionStartName)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Cannot delete the special " + MissionObjectivesFactory.MissionStartName + " objective node.");
				return;
			}
			Objective objective = currentMission.RuntimeObjectives.AllObjectives.Find((Objective o) => o.SavedObjective.UniqueName == name);
			if (objective == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find objective in model for deletion: " + name);
				return;
			}
			objective.OnRenamed -= HandleNodeRenamed;
			nodeLookup.Remove(objective);
			currentMission.RuntimeObjectives.RemoveObjective(objective);
			selectedPanel.Destroy();
		}

		private void HandleDeleteNodeOutcome(GraphNode node)
		{
			string name = Ids.StripPrefixOutcome(node.Data.ID.Value);
			Outcome outcome = currentMission.RuntimeObjectives.AllOutcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == name);
			if (outcome == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find outcome in model for deletion: " + name);
				return;
			}
			outcome.OnRenamed -= HandleNodeRenamed;
			nodeLookup.Remove(outcome);
			currentMission.RuntimeObjectives.RemoveOutcome(outcome);
			selectedPanel.Destroy();
		}

		private void HandleSelectionChanged(GraphSelection selection)
		{
			if (selection.Type == GraphSelectionType.Nodes && selection.Nodes.Count == 1)
			{
				HandleSelectNode(selection.Nodes[0]);
			}
			else
			{
				selectedPanel?.Destroy();
			}
		}

		private void HandleDuplicateNode(GraphNode originalNode)
		{
			if (originalNode.Data.NodeType == Ids.Objective)
			{
				HandleDuplicateNodeObjective(originalNode);
			}
			else if (originalNode.Data.NodeType == Ids.Outcome)
			{
				HandleDuplicateNodeOutcome(originalNode);
			}
			else
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError($"Unexpected node type for duplication: {originalNode.Data.NodeType}");
			}
		}

		private void HandleDuplicateNodeObjective(GraphNode originalNode)
		{
			string originalName = Ids.StripPrefixObjective(originalNode.Data.ID.Value);
			if (originalName == MissionObjectivesFactory.MissionStartName)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Cannot duplicate the special " + MissionObjectivesFactory.MissionStartName + " objective node.");
				return;
			}
			Objective objective = currentMission.RuntimeObjectives.AllObjectives.Find((Objective o) => o.SavedObjective.UniqueName == originalName);
			if (objective == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find original objective in model for duplication: " + originalName);
				return;
			}
			Objective objective2 = SavedObjective.Create(SavedObjective.CreateSavedObjective(objective.SavedObjective.ObjectiveTypeEnum, objective.SavedObjective.UniqueName));
			objective2.CopyFrom(objective);
			currentMission.RuntimeObjectives.AddNewObjective(objective2);
			Vector2 position = originalNode.rectTransform.anchoredPosition + duplicateOffset;
			GraphNode duplicatedNode = CreateObjectiveNode(objective2, position);
			graphEditor.DuplicateNodeConnections(originalNode, duplicatedNode);
			objective2.Save();
		}

		private void HandleDuplicateNodeOutcome(GraphNode originalNode)
		{
			string originalName = Ids.StripPrefixOutcome(originalNode.Data.ID.Value);
			Outcome outcome = currentMission.RuntimeObjectives.AllOutcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == originalName);
			if (outcome == null)
			{
				ColorLog<ObjectiveOutcomeGraph>.LogError("Failed to find original outcome in model for duplication: " + originalName);
				return;
			}
			Outcome outcome2 = SavedOutcome.Create(SavedOutcome.CreateSaved(outcome.SavedOutcome.OutcomeTypeEnum, outcome.SavedOutcome.UniqueName));
			outcome2.CopyFrom(outcome);
			currentMission.RuntimeObjectives.AddNewOutcome(outcome2);
			Vector2 position = originalNode.rectTransform.anchoredPosition + duplicateOffset;
			GraphNode duplicatedNode = CreateOutcomeNode(outcome2, position);
			graphEditor.DuplicateNodeConnections(originalNode, duplicatedNode);
			outcome2.Save();
		}

		private GraphNode CreateObjectiveNode(Objective objective, Vector2 position)
		{
			NodeId iD = Ids.ObjectiveNodeId(objective.SavedObjective.UniqueName);
			Color color = ColorLog.ColorFromName(objective.SavedObjective.ObjectiveTypeEnum.ToString(), 0.2f, 1f);
			bool flag = objective.SavedObjective.UniqueName == MissionObjectivesFactory.MissionStartName;
			GraphNodeData graphNodeData = new GraphNodeData
			{
				ID = iD,
				TitleText = objective.SavedObjective.UniqueName,
				NodeType = Ids.Objective,
				TagText = objective.SavedObjective.ObjectiveTypeEnum.ToString(),
				Position = position,
				StopDelete = flag,
				StopDuplicate = flag,
				NodeColor = color,
				TagColor = color
			};
			if (!flag)
			{
				graphNodeData.InputElements.Add(new GraphPinData
				{
					PinId = Ids.StartObjectiveInput,
					DisplayName = "Activate",
					PinType = Ids.StartObjectiveInputType,
					AllowedConnectionTypes = new List<PinType> { Ids.StartOutcomeOutputType },
					AllowMultipleConnections = true
				});
				graphNodeData.InputElements.Add(new GraphPinData
				{
					PinId = Ids.CompleteObjectiveInput,
					DisplayName = "Complete",
					PinType = Ids.CompleteObjectiveInputType,
					AllowedConnectionTypes = new List<PinType> { Ids.CompleteOutcomeOutputType },
					AllowMultipleConnections = true
				});
			}
			graphNodeData.OutputElements.Add(new GraphPinData
			{
				PinId = Ids.Completed,
				PinType = Ids.ObjectiveOutput,
				AllowedConnectionTypes = new List<PinType> { Ids.OutcomeInput },
				AllowMultipleConnections = true
			});
			objective.AddPins(graphNodeData);
			GraphNode graphNode = graphEditor.InstantiateNode(graphNodeData);
			nodeLookup[objective] = graphNode;
			objective.OnRenamed += HandleNodeRenamed;
			return graphNode;
		}

		private GraphNode CreateOutcomeNode(Outcome outcome, Vector2 position)
		{
			NodeId iD = Ids.OutcomeNodeId(outcome.SavedOutcome.UniqueName);
			Color color = ColorLog.ColorFromName(outcome.SavedOutcome.OutcomeTypeEnum.ToString(), 0.8f, 1f);
			GraphNodeData graphNodeData = new GraphNodeData
			{
				ID = iD,
				TitleText = outcome.SavedOutcome.UniqueName,
				NodeType = Ids.Outcome,
				TagText = outcome.SavedOutcome.OutcomeTypeEnum.ToString(),
				Position = position,
				NodeColor = color,
				TagColor = color
			};
			graphNodeData.InputElements.Add(new GraphPinData
			{
				PinId = Ids.Execute,
				PinType = Ids.OutcomeInput,
				AllowedConnectionTypes = new List<PinType> { Ids.ObjectiveOutput }
			});
			outcome.AddPins(graphNodeData);
			GraphNode graphNode = graphEditor.InstantiateNode(graphNodeData);
			nodeLookup[outcome] = graphNode;
			outcome.OnRenamed += HandleNodeRenamed;
			return graphNode;
		}

		public void SelectAndFocusNode(ISaveableReference reference)
		{
			if (nodeLookup.TryGetValue(reference, out var value))
			{
				value.SelectIndividually();
				graphEditor.FocusNode(value);
			}
			else
			{
				ColorLog<ObjectiveOutcomeGraph>.InfoWarn($"Could not find {reference} to focus");
			}
		}

		private void HandleNodeRenamed(ISaveableReference reference, string oldName, string newName)
		{
			if (nodeLookup.TryGetValue(reference, out var value))
			{
				value.UpdateTitle(newName);
				if (reference is Objective)
				{
					value.Data.ID = Ids.ObjectiveNodeId(newName);
				}
				else if (reference is Outcome)
				{
					value.Data.ID = Ids.OutcomeNodeId(newName);
				}
			}
			else
			{
				ColorLog<ObjectiveOutcomeGraph>.InfoWarn($"Could not find {reference} (old name: {oldName}) to rename to {newName}");
			}
		}
	}
}
