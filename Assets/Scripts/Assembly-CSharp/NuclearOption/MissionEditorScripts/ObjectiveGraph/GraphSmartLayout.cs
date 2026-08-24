using System.Collections.Generic;
using NuclearOption.NodeGraph;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Outcomes;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts.ObjectiveGraph
{
	public static class GraphSmartLayout
	{
		private class LayoutNodeInfo
		{
			public NodeId nodeId;

			public float idealY;

			public float y;

			public bool isFixed;

			public float estimatedHeight;
		}

		private const string ObjectivePrefix = "Objective_";

		private const string OutcomePrefix = "Outcome_";

		private const float BaseNodeHeight = 120f;

		private const float HeightPerPin = 38f;

		public static void Compute(List<Objective> objectives, List<Outcome> outcomes, GraphLayoutJson layout, float columnSpacing = 350f, float rowSpacing = 180f)
		{
			if (objectives == null || outcomes == null || layout == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>();
			foreach (Objective objective2 in objectives)
			{
				if (objective2 != null && objective2.SavedObjective.UniqueName != null)
				{
					hashSet.Add(objective2.SavedObjective.UniqueName);
				}
			}
			Dictionary<string, float> dictionary = new Dictionary<string, float>();
			foreach (Objective objective3 in objectives)
			{
				if (objective3 != null && objective3.SavedObjective.UniqueName != null)
				{
					GraphNodeData graphNodeData = new GraphNodeData();
					objective3.AddPins(graphNodeData);
					int num = graphNodeData.InputElements.Count + graphNodeData.OutputElements.Count + 1;
					dictionary[objective3.SavedObjective.UniqueName] = 120f + (float)num * 38f;
				}
			}
			foreach (Outcome outcome3 in outcomes)
			{
				if (outcome3 != null && outcome3.SavedOutcome.UniqueName != null)
				{
					GraphNodeData graphNodeData2 = new GraphNodeData();
					outcome3.AddPins(graphNodeData2);
					int num2 = graphNodeData2.InputElements.Count + graphNodeData2.OutputElements.Count + 1;
					dictionary[outcome3.SavedOutcome.UniqueName] = 120f + (float)num2 * 38f;
				}
			}
			Dictionary<string, Vector2> dictionary2 = new Dictionary<string, Vector2>();
			foreach (Objective objective4 in objectives)
			{
				if (objective4 != null && objective4.SavedObjective.UniqueName != null)
				{
					NodeId nodeId = new NodeId("Objective_" + objective4.SavedObjective.UniqueName);
					if (layout.TryGetNodePosition(nodeId, out var position))
					{
						dictionary2[objective4.SavedObjective.UniqueName] = position;
					}
				}
			}
			foreach (Outcome outcome4 in outcomes)
			{
				if (outcome4 != null && outcome4.SavedOutcome.UniqueName != null)
				{
					NodeId nodeId2 = new NodeId("Outcome_" + outcome4.SavedOutcome.UniqueName);
					if (layout.TryGetNodePosition(nodeId2, out var position2))
					{
						dictionary2[outcome4.SavedOutcome.UniqueName] = position2;
					}
				}
			}
			Dictionary<string, int> dictionary3 = new Dictionary<string, int>();
			Queue<string> queue = new Queue<string>();
			HashSet<string> hashSet2 = new HashSet<string>();
			foreach (KeyValuePair<string, Vector2> item in dictionary2)
			{
				int value = Mathf.Max(0, Mathf.RoundToInt(item.Value.x / columnSpacing));
				dictionary3[item.Key] = value;
				hashSet2.Add(item.Key);
				queue.Enqueue(item.Key);
			}
			HashSet<string> hashSet3 = new HashSet<string>();
			foreach (Outcome outcome5 in outcomes)
			{
				List<Objective> list = null;
				if (outcome5 is StartObjectiveOutcome startObjectiveOutcome)
				{
					list = startObjectiveOutcome.objectivesToStart;
				}
				else if (outcome5 is CompleteObjectiveOutcome completeObjectiveOutcome)
				{
					list = completeObjectiveOutcome.objectivesToStart;
				}
				if (list == null)
				{
					continue;
				}
				foreach (Objective item2 in list)
				{
					if (item2 != null && item2.SavedObjective.UniqueName != null)
					{
						hashSet3.Add(item2.SavedObjective.UniqueName);
					}
				}
			}
			foreach (Objective objective5 in objectives)
			{
				if (objective5 == null || objective5.SavedObjective.UniqueName == null)
				{
					continue;
				}
				string uniqueName = objective5.SavedObjective.UniqueName;
				bool flag = uniqueName == MissionObjectivesFactory.MissionStartName;
				if (flag || !hashSet3.Contains(uniqueName))
				{
					if (!hashSet2.Contains(uniqueName))
					{
						dictionary3[uniqueName] = 0;
						queue.Enqueue(uniqueName);
						hashSet2.Add(uniqueName);
					}
					else if (flag)
					{
						dictionary3[uniqueName] = 0;
					}
				}
			}
			while (queue.Count > 0)
			{
				string current10 = queue.Dequeue();
				int num3 = dictionary3[current10];
				Objective objective = objectives.Find((Objective o) => o.SavedObjective.UniqueName == current10);
				if (objective != null)
				{
					foreach (Outcome outcome6 in objective.Outcomes)
					{
						if (outcome6 != null && outcome6.SavedOutcome.UniqueName != null)
						{
							string uniqueName2 = outcome6.SavedOutcome.UniqueName;
							if (!hashSet2.Contains(uniqueName2))
							{
								dictionary3[uniqueName2] = num3 + 1;
								queue.Enqueue(uniqueName2);
								hashSet2.Add(uniqueName2);
							}
						}
					}
					continue;
				}
				Outcome outcome = outcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == current10);
				if (outcome == null)
				{
					continue;
				}
				List<Objective> list2 = null;
				if (outcome is StartObjectiveOutcome startObjectiveOutcome2)
				{
					list2 = startObjectiveOutcome2.objectivesToStart;
				}
				else if (outcome is CompleteObjectiveOutcome completeObjectiveOutcome2)
				{
					list2 = completeObjectiveOutcome2.objectivesToStart;
				}
				if (list2 == null)
				{
					continue;
				}
				foreach (Objective item3 in list2)
				{
					if (item3 != null && item3.SavedObjective.UniqueName != null)
					{
						string uniqueName3 = item3.SavedObjective.UniqueName;
						if (!hashSet2.Contains(uniqueName3))
						{
							dictionary3[uniqueName3] = num3 + 1;
							queue.Enqueue(uniqueName3);
							hashSet2.Add(uniqueName3);
						}
					}
				}
			}
			foreach (Objective objective6 in objectives)
			{
				if (objective6 != null && objective6.SavedObjective.UniqueName != null)
				{
					string uniqueName4 = objective6.SavedObjective.UniqueName;
					if (!dictionary3.ContainsKey(uniqueName4))
					{
						dictionary3[uniqueName4] = 0;
					}
				}
			}
			foreach (Outcome outcome7 in outcomes)
			{
				if (outcome7 != null && outcome7.SavedOutcome.UniqueName != null)
				{
					string uniqueName5 = outcome7.SavedOutcome.UniqueName;
					if (!dictionary3.ContainsKey(uniqueName5))
					{
						dictionary3[uniqueName5] = 1;
					}
				}
			}
			int num4 = 0;
			foreach (KeyValuePair<string, int> item4 in dictionary3)
			{
				if (item4.Value > num4)
				{
					num4 = item4.Value;
				}
			}
			int value2 = num4 + 1;
			foreach (Outcome outcome8 in outcomes)
			{
				if (outcome8 is EndGameOutcome && outcome8.SavedOutcome.UniqueName != null)
				{
					dictionary3[outcome8.SavedOutcome.UniqueName] = value2;
				}
			}
			int num5 = 0;
			foreach (KeyValuePair<string, int> item5 in dictionary3)
			{
				if (item5.Value > num5)
				{
					num5 = item5.Value;
				}
			}
			Dictionary<int, List<string>> dictionary4 = new Dictionary<int, List<string>>();
			foreach (KeyValuePair<string, int> item6 in dictionary3)
			{
				int value3 = item6.Value;
				if (!dictionary4.ContainsKey(value3))
				{
					dictionary4[value3] = new List<string>();
				}
				dictionary4[value3].Add(item6.Key);
			}
			Dictionary<int, List<LayoutNodeInfo>> dictionary5 = new Dictionary<int, List<LayoutNodeInfo>>();
			for (int num6 = 0; num6 <= num5; num6++)
			{
				if (!dictionary4.ContainsKey(num6))
				{
					continue;
				}
				List<string> list3 = dictionary4[num6];
				float num7 = rowSpacing;
				foreach (string item7 in list3)
				{
					if (dictionary.TryGetValue(item7, out var value4) && value4 > num7)
					{
						num7 = value4;
					}
				}
				List<LayoutNodeInfo> list4 = new List<LayoutNodeInfo>();
				foreach (string name in list3)
				{
					float y = 0f;
					int num8 = 0;
					Vector2 value5;
					bool flag2 = dictionary2.TryGetValue(name, out value5);
					if (name == MissionObjectivesFactory.MissionStartName)
					{
						flag2 = false;
					}
					if (flag2)
					{
						y = value5.y;
					}
					else if (num6 > 0)
					{
						float num9 = 0f;
						Objective obj = objectives.Find((Objective o) => o.SavedObjective.UniqueName == name);
						if (obj != null)
						{
							foreach (Outcome outcome9 in outcomes)
							{
								if (outcome9 == null || outcome9.SavedOutcome.UniqueName == null)
								{
									continue;
								}
								List<Objective> list5 = null;
								if (outcome9 is StartObjectiveOutcome startObjectiveOutcome3)
								{
									list5 = startObjectiveOutcome3.objectivesToStart;
								}
								else if (outcome9 is CompleteObjectiveOutcome completeObjectiveOutcome3)
								{
									list5 = completeObjectiveOutcome3.objectivesToStart;
								}
								if (list5 == null || !list5.Exists((Objective t) => t.SavedObjective.UniqueName == obj.SavedObjective.UniqueName))
								{
									continue;
								}
								string parentName = outcome9.SavedOutcome.UniqueName;
								if (dictionary3.TryGetValue(parentName, out var value6) && value6 == num6 - 1 && dictionary5.TryGetValue(num6 - 1, out var value7))
								{
									LayoutNodeInfo layoutNodeInfo = value7.Find((LayoutNodeInfo layoutNodeInfo3) => layoutNodeInfo3.nodeId.Value == "Outcome_" + parentName);
									if (layoutNodeInfo != null)
									{
										num9 += layoutNodeInfo.y;
										num8++;
									}
								}
							}
						}
						else
						{
							Outcome outcome2 = outcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == name);
							if (outcome2 != null)
							{
								foreach (Objective objective7 in objectives)
								{
									if (objective7 == null || !objective7.Outcomes.Contains(outcome2))
									{
										continue;
									}
									string parentName2 = objective7.SavedObjective.UniqueName;
									if (dictionary3.TryGetValue(parentName2, out var value8) && value8 == num6 - 1 && dictionary5.TryGetValue(num6 - 1, out var value9))
									{
										LayoutNodeInfo layoutNodeInfo2 = value9.Find((LayoutNodeInfo layoutNodeInfo3) => layoutNodeInfo3.nodeId.Value == "Objective_" + parentName2);
										if (layoutNodeInfo2 != null)
										{
											num9 += layoutNodeInfo2.y;
											num8++;
										}
									}
								}
							}
						}
						if (num8 > 0)
						{
							y = num9 / (float)num8;
						}
					}
					else
					{
						int count = list3.Count;
						y = ((float)list3.IndexOf(name) - (float)(count - 1) / 2f) * (0f - num7);
					}
					dictionary.TryGetValue(name, out var value10);
					list4.Add(new LayoutNodeInfo
					{
						nodeId = new NodeId((hashSet.Contains(name) ? "Objective_" : "Outcome_") + name),
						y = y,
						isFixed = flag2,
						estimatedHeight = Mathf.Max(value10, rowSpacing)
					});
				}
				if (num6 > 0)
				{
					RelaxColumn(list4, num7);
				}
				dictionary5[num6] = list4;
			}
			int num10 = 2;
			for (int num11 = 0; num11 < num10; num11++)
			{
				for (int num12 = num5 - 1; num12 >= 0; num12--)
				{
					if (dictionary5.ContainsKey(num12))
					{
						float columnRowSpacing = GetColumnRowSpacing(dictionary4[num12], dictionary, rowSpacing);
						List<LayoutNodeInfo> list6 = dictionary5[num12];
						foreach (LayoutNodeInfo item8 in list6)
						{
							if (!item8.isFixed)
							{
								float averageChildrenY = GetAverageChildrenY(item8.nodeId.Value, objectives, outcomes, dictionary5, dictionary3, num12);
								item8.y = Mathf.Lerp(item8.y, averageChildrenY, 0.5f);
							}
						}
						RelaxColumn(list6, columnRowSpacing);
					}
				}
				for (int num13 = 1; num13 <= num5; num13++)
				{
					if (!dictionary5.ContainsKey(num13))
					{
						continue;
					}
					float columnRowSpacing2 = GetColumnRowSpacing(dictionary4[num13], dictionary, rowSpacing);
					List<LayoutNodeInfo> list7 = dictionary5[num13];
					foreach (LayoutNodeInfo item9 in list7)
					{
						if (!item9.isFixed)
						{
							float averageParentsY = GetAverageParentsY(item9.nodeId.Value, objectives, outcomes, dictionary5, dictionary3, num13);
							item9.y = Mathf.Lerp(item9.y, averageParentsY, 0.5f);
						}
					}
					RelaxColumn(list7, columnRowSpacing2);
				}
			}
			for (int num14 = 0; num14 <= num5; num14++)
			{
				if (!dictionary5.ContainsKey(num14))
				{
					continue;
				}
				foreach (LayoutNodeInfo item10 in dictionary5[num14])
				{
					if (!item10.isFixed)
					{
						float x = (float)num14 * columnSpacing;
						Vector2 position3 = new Vector2(x, item10.y);
						layout.nodes.Add(new GraphNodeLayoutJson
						{
							nodeId = item10.nodeId,
							position = position3
						});
					}
				}
			}
		}

		private static float GetColumnRowSpacing(List<string> nodeNames, Dictionary<string, float> nodeHeights, float defaultRowSpacing)
		{
			float num = defaultRowSpacing;
			foreach (string nodeName in nodeNames)
			{
				if (nodeHeights.TryGetValue(nodeName, out var value) && value > num)
				{
					num = value;
				}
			}
			return num;
		}

		private static float GetNodeY(string name, Dictionary<int, List<LayoutNodeInfo>> layoutData, Dictionary<string, int> levels)
		{
			if (levels.TryGetValue(name, out var value) && layoutData.TryGetValue(value, out var value2))
			{
				LayoutNodeInfo layoutNodeInfo = value2.Find((LayoutNodeInfo x) => x.nodeId.Value == "Objective_" + name || x.nodeId.Value == "Outcome_" + name);
				if (layoutNodeInfo != null)
				{
					return layoutNodeInfo.y;
				}
			}
			return 0f;
		}

		private static float GetAverageChildrenY(string name, List<Objective> objectives, List<Outcome> outcomes, Dictionary<int, List<LayoutNodeInfo>> layoutData, Dictionary<string, int> levels, int col)
		{
			float num = 0f;
			int num2 = 0;
			Objective objective = objectives.Find((Objective o) => o.SavedObjective.UniqueName == name);
			if (objective != null)
			{
				foreach (Outcome outcome2 in objective.Outcomes)
				{
					if (outcome2 != null && outcome2.SavedOutcome.UniqueName != null)
					{
						string uniqueName = outcome2.SavedOutcome.UniqueName;
						if (levels.TryGetValue(uniqueName, out var value) && value == col + 1)
						{
							num += GetNodeY(uniqueName, layoutData, levels);
							num2++;
						}
					}
				}
			}
			else
			{
				Outcome outcome = outcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == name);
				if (outcome != null)
				{
					List<Objective> list = null;
					if (outcome is StartObjectiveOutcome startObjectiveOutcome)
					{
						list = startObjectiveOutcome.objectivesToStart;
					}
					else if (outcome is CompleteObjectiveOutcome completeObjectiveOutcome)
					{
						list = completeObjectiveOutcome.objectivesToStart;
					}
					if (list != null)
					{
						foreach (Objective item in list)
						{
							if (item != null && item.SavedObjective.UniqueName != null)
							{
								string uniqueName2 = item.SavedObjective.UniqueName;
								if (levels.TryGetValue(uniqueName2, out var value2) && value2 == col + 1)
								{
									num += GetNodeY(uniqueName2, layoutData, levels);
									num2++;
								}
							}
						}
					}
				}
			}
			if (num2 <= 0)
			{
				return GetNodeY(name, layoutData, levels);
			}
			return num / (float)num2;
		}

		private static float GetAverageParentsY(string name, List<Objective> objectives, List<Outcome> outcomes, Dictionary<int, List<LayoutNodeInfo>> layoutData, Dictionary<string, int> levels, int col)
		{
			float num = 0f;
			int num2 = 0;
			Objective obj = objectives.Find((Objective o) => o.SavedObjective.UniqueName == name);
			if (obj != null)
			{
				foreach (Outcome outcome2 in outcomes)
				{
					if (outcome2 == null || outcome2.SavedOutcome.UniqueName == null)
					{
						continue;
					}
					List<Objective> list = null;
					if (outcome2 is StartObjectiveOutcome startObjectiveOutcome)
					{
						list = startObjectiveOutcome.objectivesToStart;
					}
					else if (outcome2 is CompleteObjectiveOutcome completeObjectiveOutcome)
					{
						list = completeObjectiveOutcome.objectivesToStart;
					}
					if (list != null && list.Exists((Objective t) => t.SavedObjective.UniqueName == obj.SavedObjective.UniqueName))
					{
						string uniqueName = outcome2.SavedOutcome.UniqueName;
						if (levels.TryGetValue(uniqueName, out var value) && value == col - 1)
						{
							num += GetNodeY(uniqueName, layoutData, levels);
							num2++;
						}
					}
				}
			}
			else
			{
				Outcome outcome = outcomes.Find((Outcome o) => o.SavedOutcome.UniqueName == name);
				if (outcome != null)
				{
					foreach (Objective objective in objectives)
					{
						if (objective != null && objective.Outcomes.Contains(outcome))
						{
							string uniqueName2 = objective.SavedObjective.UniqueName;
							if (levels.TryGetValue(uniqueName2, out var value2) && value2 == col - 1)
							{
								num += GetNodeY(uniqueName2, layoutData, levels);
								num2++;
							}
						}
					}
				}
			}
			if (num2 <= 0)
			{
				return GetNodeY(name, layoutData, levels);
			}
			return num / (float)num2;
		}

		private static void RelaxColumn(List<LayoutNodeInfo> columnNodes, float rowSpacing)
		{
			columnNodes.Sort((LayoutNodeInfo a, LayoutNodeInfo b) => b.y.CompareTo(a.y));
			int count = columnNodes.Count;
			float[] array = new float[count];
			for (int num = 0; num < count; num++)
			{
				array[num] = columnNodes[num].y;
			}
			int num2 = 30;
			for (int num3 = 0; num3 < num2; num3++)
			{
				for (int num4 = 1; num4 < count; num4++)
				{
					float num5 = Mathf.Max(rowSpacing, columnNodes[num4 - 1].estimatedHeight);
					float num6 = array[num4 - 1] - num5;
					if (!(array[num4] > num6))
					{
						continue;
					}
					float num7 = array[num4] - num6;
					if (!columnNodes[num4 - 1].isFixed || !columnNodes[num4].isFixed)
					{
						if (columnNodes[num4 - 1].isFixed)
						{
							array[num4] -= num7;
							continue;
						}
						if (columnNodes[num4].isFixed)
						{
							array[num4 - 1] += num7;
							continue;
						}
						array[num4 - 1] += num7 * 0.5f;
						array[num4] -= num7 * 0.5f;
					}
				}
				for (int num8 = count - 2; num8 >= 0; num8--)
				{
					float num9 = Mathf.Max(rowSpacing, columnNodes[num8].estimatedHeight);
					float num10 = array[num8] - num9;
					if (array[num8 + 1] > num10)
					{
						float num11 = array[num8 + 1] - num10;
						if (!columnNodes[num8].isFixed || !columnNodes[num8 + 1].isFixed)
						{
							if (columnNodes[num8].isFixed)
							{
								array[num8 + 1] -= num11;
							}
							else if (columnNodes[num8 + 1].isFixed)
							{
								array[num8] += num11;
							}
							else
							{
								array[num8] += num11 * 0.5f;
								array[num8 + 1] -= num11 * 0.5f;
							}
						}
					}
				}
			}
			for (int num12 = 0; num12 < count; num12++)
			{
				columnNodes[num12].y = array[num12];
			}
		}
	}
}
