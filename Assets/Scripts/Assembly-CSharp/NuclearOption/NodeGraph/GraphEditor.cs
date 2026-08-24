using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public class GraphEditor : MonoBehaviour
	{
		public delegate void PinConnectionDelegate(GraphPin outputPin, GraphPin inputPin);

		public delegate void SelectionChangedDelegate(GraphSelection selection);

		public delegate void DeleteNodeDelegate(GraphNode node);

		public delegate void DuplicateNodeDelegate(GraphNode node);

		[SerializeField]
		private RectTransform contentTransform;

		[SerializeField]
		private GraphNode nodePrefab;

		[SerializeField]
		private GraphContextMenu contextMenu;

		[SerializeField]
		private Canvas canvas;

		[SerializeField]
		private GraphConnectionRenderer connectionRenderer;

		private ContextMenuConfig nodeContextMenuCache;

		private ContextMenuConfig pinContextMenuCache;

		private ContextMenuConfig connectionContextMenuCache;

		private ContextMenuConfig backgroundMenuConfig;

		private GraphLayoutJson _currentLayout;

		private readonly List<GraphNode> selectedNodes = new List<GraphNode>();

		public Vector2 TempDragTarget { get; set; }

		public GraphPin DraggedPin { get; set; }

		public GraphPin SelectedPin { get; private set; }

		public GraphConnection? SelectedConnection { get; private set; }

		public List<GraphConnection> Connections { get; } = new List<GraphConnection>();

		public List<GraphNode> AllNodes { get; } = new List<GraphNode>();

		public Canvas Canvas => canvas;

		public GraphContextMenu ContextMenu => contextMenu;

		public IReadOnlyList<GraphNode> SelectedNodes => selectedNodes;

		public GraphLayoutJson CurrentLayout
		{
			get
			{
				return _currentLayout;
			}
			set
			{
				if (_currentLayout != null)
				{
					_currentLayout.SyncLayout = null;
				}
				_currentLayout = value;
				if (_currentLayout != null)
				{
					_currentLayout.SyncLayout = SyncLayout;
				}
			}
		}

		public static GraphEditor i { get; private set; }

		public event PinConnectionDelegate OnConnectPins;

		public event PinConnectionDelegate OnDisconnectPins;

		public event SelectionChangedDelegate OnSelectionChanged;

		public event DeleteNodeDelegate OnDeleteNode;

		public event DuplicateNodeDelegate OnDuplicateNode;

		private void SyncLayout()
		{
			_currentLayout.panPosition = contentTransform.anchoredPosition;
			_currentLayout.zoom = contentTransform.localScale.x;
			_currentLayout.nodes.Clear();
			foreach (GraphNode allNode in AllNodes)
			{
				_currentLayout.nodes.Add(new GraphNodeLayoutJson
				{
					nodeId = allNode.Data.ID,
					position = allNode.rectTransform.anchoredPosition
				});
			}
		}

		public void OnNodeSelectedInternal(GraphNode node, bool selected, bool notify = true)
		{
			if (selected)
			{
				if (selectedNodes.Contains(node))
				{
					ColorLog<GraphEditor>.LogError("Node " + node.name + " was already in selectedNodes list.");
				}
				else
				{
					selectedNodes.Add(node);
				}
			}
			else if (!selectedNodes.Remove(node))
			{
				ColorLog<GraphEditor>.LogError("Node " + node.name + " was not in selectedNodes list.");
			}
			if (notify)
			{
				this.OnSelectionChanged?.Invoke(GraphSelection.FromNodes(selectedNodes));
			}
		}

		public void ApplyPendingSelection()
		{
			bool flag = false;
			foreach (GraphNode allNode in AllNodes)
			{
				if (allNode.SelectionState == NodeSelectionState.PendingDragSelect)
				{
					allNode.SetSelected(NodeSelectionState.Selected, updateEditor: true, notify: false);
					flag = true;
				}
			}
			if (flag)
			{
				this.OnSelectionChanged?.Invoke(GraphSelection.FromNodes(selectedNodes));
			}
		}

		private void OnValidate()
		{
		}

		private void Awake()
		{
			i = this;
		}

		public void SelectPin(GraphPin pin)
		{
			ClearSelection(notify: false);
			SelectedPin = pin;
			SelectedPin.SetSelected(selected: true);
			this.OnSelectionChanged?.Invoke(GraphSelection.FromPin(SelectedPin));
		}

		public void SelectConnection(GraphConnection connection)
		{
			ClearSelection(notify: false);
			SelectedConnection = connection;
			this.OnSelectionChanged?.Invoke(GraphSelection.FromConnection(connection));
		}

		public void ClearSelection(bool notify = true)
		{
			foreach (GraphNode selectedNode in selectedNodes)
			{
				selectedNode.SetSelected(NodeSelectionState.None, updateEditor: false);
			}
			selectedNodes.Clear();
			if (SelectedPin != null)
			{
				SelectedPin.SetSelected(selected: false);
			}
			SelectedPin = null;
			SelectedConnection = null;
			connectionRenderer.SetVerticesDirty();
			if (notify)
			{
				this.OnSelectionChanged?.Invoke(GraphSelection.None());
			}
		}

		public void Connect(GraphPin source, GraphPin target)
		{
			GraphPin graphPin = ((source.Direction == PinDirection.Input) ? target : source);
			GraphPin graphPin2 = ((source.Direction == PinDirection.Input) ? source : target);
			if (Connections.Contains(new GraphConnection(source, target)))
			{
				ColorLog<GraphEditor>.InfoWarn($"Connection already exists between {graphPin} and {graphPin2}");
				return;
			}
			if (!graphPin2.AllowMultipleConnections)
			{
				for (int num = Connections.Count - 1; num >= 0; num--)
				{
					GraphConnection graphConnection = Connections[num];
					if (graphConnection.InputPin == graphPin2)
					{
						this.OnDisconnectPins?.Invoke(graphConnection.OutputPin, graphConnection.InputPin);
						Connections.RemoveAt(num);
					}
				}
			}
			if (!graphPin.AllowMultipleConnections)
			{
				for (int num2 = Connections.Count - 1; num2 >= 0; num2--)
				{
					GraphConnection graphConnection2 = Connections[num2];
					if (graphConnection2.OutputPin == graphPin)
					{
						this.OnDisconnectPins?.Invoke(graphConnection2.OutputPin, graphConnection2.InputPin);
						Connections.RemoveAt(num2);
					}
				}
			}
			Connections.Add(new GraphConnection(source, target));
			this.OnConnectPins?.Invoke(graphPin, graphPin2);
		}

		public void SetBackgroundMenuConfig(ContextMenuConfig config)
		{
			backgroundMenuConfig = config;
		}

		public void OpenContextMenuAtMouse(ContextMenuOpenSource args)
		{
			contextMenu.Open(args, backgroundMenuConfig, Input.mousePosition);
		}

		public GraphNode InstantiateNode(GraphNodeData data)
		{
			GraphNode graphNode = Object.Instantiate(nodePrefab, contentTransform);
			graphNode.Setup(data, this);
			return graphNode;
		}

		public void DestroyAllNodes()
		{
			ClearSelection();
			foreach (GraphNode item in new List<GraphNode>(AllNodes))
			{
				Object.Destroy(item.gameObject);
			}
			AllNodes.Clear();
			Connections.Clear();
		}

		public void ConnectVisual(GraphPin source, GraphPin target)
		{
			Connections.Add(new GraphConnection(source, target));
		}

		public Vector2 ScreenToLocalPosition(Vector2 screenPosition)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(contentTransform, screenPosition, null, out var localPoint);
			return localPoint;
		}

		public bool TryGetConnectionAtPosition(Vector2 screenPosition, out GraphConnection connection, float threshold = 12f)
		{
			Vector2 b = ScreenToLocalPosition(screenPosition);
			float num = threshold / contentTransform.localScale.x;
			float a = ((connectionRenderer != null) ? connectionRenderer.HandleDistance : 100f);
			foreach (GraphConnection connection2 in Connections)
			{
				Vector2 vector = contentTransform.InverseTransformPoint(connection2.OutputPin.RectTransform.position);
				Vector2 vector2 = contentTransform.InverseTransformPoint(connection2.InputPin.RectTransform.position);
				float num2 = Mathf.Abs(vector.x - vector2.x);
				float num3 = Mathf.Max(a, num2 * 0.5f);
				Vector2 p = vector + Vector2.right * num3;
				float num4 = Mathf.Max(a, num2 * 0.5f);
				Vector2 p2 = vector2 + Vector2.left * num4;
				int num5 = 15;
				for (int i = 0; i <= num5; i++)
				{
					float t = (float)i / (float)num5;
					if (Vector2.Distance(GraphConnectionRenderer.EvaluateCubicBezier(vector, p, p2, vector2, t), b) < num)
					{
						connection = connection2;
						return true;
					}
				}
			}
			connection = default(GraphConnection);
			return false;
		}

		public void DeleteSelectedNodes()
		{
			if (selectedNodes.Count == 0)
			{
				return;
			}
			List<GraphNode> list = new List<GraphNode>(selectedNodes);
			ClearSelection(notify: false);
			foreach (GraphNode item in list)
			{
				DeleteNode(item);
			}
			this.OnSelectionChanged?.Invoke(GraphSelection.None());
		}

		public void DeleteNode(GraphNode node)
		{
			if (node.Data.StopDelete)
			{
				ColorLog<GraphEditor>.Info($"{node.Data.ID} has StopDelete");
				return;
			}
			ColorLog<GraphEditor>.Info($"Deleting {node.Data.ID}");
			if (node.IsSelected)
			{
				node.SetSelected(NodeSelectionState.None);
			}
			for (int num = Connections.Count - 1; num >= 0; num--)
			{
				GraphConnection graphConnection = Connections[num];
				if (graphConnection.InputPin.Node == node || graphConnection.OutputPin.Node == node)
				{
					this.OnDisconnectPins?.Invoke(graphConnection.OutputPin, graphConnection.InputPin);
					Connections.RemoveAt(num);
				}
			}
			this.OnDeleteNode?.Invoke(node);
			Object.Destroy(node.gameObject);
		}

		public void Disconnect(GraphConnection connection)
		{
			int num = Connections.IndexOf(connection);
			if (num >= 0)
			{
				this.OnDisconnectPins?.Invoke(connection.OutputPin, connection.InputPin);
				Connections.RemoveAt(num);
			}
			else
			{
				ColorLog<GraphEditor>.InfoWarn($"No connection found for {connection.InputPin} {connection.OutputPin}");
			}
		}

		public void Disconnect(GraphPin pin)
		{
			for (int num = Connections.Count - 1; num >= 0; num--)
			{
				GraphConnection graphConnection = Connections[num];
				if (graphConnection.InputPin == pin || graphConnection.OutputPin == pin)
				{
					this.OnDisconnectPins?.Invoke(graphConnection.OutputPin, graphConnection.InputPin);
					Connections.RemoveAt(num);
				}
			}
		}

		public void DuplicateNodeConnections(GraphNode originalNode, GraphNode duplicatedNode)
		{
			foreach (GraphConnection item in new List<GraphConnection>(Connections))
			{
				GraphPin pin2;
				if (item.OutputPin.Node == originalNode)
				{
					if (item.InputPin.AllowMultipleConnections && duplicatedNode.TryGetPin(item.OutputPin.PinId, PinDirection.Output, out var pin))
					{
						Connect(pin, item.InputPin);
					}
				}
				else if (item.InputPin.Node == originalNode && item.OutputPin.AllowMultipleConnections && duplicatedNode.TryGetPin(item.InputPin.PinId, PinDirection.Input, out pin2))
				{
					Connect(item.OutputPin, pin2);
				}
			}
		}

		public void OpenContextMenuForNode(GraphNode node, Vector2 screenPos)
		{
			if (node == null)
			{
				return;
			}
			node.SelectIndividually();
			if (nodeContextMenuCache == null)
			{
				nodeContextMenuCache = new ContextMenuConfig
				{
					Title = "Node Options",
					Options = new List<ContextMenuOption>
					{
						new ContextMenuOption
						{
							optionId = new ContextMenuOptionId("Node Options", 0, isTopLevel: true),
							label = "Delete Node",
							onClick = null
						},
						new ContextMenuOption
						{
							optionId = new ContextMenuOptionId("Node Options", 1, isTopLevel: true),
							label = "Duplicate Node",
							onClick = null
						},
						new ContextMenuOption
						{
							optionId = new ContextMenuOptionId("Node Options", 2, isTopLevel: true),
							label = "Disconnect All Connections",
							onClick = null
						}
					}
				};
			}
			nodeContextMenuCache.Title = "Node: " + node.Data.TitleText;
			nodeContextMenuCache.Options[0].onClick = delegate
			{
				DeleteNode(node);
			};
			nodeContextMenuCache.Options[0].Disable = node.Data.StopDelete;
			nodeContextMenuCache.Options[1].onClick = delegate
			{
				DuplicateNode(node);
			};
			nodeContextMenuCache.Options[1].Disable = node.Data.StopDuplicate;
			nodeContextMenuCache.Options[2].onClick = delegate
			{
				foreach (GraphPin pin in node.Pins)
				{
					Disconnect(pin);
				}
			};
			contextMenu.Open(default(ContextMenuOpenSource), nodeContextMenuCache, screenPos);
		}

		public void DuplicateNode(GraphNode node)
		{
			if (node == null)
			{
				ColorLog<GraphEditor>.LogError("DuplicateNode called with null node");
			}
			else if (node.Data.StopDuplicate)
			{
				ColorLog<GraphEditor>.Info($"{node.Data.ID} has StopDuplicate");
			}
			else
			{
				this.OnDuplicateNode?.Invoke(node);
			}
		}

		public void OpenContextMenuForPin(GraphPin pin, Vector2 screenPos)
		{
			if (pin == null)
			{
				return;
			}
			SelectPin(pin);
			if (pinContextMenuCache == null)
			{
				pinContextMenuCache = new ContextMenuConfig
				{
					Options = new List<ContextMenuOption>()
				};
			}
			pinContextMenuCache.Title = $"Pin: {pin}";
			pinContextMenuCache.Options.Clear();
			pinContextMenuCache.Options.Add(new ContextMenuOption
			{
				optionId = new ContextMenuOptionId("Pin Options", 0, isTopLevel: true),
				label = "Disconnect All",
				onClick = delegate
				{
					Disconnect(pin);
				}
			});
			foreach (GraphConnection connection in Connections)
			{
				if (connection.InputPin == pin)
				{
					GraphPin outputPin = connection.OutputPin;
					GraphConnection capturedConn = connection;
					pinContextMenuCache.Options.Add(new ContextMenuOption
					{
						optionId = new ContextMenuOptionId("Pin Options", 1, isTopLevel: true),
						label = "Disconnect from: " + outputPin,
						onClick = delegate
						{
							Disconnect(capturedConn);
						}
					});
				}
				else if (connection.OutputPin == pin)
				{
					GraphPin inputPin = connection.InputPin;
					GraphConnection capturedConn2 = connection;
					pinContextMenuCache.Options.Add(new ContextMenuOption
					{
						optionId = new ContextMenuOptionId("Pin Options", 1, isTopLevel: true),
						label = "Disconnect from: " + inputPin,
						onClick = delegate
						{
							Disconnect(capturedConn2);
						}
					});
				}
			}
			contextMenu.Open(default(ContextMenuOpenSource), pinContextMenuCache, screenPos);
		}

		public void OpenContextMenuForConnection(GraphConnection connection, Vector2 screenPos)
		{
			SelectConnection(connection);
			if (connectionContextMenuCache == null)
			{
				connectionContextMenuCache = new ContextMenuConfig
				{
					Title = "Connection Options",
					Options = new List<ContextMenuOption>
					{
						new ContextMenuOption
						{
							optionId = new ContextMenuOptionId("Connection Options", 0, isTopLevel: true),
							label = "Delete Connection",
							onClick = null
						}
					}
				};
			}
			connectionContextMenuCache.Options[0].onClick = delegate
			{
				Disconnect(connection);
			};
			contextMenu.Open(default(ContextMenuOpenSource), connectionContextMenuCache, screenPos);
		}

		private void Update()
		{
			if (InputFieldChecker.InsideInputField)
			{
				return;
			}
			if (Input.GetKeyDown(KeyCode.Delete))
			{
				if (SelectedConnection.HasValue)
				{
					Disconnect(SelectedConnection.Value);
					ClearSelection();
				}
				else if (SelectedPin != null)
				{
					Disconnect(SelectedPin);
					ClearSelection();
				}
				else
				{
					DeleteSelectedNodes();
				}
			}
			if (Input.GetKeyDown(KeyCode.A))
			{
				FocusAll();
			}
			if (Input.GetKeyDown(KeyCode.F))
			{
				FocusSelected();
			}
		}

		public void FocusNode(GraphNode node)
		{
			Vector2 anchoredPosition = node.rectTransform.anchoredPosition;
			Vector2 size = node.rectTransform.rect.size;
			Vector2 pivot = node.rectTransform.pivot;
			Vector2 vector = anchoredPosition - Vector2.Scale(size, pivot);
			Vector2 vector2 = vector + size - vector;
			Vector2 vector3 = vector + vector2 * 0.5f;
			_ = ((RectTransform)contentTransform.parent).rect.size;
			float x = contentTransform.localScale.x;
			contentTransform.anchoredPosition = -vector3 * x;
		}

		public void FocusAll()
		{
			if (AllNodes.Count == 0)
			{
				return;
			}
			Vector2 vector = new Vector2(float.MaxValue, float.MaxValue);
			Vector2 vector2 = new Vector2(float.MinValue, float.MinValue);
			foreach (GraphNode allNode in AllNodes)
			{
				Vector2 anchoredPosition = allNode.rectTransform.anchoredPosition;
				Vector2 size = allNode.rectTransform.rect.size;
				Vector2 pivot = allNode.rectTransform.pivot;
				Vector2 vector3 = anchoredPosition - Vector2.Scale(size, pivot);
				Vector2 rhs = vector3 + size;
				vector = Vector2.Min(vector, vector3);
				vector2 = Vector2.Max(vector2, rhs);
			}
			if (!(vector.x > vector2.x) && !(vector.y > vector2.y))
			{
				Vector2 vector4 = vector2 - vector;
				Vector2 vector5 = vector + vector4 * 0.5f;
				float num = 80f;
				vector4 += new Vector2(num * 2f, num * 2f);
				Vector2 size2 = ((RectTransform)contentTransform.parent).rect.size;
				if (!(size2.x <= 0f) && !(size2.y <= 0f))
				{
					float a = size2.x / vector4.x;
					float b = size2.y / vector4.y;
					float value = Mathf.Min(a, b);
					value = Mathf.Clamp(value, 0.2f, 1.2f);
					contentTransform.localScale = new Vector3(value, value, 1f);
					contentTransform.anchoredPosition = -vector5 * value;
				}
			}
		}

		public void FocusSelected()
		{
			if (selectedNodes.Count == 0)
			{
				FocusAll();
				return;
			}
			Vector2 vector = new Vector2(float.MaxValue, float.MaxValue);
			Vector2 vector2 = new Vector2(float.MinValue, float.MinValue);
			foreach (GraphNode selectedNode in selectedNodes)
			{
				Vector2 anchoredPosition = selectedNode.rectTransform.anchoredPosition;
				Vector2 size = selectedNode.rectTransform.rect.size;
				Vector2 pivot = selectedNode.rectTransform.pivot;
				Vector2 vector3 = anchoredPosition - Vector2.Scale(size, pivot);
				Vector2 rhs = vector3 + size;
				vector = Vector2.Min(vector, vector3);
				vector2 = Vector2.Max(vector2, rhs);
			}
			if (!(vector.x > vector2.x) && !(vector.y > vector2.y))
			{
				Vector2 vector4 = vector2 - vector;
				Vector2 vector5 = vector + vector4 * 0.5f;
				float num = 80f;
				vector4 += new Vector2(num * 2f, num * 2f);
				Vector2 size2 = ((RectTransform)contentTransform.parent).rect.size;
				if (!(size2.x <= 0f) && !(size2.y <= 0f))
				{
					float a = size2.x / vector4.x;
					float b = size2.y / vector4.y;
					float value = Mathf.Min(a, b);
					value = Mathf.Clamp(value, 0.2f, 1.2f);
					contentTransform.localScale = new Vector3(value, value, 1f);
					contentTransform.anchoredPosition = -vector5 * value;
				}
			}
		}

		public void ApplyLayoutPanZoom()
		{
			contentTransform.anchoredPosition = CurrentLayout?.panPosition ?? ((Vector2)Vector3.zero);
			float num = CurrentLayout?.zoom ?? 1f;
			float num2 = Mathf.Clamp((num > 0.01f) ? num : 1f, 0.2f, 2f);
			contentTransform.localScale = new Vector3(num2, num2, 1f);
		}

		public void PruneLayout()
		{
			if (CurrentLayout?.nodes == null)
			{
				return;
			}
			for (int num = CurrentLayout.nodes.Count - 1; num >= 0; num--)
			{
				GraphNodeLayoutJson entry = CurrentLayout.nodes[num];
				if (!AllNodes.Exists((GraphNode n) => n.Data != null && n.Data.ID == entry.nodeId))
				{
					CurrentLayout.nodes.RemoveAt(num);
				}
			}
		}
	}
}
