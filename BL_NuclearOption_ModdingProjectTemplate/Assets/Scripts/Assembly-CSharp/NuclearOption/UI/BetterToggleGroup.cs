using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	[AddComponentMenu("UI/Better Toggle Group", 532)]
	[RequireComponent(typeof(RectTransform))]
	public class BetterToggleGroup : MonoBehaviour
	{
		public delegate void ToggleIndexOn(int index);

		[Serializable]
		public enum GroupMode
		{
			Single = 0,
			Flags = 1,
			AtleastOneFlag = 2
		}

		[SerializeField]
		private GroupMode mode;

		[SerializeField]
		private int currentValue;

		[SerializeField]
		private List<Toggle> toggles;

		private bool settingValue;

		public bool FlagMode
		{
			get
			{
				if (mode != GroupMode.Flags)
				{
					return mode == GroupMode.AtleastOneFlag;
				}
				return true;
			}
		}

		public event ToggleIndexOn OnChangeValue;

		public int GetIndex()
		{
			return currentValue;
		}

		public int GetFlags()
		{
			return currentValue;
		}

		private void Awake()
		{
			for (int i = 0; i < toggles.Count; i++)
			{
				int index = i;
				toggles[i].onValueChanged.AddListener(delegate(bool on)
				{
					ToggleChanged(on, index);
				});
			}
		}

		private void OnValidate()
		{
			List<Toggle> list = toggles;
			if (list != null && list.Count > 0)
			{
				SetValue(currentValue, notify: false);
			}
		}

		private void Start()
		{
			SetValue(currentValue, notify: false);
		}

		private void ToggleChanged(bool on, int i)
		{
			if (settingValue)
			{
				return;
			}
			if (FlagMode)
			{
				int num = ToggleFlag(currentValue, on, i);
				if (mode == GroupMode.AtleastOneFlag && num == 0)
				{
					toggles[i].SetIsOnWithoutNotify(value: true);
					RejectToggle(i);
				}
				else
				{
					SetValue(num, notify: true);
				}
			}
			else
			{
				SetValue(i, notify: true);
			}
		}

		private void RejectToggle(int i)
		{
			if (toggles[i].TryGetComponent<ButtonErrorFeedback>(out var component))
			{
				component.TriggerError().Forget();
			}
		}

		private static int ToggleFlag(int current, bool on, int i)
		{
			int num = 1 << i;
			if (on)
			{
				return current | num;
			}
			return current & ~num;
		}

		public void SetIndex(int value, bool notify)
		{
			SetValue(value, notify);
		}

		public void SetFlags(int value, bool notify)
		{
			SetValue(value, notify);
		}

		private void SetValue(int value, bool notify)
		{
			if (!FlagMode && value >= toggles.Count)
			{
				throw new IndexOutOfRangeException($"Failed to set ToggleGroup to value {value} because there are only {toggles.Count} toggles");
			}
			if (mode == GroupMode.AtleastOneFlag && value == 0)
			{
				throw new ArgumentException("Atleast one flag must be set");
			}
			settingValue = true;
			currentValue = value;
			try
			{
				for (int i = 0; i < toggles.Count; i++)
				{
					Toggle toggle = toggles[i];
					bool flag;
					if (FlagMode)
					{
						int num = 1 << i;
						flag = (value & num) != 0;
					}
					else
					{
						flag = i == value;
						toggle.interactable = !flag;
					}
					if (notify)
					{
						try
						{
							toggle.isOn = flag;
						}
						catch (Exception arg)
						{
							Debug.LogError($"Error setting inner Toggle {arg}");
						}
					}
					else
					{
						toggle.SetIsOnWithoutNotify(flag);
					}
				}
			}
			finally
			{
				settingValue = false;
			}
			if (notify)
			{
				this.OnChangeValue?.Invoke(value);
			}
		}
	}
}
