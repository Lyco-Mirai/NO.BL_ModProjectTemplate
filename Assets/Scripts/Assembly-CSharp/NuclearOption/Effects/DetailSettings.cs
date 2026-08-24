using UnityEngine;

namespace NuclearOption.Effects
{
	public class DetailSettings
	{
		public static readonly float TreeRangeMultiplierMin = 0.5f;

		public static readonly float TreeRangeMultiplierMax = 2f;

		private bool _grassEnabled = true;

		private float _treeRangeMultiplier = 1f;

		public bool GrassEnabled
		{
			get
			{
				return _grassEnabled;
			}
			set
			{
				_grassEnabled = value;
				PlayerPrefs.SetInt("GrassEnabled", _grassEnabled ? 1 : 0);
			}
		}

		public float TreeRangeMultiplier
		{
			get
			{
				return _treeRangeMultiplier;
			}
			set
			{
				value = Mathf.Clamp(value, TreeRangeMultiplierMin, TreeRangeMultiplierMax);
				_treeRangeMultiplier = value;
				PlayerPrefs.SetFloat("TreeRangeMultiplier", _treeRangeMultiplier);
			}
		}

		private void SetDefaults()
		{
			_grassEnabled = true;
			_treeRangeMultiplier = 1f;
		}

		public void Load()
		{
			GrassEnabled = PlayerPrefs.GetInt("GrassEnabled", 1) == 1;
			float value = PlayerPrefs.GetFloat("TreeRangeMultiplier", 1f);
			TreeRangeMultiplier = Mathf.Clamp(value, TreeRangeMultiplierMin, TreeRangeMultiplierMax);
		}

		public void Clear()
		{
			PlayerPrefs.DeleteKey("GrassEnabled");
			PlayerPrefs.DeleteKey("TreeRangeMultiplier");
			PlayerPrefs.Save();
			SetDefaults();
		}
	}
}
