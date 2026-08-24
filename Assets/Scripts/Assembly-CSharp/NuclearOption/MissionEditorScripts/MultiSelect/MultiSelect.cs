using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mirage;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts.MultiSelect
{
	public class MultiSelect<TObject>
	{
		public delegate bool EqualityChecker<T>(T first, T other);

		public delegate T GetField<T>(TObject obj);

		public delegate ref T GetFieldRef<T>(TObject obj);

		public delegate TTo ModifyValue<TFrom, TTo>(TFrom from);

		public delegate string GetLabelString<T>(T value);

		private readonly List<TObject> _targets = new List<TObject>();

		public List<(object key, Action callback)> Changed = new List<(object, Action)>();

		public IReadOnlyList<TObject> Targets => _targets;

		public void AddAndInvokeChanged(object key, Action action)
		{
			action();
			Changed.Add((key, action));
		}

		public void RemoveChanged(object key)
		{
			for (int num = Changed.Count - 1; num >= 0; num--)
			{
				if (Changed[num].key == key)
				{
					Changed.RemoveAt(num);
				}
			}
		}

		private void TargetsChangedInvoke()
		{
			for (int i = 0; i < Changed.Count; i++)
			{
				Changed[i].callback();
			}
		}

		public void ReplaceTargets(IReadOnlyList<TObject> targets)
		{
			if (targets != _targets)
			{
				_targets.Clear();
				_targets.AddRange(targets);
			}
			TargetsChangedInvoke();
		}

		public void ReplaceTargets(TObject target)
		{
			_targets.Clear();
			_targets.Add(target);
			TargetsChangedInvoke();
		}

		public void AddTarget(TObject target)
		{
			_targets.Add(target);
			TargetsChangedInvoke();
		}

		public void AddTargets(List<TObject> targets)
		{
			_targets.AddRange(targets);
			TargetsChangedInvoke();
		}

		public void RemoveTarget(TObject target)
		{
			_targets.Remove(target);
			TargetsChangedInvoke();
		}

		public void RemoveTargets(List<TObject> targets)
		{
			foreach (TObject target in targets)
			{
				_targets.Remove(target);
			}
			TargetsChangedInvoke();
		}

		public Action<T> SetSameValueAction<T>(GetFieldRef<T> getField)
		{
			return delegate(T value)
			{
				SetSameValue(getField, value);
			};
		}

		public UnityAction<T> SetSameValueUnityAction<T>(GetFieldRef<T> getField)
		{
			return delegate(T value)
			{
				SetSameValue(getField, value);
			};
		}

		public void SetSameValue<T>(GetFieldRef<T> getField, T value)
		{
			foreach (TObject target in _targets)
			{
				getField(target) = value;
			}
		}

		public bool TryGetSameValue<T>(GetField<T> getField, out T sameValue)
		{
			return TryGetSameValue(_targets, getField, out sameValue);
		}

		public static bool TryGetSameValue<T>(IEnumerable<TObject> targetsEnumerable, GetField<T> getField, out T sameValue)
		{
			using AutoPool<List<TObject>>.Wrapper wrapper = AutoPool<List<TObject>>.Take();
			List<TObject> item = wrapper.Item;
			item.Clear();
			item.AddRange(targetsEnumerable);
			return TryGetSameValue(item, getField, out sameValue);
		}

		public static bool TryGetSameValue<T>(IReadOnlyList<TObject> targets, GetField<T> getField, out T sameValue)
		{
			sameValue = default(T);
			if (targets.Count == 0)
			{
				return false;
			}
			T val = getField(targets[0]);
			for (int i = 1; i < targets.Count; i++)
			{
				T y = getField(targets[i]);
				if (!EqualityComparer<T>.Default.Equals(val, y))
				{
					return false;
				}
			}
			sameValue = val;
			return true;
		}

		public T GetSameValueOrDefault<T>(GetField<T> getField, T defaultValue)
		{
			if (!TryGetSameValue(getField, out var sameValue))
			{
				return defaultValue;
			}
			return sameValue;
		}

		public string GetSameLabel<T>(GetField<T> getField, GetLabelString<T> getLabel)
		{
			if (!TryGetSameValue(getField, out var sameValue))
			{
				return "-";
			}
			return getLabel(sameValue);
		}

		public string GetSameLabel(GetField<string> getField)
		{
			if (!TryGetSameValue(getField, out var sameValue))
			{
				return "-";
			}
			return sameValue;
		}

		public void SetupSlider(Slider slider, TextMeshProUGUI text, GetFieldRef<float> getField, GetLabelString<float> getLabel)
		{
			SetupSlider(slider, text, getField, (float v) => v, (float v) => v, getLabel);
		}

		public void SetupSlider<T>(Slider slider, TextMeshProUGUI text, GetFieldRef<T> getField, ModifyValue<T, float> fieldToSlider, ModifyValue<float, T> sliderToField, GetLabelString<T> getLabel)
		{
			slider.onValueChanged.AddListener(delegate(float v)
			{
				T value = sliderToField(v);
				SetSameValue(getField, value);
				if (text != null)
				{
					string text2 = getLabel(value);
					text.text = text2;
				}
			});
			AddAndInvokeChanged(slider, delegate
			{
				T sameValue;
				float valueWithoutNotify = (TryGetSameValue(getField.Cast(), out sameValue) ? fieldToSlider(sameValue) : 0f);
				slider.SetValueWithoutNotify(valueWithoutNotify);
				if (text != null)
				{
					string sameLabel = GetSameLabel(getField.Cast(), getLabel);
					text.text = sameLabel;
				}
			});
		}

		public void SetupDropdown(TMP_Dropdown dropdown, List<string> dropdownOptions, GetFieldRef<string> getField)
		{
			dropdown.options.Clear();
			dropdown.AddOptions(dropdownOptions);
			dropdown.onValueChanged.AddListener(delegate(int index)
			{
				string value = dropdownOptions[index];
				SetSameValue(getField, value);
			});
			AddAndInvokeChanged(dropdown, delegate
			{
				string sameValue;
				int valueWithoutNotify = (TryGetSameValue(getField.Cast(), out sameValue) ? dropdownOptions.IndexOf(sameValue) : (-1));
				dropdown.SetValueWithoutNotify(valueWithoutNotify);
			});
		}

		public void SetupDropdown<T>(TMP_Dropdown dropdown, GetFieldRef<T> getField, ModifyValue<T, int> fieldToDropdown, ModifyValue<int, T> dropdownToField)
		{
			dropdown.onValueChanged.AddListener(delegate(int v)
			{
				T value = dropdownToField(v);
				SetSameValue(getField, value);
			});
			AddAndInvokeChanged(dropdown, delegate
			{
				T sameValue;
				int valueWithoutNotify = (TryGetSameValue(getField.Cast(), out sameValue) ? fieldToDropdown(sameValue) : (-1));
				dropdown.SetValueWithoutNotify(valueWithoutNotify);
			});
		}

		public void SetupInputField(TMP_InputField inputField, GetFieldRef<string> getField)
		{
			SetupInputField(inputField, getField, (string t) => t, (string t) => t);
		}

		public void SetupInputField<T>(TMP_InputField inputField, GetFieldRef<T> getField, ModifyValue<T, string> fieldToInputField, ModifyValue<string, T> inputFieldToField)
		{
			inputField.onEndEdit.AddListener(delegate(string v)
			{
				T value = inputFieldToField(v);
				SetSameValue(getField, value);
			});
			AddAndInvokeChanged(inputField, delegate
			{
				T sameValue;
				string textWithoutNotify = (TryGetSameValue(getField.Cast(), out sameValue) ? fieldToInputField(sameValue) : "-");
				inputField.SetTextWithoutNotify(textWithoutNotify);
			});
		}

		public void SetupToggle(Toggle toggle, GameObject toggleDifferentValue, GetFieldRef<bool> getField)
		{
			toggle.onValueChanged.AddListener(delegate(bool value)
			{
				SetSameValue(getField, value);
			});
			AddAndInvokeChanged(toggle, delegate
			{
				bool sameValue;
				bool flag = TryGetSameValue(getField.Cast(), out sameValue);
				bool isOnWithoutNotify = flag && sameValue;
				if (toggleDifferentValue != null)
				{
					toggleDifferentValue.SetActive(!flag);
				}
				toggle.SetIsOnWithoutNotify(isOnWithoutNotify);
			});
		}

		public bool Any(GetField<bool> getField)
		{
			foreach (TObject target in _targets)
			{
				if (getField(target))
				{
					return true;
				}
			}
			return false;
		}

		public bool All(GetField<bool> getField)
		{
			foreach (TObject target in _targets)
			{
				if (!getField(target))
				{
					return false;
				}
			}
			return true;
		}

		public bool AllTheSame(EqualityChecker<TObject> areEqual)
		{
			return AllTheSame((TObject x) => x, areEqual);
		}

		public bool AllTheSame<T>(GetField<T> getField) where T : IEquatable<T>
		{
			return AllTheSame(getField, (T first, T other) => EqualityComparer<T>.Default.Equals(first, other));
		}

		public bool AllTheSame<T>(GetField<IReadOnlyList<T>> getField) where T : IEquatable<T>
		{
			return AllTheSame(getField, (T first, T other) => EqualityComparer<T>.Default.Equals(first, other));
		}

		public bool AllTheSame<T>(GetField<IReadOnlyList<T>> getField, EqualityChecker<T> areEqual)
		{
			return AllTheSame(getField, delegate(IReadOnlyList<T> firstList, IReadOnlyList<T> otherList)
			{
				if (firstList == null && otherList == null)
				{
					return true;
				}
				if (firstList?.Count != otherList?.Count)
				{
					return false;
				}
				for (int i = 0; i < firstList.Count; i++)
				{
					if (!areEqual(firstList[i], otherList[i]))
					{
						return false;
					}
				}
				return true;
			});
		}

		[Conditional("UNITY_ASSERTIONS")]
		private static void AssertNotReferenceEquals<T>(T a, T b)
		{
			if (!typeof(T).IsValueType && (object)a == (object)b)
			{
				UnityEngine.Debug.LogError("2 lists had the same reference when using classes. Each target should have its own objects");
			}
		}

		public bool AllTheSame<T>(GetField<T> getField, EqualityChecker<T> checker)
		{
			if (Targets.Count == 0)
			{
				return false;
			}
			if (Targets.Count == 1)
			{
				return true;
			}
			T first = getField(Targets[0]);
			for (int i = 1; i < Targets.Count; i++)
			{
				T other = getField(Targets[i]);
				if (!checker(first, other))
				{
					return false;
				}
			}
			return true;
		}
	}
}
