using System;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionAutoSaveSettings
	{
		private const float NO_SAVE_INTERVAL_PERCENT = 0.2f;

		private const string KEY_INTERVAL = "AutoSave_Interval";

		private const string KEY_MAX_SAVES = "AutoSave_MaxSaves";

		private const string KEY_RETENTION = "AutoSave_RetentionDays";

		private static MissionAutoSaveSettings instance;

		public float IntervalMinutes = 5f;

		public int MaxAutoSaves = 5;

		public int RetentionDays = 60;

		[NonSerialized]
		public float NextAutoSaveTime;

		public bool CheckTimed()
		{
			return Time.unscaledTime > NextAutoSaveTime;
		}

		public void SetNextSavedTime(bool didSave)
		{
			float num = (didSave ? 1f : 0.2f) * IntervalMinutes;
			NextAutoSaveTime = Time.unscaledTime + num * 60f;
		}

		public static MissionAutoSaveSettings GetOrLoad()
		{
			if (instance == null)
			{
				MissionAutoSaveSettings missionAutoSaveSettings = new MissionAutoSaveSettings();
				missionAutoSaveSettings.IntervalMinutes = PlayerPrefs.GetFloat("AutoSave_Interval", 5f);
				missionAutoSaveSettings.MaxAutoSaves = PlayerPrefs.GetInt("AutoSave_MaxSaves", 5);
				missionAutoSaveSettings.RetentionDays = PlayerPrefs.GetInt("AutoSave_RetentionDays", 60);
				missionAutoSaveSettings.Validate();
				instance = missionAutoSaveSettings;
			}
			return instance;
		}

		public void Validate()
		{
			IntervalMinutes = Mathf.Max(0.5f, IntervalMinutes);
			MaxAutoSaves = Mathf.Clamp(MaxAutoSaves, 1, 100);
			RetentionDays = Mathf.Clamp(RetentionDays, 1, 365);
		}

		public void Save()
		{
			Validate();
			PlayerPrefs.SetFloat("AutoSave_Interval", IntervalMinutes);
			PlayerPrefs.SetInt("AutoSave_MaxSaves", MaxAutoSaves);
			PlayerPrefs.SetInt("AutoSave_RetentionDays", RetentionDays);
			PlayerPrefs.Save();
		}
	}
}
