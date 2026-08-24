using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace JamesFrowen.ScriptableVariables.UI
{
	public abstract class ListController<T> : MonoBehaviour
	{
		private static readonly ProfilerMarker updateListMarker = new ProfilerMarker("ListController UpdateList");

		private static readonly ProfilerMarker updateListValuesMarker = new ProfilerMarker("ListController UpdateList Values");

		private static readonly ProfilerMarker populateRowsMarker = new ProfilerMarker("ListController PopulateRows");

		private static readonly ProfilerMarker populateRowsResultMarker = new ProfilerMarker("ListController PopulateRows Result");

		[SerializeField]
		protected RectTransform _parent;

		[SerializeField]
		private int _maxRows = 100;

		[SerializeField]
		private GameObject _prefab;

		[SerializeField]
		private bool _findExisting;

		[SerializeField]
		private bool _includeInactive;

		[SerializeField]
		private bool _updateInViewOnly;

		[SerializeField]
		private ScrollRect _scrollView;

		private bool _hasInit;

		private readonly List<ListItem<T>> _rows = new List<ListItem<T>>();

		private readonly List<T> _values = new List<T>();

		private bool taskIsRunning;

		private int viewIndexTop;

		private int viewIndexBottom;

		private int ValueCountClamped => Math.Min(_values.Count, _maxRows);

		protected virtual void Awake()
		{
			CheckInit(setEmpty: true);
			if (_updateInViewOnly)
			{
				_scrollView.onValueChanged.AddListener(ScrollViewChanged);
			}
		}

		private void ScrollViewChanged(Vector2 scroll)
		{
			if (!taskIsRunning && _rows.Count > 0 && _values.Count > 0)
			{
				float height = _scrollView.viewport.rect.height;
				float num = 1f - scroll.y;
				RectTransform parent = _parent;
				float num2 = 0f;
				float num3 = 0f;
				if (parent.TryGetComponent<VerticalLayoutGroup>(out var component))
				{
					num2 = component.padding.top + component.padding.bottom;
					num3 = component.spacing;
				}
				float num4 = parent.sizeDelta.y - num2;
				float num5 = _rows.First().CalculateHeight(_values.First()) + num3;
				float num6 = (num4 - height) * num;
				float num7 = num6 + height;
				int num8 = Mathf.FloorToInt(num6 / num5);
				int num9 = Mathf.CeilToInt(num7 / num5) + 1;
				Debug.Log($"view:{height} content:{num4} {num}%, {num6} -> {num7}, {num8} -> {num9}");
				num8 = Mathf.Clamp(num8, 0, ValueCountClamped - 1);
				num9 = Mathf.Clamp(num9, 0, ValueCountClamped - 1);
				int num10 = Mathf.Min(num8, viewIndexTop);
				int num11 = Mathf.Max(num9, viewIndexBottom);
				for (int i = num10; i <= num11; i++)
				{
					bool visible = num8 <= i && i <= num9;
					ViewChanged(i, visible);
				}
				viewIndexTop = num8;
				viewIndexBottom = num9;
			}
		}

		private void ViewChanged(int index, bool visible)
		{
			ListItem<T> listItem = _rows[index];
			if (listItem.IsActive != visible)
			{
				Debug.Log($"{index}: {visible}");
				listItem.SetActive(visible);
				if (visible)
				{
					listItem.SetNewValue(_values[index]);
				}
			}
		}

		private void CheckInit(bool setEmpty)
		{
			if (_hasInit)
			{
				return;
			}
			_hasInit = true;
			if (_findExisting)
			{
				_rows.AddRange(GetComponentsInChildren<ListItem<T>>(_includeInactive));
				if (_updateInViewOnly)
				{
					foreach (ListItem<T> row in _rows)
					{
						row.SetActive(active: false);
					}
				}
			}
			if (setEmpty)
			{
				UpdateList(Array.Empty<T>());
			}
		}

		protected virtual void OnValidate()
		{
		}

		public void UpdateList(IEnumerable<T> values)
		{
			using (updateListMarker.Auto())
			{
				_values.Clear();
				if (values != null)
				{
					_values.AddRange(values);
				}
				if (!taskIsRunning)
				{
					UpdateListInner().Forget();
				}
			}
		}

		public void UpdateList(IReadOnlyList<T> values)
		{
			using (updateListMarker.Auto())
			{
				_values.Clear();
				if (values != null)
				{
					_values.AddRange(values);
				}
				if (!taskIsRunning)
				{
					UpdateListInner().Forget();
				}
			}
		}

		public async UniTask UpdateListInner()
		{
			taskIsRunning = true;
			CancellationToken cancel = base.destroyCancellationToken;
			try
			{
				CheckInit(setEmpty: false);
				while (true)
				{
					int num = Math.Min(_maxRows, _values.Count) - _rows.Count;
					if (num <= 0)
					{
						break;
					}
					await CreateNewRows(num);
					if (cancel.IsCancellationRequested)
					{
						return;
					}
					if (_updateInViewOnly)
					{
						RebuildLayout();
						await UniTask.Yield();
						if (cancel.IsCancellationRequested)
						{
							return;
						}
					}
				}
			}
			finally
			{
				taskIsRunning = false;
			}
			using (updateListValuesMarker.Auto())
			{
				if (_updateInViewOnly)
				{
					ScrollViewChanged(_scrollView.normalizedPosition);
				}
				else
				{
					UpdateRows();
				}
			}
		}

		private void UpdateRows()
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				ListItem<T> listItem = _rows[i];
				if (i < ValueCountClamped)
				{
					listItem.SetActive(active: true);
					listItem.SetNewValue(_values[i]);
				}
				else
				{
					listItem.SetActive(active: false);
				}
			}
			RebuildLayout();
		}

		public void Redraw()
		{
			int valueCountClamped = ValueCountClamped;
			for (int i = 0; i < valueCountClamped; i++)
			{
				_rows[i].Redraw();
			}
		}

		private async UniTask CreateNewRows(int createCount)
		{
			AsyncInstantiateOperation<GameObject> op;
			using (populateRowsMarker.Auto())
			{
				op = UnityEngine.Object.InstantiateAsync(_prefab, createCount, _parent);
			}
			CancellationToken cancel = base.destroyCancellationToken;
			await op.ToUniTask();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			using (populateRowsResultMarker.Auto())
			{
				for (int i = 0; i < createCount; i++)
				{
					ListItem<T> component = op.Result[i].GetComponent<ListItem<T>>();
					_rows.Add(component);
					if (_updateInViewOnly)
					{
						component.SetActive(active: false);
					}
				}
			}
		}

		protected virtual void RebuildLayout()
		{
		}
	}
}
