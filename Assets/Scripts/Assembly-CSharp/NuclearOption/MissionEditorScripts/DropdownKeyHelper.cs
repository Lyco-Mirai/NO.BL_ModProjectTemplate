using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public readonly struct DropdownKeyHelper<T>
	{
		private readonly TMP_Dropdown dropdown;

		public readonly List<T> keys;

		public DropdownKeyHelper(TMP_Dropdown dropdown, List<T> keys)
		{
			this.dropdown = dropdown;
			this.keys = keys;
		}

		public DropdownKeyHelper(TMP_Dropdown dropdown)
		{
			this.dropdown = dropdown;
			keys = new List<T>();
		}

		public void Clear()
		{
			dropdown.options.Clear();
			keys.Clear();
		}

		public void Add(string label, T key)
		{
			dropdown.options.Add(new TMP_Dropdown.OptionData(label));
			keys.Add(key);
		}

		public int IndexOf(T key)
		{
			return Mathf.Max(keys.IndexOf(key), 0);
		}
	}
}
