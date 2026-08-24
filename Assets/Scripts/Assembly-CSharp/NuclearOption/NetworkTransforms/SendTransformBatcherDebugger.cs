using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NuclearOption.DebugScripts;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class SendTransformBatcherDebugger : MonoBehaviour
	{
		private delegate ref T GetField<T>(DisplaySnapshot unit);

		private class DebugUnit
		{
			public readonly NetworkTransformBase NetTransform;

			public readonly DisplaySnapshot displaySnapshot;

			public bool Active => NetTransform.debugActive;

			public DebugUnit(NetworkTransformBase unit)
			{
				NetTransform = unit;
				if (displaySnapshot == null)
				{
					displaySnapshot = new DisplaySnapshot(unit.SnapshotBuffer, unit.name, unit.SendBatcher.LineRendererPrefab);
				}
			}
		}

		public struct Gui
		{
			public string snapType;

			public Vector3 rb_MovePosition;

			public Quaternion rb_MoveRotation;

			public Vector3 rb_velocity;

			public float influence_position;

			public float influence_velocity;

			public float influence_acceleration;

			public Vector3 snapshot_velocity;

			public Vector3 snapshot_acceleration;

			public void CalculateInfluence(NetworkTransformBase.ViewSnapshot snapshot, Vector3 oldPos)
			{
				Vector3 vector = snapshot.Position - oldPos;
				vector -= snapshot_velocity;
				influence_position = (vector - snapshot_acceleration).magnitude;
				influence_velocity = snapshot_velocity.magnitude;
				influence_acceleration = snapshot_acceleration.magnitude;
				float num = influence_position + influence_velocity + influence_acceleration;
				influence_position /= num;
				influence_velocity /= num;
				influence_acceleration /= num;
				rb_MovePosition = snapshot.Position;
				rb_MoveRotation = snapshot.Rotation;
				rb_velocity = snapshot.Velocity;
			}
		}

		public static class DebugSnapshotWriter
		{
			private static StreamWriter writer;

			private static StringBuilder builder = new StringBuilder();

			public static void Debug_WriteSnapshots(Aircraft aircraft, double smoothTime, Action<Action<object>> writeValues)
			{
				if (writer == null)
				{
					writer = new StreamWriter($"./SmoothTransform_{DateTime.Now:HH-mm--ss}.csv")
					{
						AutoFlush = true
					};
					writer.WriteLine("{NetworkTime},{smoothTime},{previous.timestamp},{previous.snapshot.globalPos},{previous.snapshot.velocity},{current.timestamp},{current.snapshot.globalPos},{current.snapshot.velocity}");
				}
				writer.Write($"{aircraft.NetworkTime.Time},{smoothTime},");
				writeValues(delegate(object value)
				{
					writer.Write($"{value},");
				});
				writer.Write("\n");
			}
		}

		private class DisplaySnapshot
		{
			private Dictionary<double, (GameObject go, Material mat)> proxies = new Dictionary<double, (GameObject, Material)>();

			private readonly string name;

			private readonly LineRenderer lineRendererPrefab;

			private readonly NetworkTransformBase.SnapshotBufferLocalSnapshot snapshotBuffer;

			public bool Boxes;

			public bool Extrapolate;

			public bool LinePath;

			public bool InterpolationMarker;

			private GameObject interpolationSphereMarker;

			private int colorIndex;

			private Stack<(GameObject go, Material mat)> pool = new Stack<(GameObject, Material)>();

			private LineRenderer lineRenderer;

			private Vector3[] positionList = new Vector3[1000];

			private LineRenderer pathRenderer;

			public bool AnyShowing()
			{
				if (!Boxes && !LinePath)
				{
					return InterpolationMarker;
				}
				return true;
			}

			public (GameObject go, Material mat) CreateProxy(Vector3 scale, PrimitiveType primitiveType = PrimitiveType.Cube)
			{
				GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
				gameObject.name = name;
				gameObject.transform.localScale = scale;
				Renderer component = gameObject.GetComponent<Renderer>();
				Material material = component.material;
				material.color = Color.red;
				component.material = material;
				if (gameObject.TryGetComponent<Collider>(out var component2))
				{
					component2.enabled = false;
				}
				return (go: gameObject, mat: material);
			}

			public DisplaySnapshot(NetworkTransformBase.SnapshotBufferLocalSnapshot snapshotBuffer, string name, LineRenderer lineRendererPrefab)
			{
				this.name = name;
				this.lineRendererPrefab = lineRendererPrefab;
				this.snapshotBuffer = snapshotBuffer;
			}

			public void VisualUpdate(double snapshotTime, double extrapolationTime, float maxAge)
			{
				if (snapshotBuffer.Count > 0)
				{
					if (Boxes)
					{
						CreateNew(extrapolationTime, maxAge);
					}
					if (LinePath && CloseToCamera(snapshotBuffer))
					{
						DrawPathLine(snapshotTime, extrapolationTime, maxAge);
					}
					if (InterpolationMarker)
					{
						if (interpolationSphereMarker == null)
						{
							(GameObject, Material) tuple = CreateProxy(Vector3.one, PrimitiveType.Sphere);
							(interpolationSphereMarker, _) = tuple;
							tuple.Item2.color = Color.green;
						}
						NetworkTransformBase.ViewSnapshot snapshotForTime = snapshotBuffer.GetSnapshotForTime(snapshotTime);
						interpolationSphereMarker.transform.position = snapshotForTime.Position;
					}
				}
				RemoveOld(snapshotTime);
			}

			private static bool CloseToCamera(NetworkTransformBase.SnapshotBufferLocalSnapshot snapshotBuffer)
			{
				Camera mainCamera = SceneSingleton<CameraStateManager>.i.mainCamera;
				Vector3 vector = snapshotBuffer.Get(snapshotBuffer.Count - 1).Snapshot.globalPos.ToLocalPosition();
				if (!FastMath.InRange(mainCamera.transform.position, vector, 1000f))
				{
					return false;
				}
				Vector3 vector2 = mainCamera.WorldToViewportPoint(vector);
				if (vector2.z > -100f && -0.1f <= vector2.x && vector2.x <= 1.1f && -0.1f <= vector2.y)
				{
					return vector2.y <= 1.1f;
				}
				return false;
			}

			private void DrawPathLine(double snapshotTime, double extrapolationTime, float maxAge)
			{
				double num = extrapolationTime - snapshotTime;
				int num2 = 0;
				double timestamp = snapshotBuffer.Get(0).Timestamp;
				double timestamp2 = snapshotBuffer.Get(snapshotBuffer.Count - 1).Timestamp;
				for (double num3 = timestamp; num3 < timestamp2; num3 += 0.0010000000474974513)
				{
					NetworkTransformBase.ViewSnapshot snapshotForTime = snapshotBuffer.GetSnapshotForTime(num3);
					NetworkTransformBase.ViewSnapshot viewSnapshot = snapshotForTime.Extrapolate(snapshotForTime.Timestamp + num, maxAge);
					if (positionList.Length <= num2)
					{
						Array.Resize(ref positionList, positionList.Length * 2);
					}
					positionList[num2] = viewSnapshot.Position.ToGlobalPosition().AsVector3();
					num2++;
				}
				if (pathRenderer == null)
				{
					pathRenderer = UnityEngine.Object.Instantiate(lineRendererPrefab, Datum.origin);
					pathRenderer.name = name + "_pathRenderer";
				}
				pathRenderer.positionCount = num2;
				pathRenderer.SetPositions(positionList);
			}

			private void DrawLinesBetweenSnapshots()
			{
				Color color = (Extrapolate ? Color.green : Color.blue);
				for (int i = 1; i < snapshotBuffer.Count; i++)
				{
					SnapshotBuffer<NetworkTransformBase.LocalSnapshot>.TimedSnapshot timedSnapshot = snapshotBuffer.Get(i);
					SnapshotBuffer<NetworkTransformBase.LocalSnapshot>.TimedSnapshot timedSnapshot2 = snapshotBuffer.Get(i - 1);
					Debug.DrawLine(timedSnapshot.Snapshot.globalPos.ToLocalPosition(), timedSnapshot2.Snapshot.globalPos.ToLocalPosition(), color);
				}
			}

			private void CreateNew(double extrapolationTime, float maxAge)
			{
				for (int i = 1; i < snapshotBuffer.Count; i++)
				{
					SnapshotBuffer<NetworkTransformBase.LocalSnapshot>.TimedSnapshot timedSnapshot = snapshotBuffer.Get(i);
					if (!proxies.ContainsKey(timedSnapshot.Timestamp))
					{
						GameObject proxy = GetProxy(timedSnapshot.Timestamp, Extrapolate);
						if (Extrapolate)
						{
							NetworkTransformBase.ViewSnapshot viewSnapshot = snapshotBuffer.AfterCurrent(i).Extrapolate(extrapolationTime, maxAge);
							proxy.transform.SetPositionAndRotation(viewSnapshot.Position, viewSnapshot.Rotation);
						}
						else
						{
							proxy.transform.SetPositionAndRotation(timedSnapshot.Snapshot.globalPos.ToLocalPosition(), timedSnapshot.Snapshot.rotation ?? Quaternion.identity);
						}
					}
				}
			}

			private GameObject GetProxy(double timestamp, bool extrapolate)
			{
				(GameObject, Material) tuple;
				if (pool.Count == 0)
				{
					Vector3 scale = new Vector3(10f, 2f, 4f) * 0.5f;
					tuple = CreateProxy(scale);
					tuple.Item1.transform.localScale = tuple.Item1.transform.localScale * 0.7f;
				}
				else
				{
					tuple = pool.Pop();
					tuple.Item1.SetActive(value: true);
				}
				float num = (float)colorIndex / 10f * 0.8f;
				colorIndex++;
				if (colorIndex > 10)
				{
					colorIndex = 0;
				}
				var (result, material) = tuple;
				if (extrapolate)
				{
					material.color = new Color(num, 1f, num, 0.5f);
				}
				else
				{
					material.color = new Color(num, num, 1f, 0.5f);
				}
				proxies.Add(timestamp, tuple);
				return result;
			}

			private void RemoveOld(double removeTime)
			{
				Span<double> span = stackalloc double[proxies.Count];
				int num = 0;
				foreach (KeyValuePair<double, (GameObject, Material)> proxy in proxies)
				{
					double key = proxy.Key;
					if (key < removeTime - 2.0)
					{
						span[num] = key;
						num++;
					}
				}
				for (int i = 0; i < num; i++)
				{
					double key2 = span[i];
					(GameObject, Material) item = proxies[key2];
					proxies.Remove(key2);
					pool.Push(item);
					item.Item1.SetActive(value: false);
				}
			}
		}

		private DebugUnit followingUnit;

		private readonly List<DebugUnit> drawList = new List<DebugUnit>();

		private readonly List<DebugUnit> tempList = new List<DebugUnit>();

		private readonly List<Unit> tmpUnits = new List<Unit>();

		private DebugUnit FindOrCreate(NetworkTransformBase networkTransform)
		{
			foreach (DebugUnit draw in drawList)
			{
				if (draw.NetTransform == networkTransform)
				{
					return draw;
				}
			}
			DebugUnit debugUnit = new DebugUnit(networkTransform);
			drawList.Add(debugUnit);
			return debugUnit;
		}

		public void UpdateDebugFollow(ref VisualUpdateTime visualTime)
		{
			if (!base.enabled)
			{
				base.enabled = true;
			}
			NetworkTransformBase networkTransformBase = null;
			if (SceneSingleton<CameraStateManager>.i.followingUnit != null && !SceneSingleton<CameraStateManager>.i.followingUnit.HasAuthority && SceneSingleton<CameraStateManager>.i.followingUnit.TryGetComponent<NetworkTransformBase>(out var component))
			{
				networkTransformBase = component;
			}
			if (followingUnit?.NetTransform != networkTransformBase)
			{
				if (followingUnit != null && followingUnit.NetTransform != null && !followingUnit.displaySnapshot.AnyShowing())
				{
					followingUnit.NetTransform.debugActive = false;
				}
				if (networkTransformBase != null)
				{
					DebugUnit debugUnit = FindOrCreate(networkTransformBase);
					networkTransformBase.debugActive = true;
					followingUnit = debugUnit;
				}
				else
				{
					followingUnit = null;
				}
			}
			CheckInput();
			UpdateDebug(ref visualTime);
		}

		private void CheckInput()
		{
			if (Input.GetKeyDown(KeyCode.Alpha5))
			{
				ToggleGroup((DisplaySnapshot u) => ref u.Boxes);
			}
			if (Input.GetKeyDown(KeyCode.Alpha6))
			{
				ToggleGroup((DisplaySnapshot u) => ref u.Extrapolate);
			}
			if (Input.GetKeyDown(KeyCode.Alpha7))
			{
				ToggleGroup((DisplaySnapshot u) => ref u.InterpolationMarker);
			}
			if (Input.GetKeyDown(KeyCode.Alpha8))
			{
				ToggleGroup((DisplaySnapshot u) => ref u.LinePath);
			}
			void ToggleGroup(GetField<bool> getField)
			{
				UpdateTempList();
				if (tempList.Count == 0)
				{
					return;
				}
				bool? flag = getField(tempList[0].displaySnapshot);
				for (int i = 1; i < tempList.Count; i++)
				{
					if (getField(tempList[i].displaySnapshot) != flag.Value)
					{
						flag = null;
						break;
					}
				}
				bool flag2 = !flag.HasValue || !flag.Value;
				foreach (DebugUnit temp in tempList)
				{
					getField(temp.displaySnapshot) = flag2;
					if (flag2 && !temp.NetTransform.debugActive)
					{
						temp.NetTransform.debugActive = true;
					}
				}
			}
			void UpdateTempList()
			{
				tempList.Clear();
				if (Input.GetKey(KeyCode.LeftShift))
				{
					BattlefieldGrid.GetUnitsInRangeNonAlloc(SceneSingleton<CameraStateManager>.i.transform.GlobalPosition(), 500f, tmpUnits);
					{
						foreach (Unit tmpUnit in tmpUnits)
						{
							if (!tmpUnit.HasAuthority && tmpUnit.TryGetComponent<NetworkTransformBase>(out var component))
							{
								DebugUnit item = FindOrCreate(component);
								tempList.Add(item);
							}
						}
						return;
					}
				}
				if (followingUnit != null)
				{
					tempList.Add(followingUnit);
				}
			}
		}

		private void UpdateDebug(ref VisualUpdateTime visualTime)
		{
			for (int num = drawList.Count - 1; num >= 0; num--)
			{
				if (drawList[num].NetTransform == null)
				{
					drawList.RemoveAt(num);
				}
			}
			foreach (DebugUnit draw in drawList)
			{
				if (draw.NetTransform is ShipNetworkTransform)
				{
					break;
				}
				if (draw.Active)
				{
					float num2 = draw.NetTransform.SyncInterval * 2.5f;
					double snapshotTime = visualTime.interpolationTime - (double)num2;
					double extrapolationTime = visualTime.interpolationTime + visualTime.extrapolationOffset * (double)draw.NetTransform.extrapolationFactor;
					draw.displaySnapshot.VisualUpdate(snapshotTime, extrapolationTime, visualTime.maxExtrapolateAge);
				}
			}
		}

		private void OnGUI()
		{
			if (!DebugVis.Enabled)
			{
				drawList.Clear();
				base.enabled = false;
			}
			else
			{
				if (drawList.Count == 0 || drawList.All((DebugUnit x) => !x.Active))
				{
					return;
				}
				DisplaySnapshot displaySnapshot = followingUnit?.displaySnapshot;
				GUILayout.Space(30f);
				GUILayout.Label($"(5) Boxes: {displaySnapshot?.Boxes ?? false}");
				GUILayout.Label($"(6) Extrapolation: {displaySnapshot?.Extrapolate ?? false}");
				GUILayout.Label($"(7) Interpolation Marker: {displaySnapshot?.InterpolationMarker ?? false}");
				GUILayout.Label($"(8) Extrapolate Line: {displaySnapshot?.LinePath ?? false}");
				GUILayout.Label("(shift) Enable All nearby");
				Gui? gui = followingUnit?.NetTransform.debugGui;
				if (followingUnit != null)
				{
					GUILayout.Space(30f);
					GUILayout.Label("Type: " + gui?.snapType);
					GUILayout.Label("Pos: " + ToBars(gui?.influence_position ?? 0f));
					GUILayout.Label("Vel: " + ToBars(gui?.influence_velocity ?? 0f));
					GUILayout.Label("Acc: " + ToBars(gui?.influence_acceleration ?? 0f));
				}
				GUILayout.Space(30f);
				foreach (DebugUnit draw in drawList)
				{
					if (draw.NetTransform == null)
					{
						continue;
					}
					bool flag = GUILayout.Toggle(draw.Active, draw.NetTransform.name);
					if (flag == draw.Active)
					{
						continue;
					}
					if (flag)
					{
						draw.NetTransform.debugActive = true;
						if (followingUnit != null)
						{
							draw.displaySnapshot.Boxes = followingUnit.displaySnapshot.Boxes;
							draw.displaySnapshot.Extrapolate = followingUnit.displaySnapshot.Extrapolate;
							draw.displaySnapshot.InterpolationMarker = followingUnit.displaySnapshot.InterpolationMarker;
							draw.displaySnapshot.LinePath = followingUnit.displaySnapshot.LinePath;
						}
					}
					else
					{
						draw.NetTransform.debugActive = false;
						draw.displaySnapshot.Boxes = false;
						draw.displaySnapshot.Extrapolate = false;
						draw.displaySnapshot.InterpolationMarker = false;
						draw.displaySnapshot.LinePath = false;
					}
				}
			}
			static string ToBars(float percent)
			{
				int num = (int)(Mathf.Clamp01(percent) * 20f);
				return "[" + new string('#', num) + new string('_', 20 - num) + "]";
			}
		}
	}
}
