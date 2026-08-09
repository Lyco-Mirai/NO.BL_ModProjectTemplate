using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreatList : MonoBehaviour
{
	[Serializable]
	private class MissileAlarm
	{
		public string seekerType;

		public AudioClip[] alarmClips;

		public AudioClip alertClip;

		private AudioSource alarmSource;

		private List<Missile> missiles;

		public void Setup(Aircraft aircraft)
		{
			missiles = new List<Missile>();
			alarmSource = aircraft.gameObject.AddComponent<AudioSource>();
			alarmSource.playOnAwake = false;
			alarmSource.loop = true;
			alarmSource.clip = alarmClips[0];
			alarmSource.volume = 0.8f;
			alarmSource.spatialBlend = 0f;
			alarmSource.dopplerLevel = 0f;
			alarmSource.pitch = 1f;
			alarmSource.outputAudioMixerGroup = SoundManager.i.MissileAlertMixer;
		}

		public void ManageAlarmSound()
		{
			if (alarmClips.Length < 2 || missiles.Count == 0)
			{
				return;
			}
			int num = alarmClips.Length - 1;
			foreach (Missile missile in missiles)
			{
				num = Mathf.Min(num, (int)(missile.seekerMode - 1));
			}
			AudioClip audioClip = alarmClips[num];
			if (alarmSource != null && audioClip != alarmSource.clip)
			{
				alarmSource.clip = audioClip;
				SyncTime();
				alarmSource.Play();
			}
		}

		public void AddMissile(Missile missile)
		{
			missiles.Add(missile);
			ManageAlarmSound();
			alarmSource.Play();
			SoundManager.PlayInterfaceOneShot(alertClip);
		}

		public void ClearMissiles()
		{
			missiles.Clear();
		}

		public void SyncTime()
		{
			if (alarmSource != null)
			{
				alarmSource.time = 0f;
			}
		}

		public void RemoveMissile(Missile missile)
		{
			int count = missiles.Count;
			missiles.Remove(missile);
			if (missiles.Count != count && missiles.Count == 0)
			{
				alarmSource.Stop();
			}
		}

		public void Remove()
		{
			UnityEngine.Object.Destroy(alarmSource);
		}
	}

	[SerializeField]
	private GameObject threatItemPrefab;

	[SerializeField]
	private MissileAlarm[] alarmTypes;

	private MissileWarning missileWarning;

	private Dictionary<PersistentID, ThreatItem> itemLookup = new Dictionary<PersistentID, ThreatItem>();

	private Dictionary<string, MissileAlarm> alarmLookup = new Dictionary<string, MissileAlarm>();

	public void SetAircraft(Aircraft aircraft)
	{
		alarmLookup.Clear();
		MissileAlarm[] array = alarmTypes;
		foreach (MissileAlarm missileAlarm in array)
		{
			alarmLookup.Add(missileAlarm.seekerType, missileAlarm);
			missileAlarm.Setup(aircraft);
			missileAlarm.ClearMissiles();
		}
		foreach (KeyValuePair<PersistentID, ThreatItem> item in itemLookup)
		{
			UnityEngine.Object.Destroy(item.Value.gameObject);
		}
		itemLookup.Clear();
		missileWarning = aircraft.GetMissileWarningSystem();
		missileWarning.onMissileWarning += ThreatList_OnMissileWarning;
		missileWarning.offMissileWarning += ThreatList_OffMissileWarning;
		aircraft.onDisableUnit += ThreatList_OnAircraftDisable;
	}

	private void ThreatList_OnMissileWarning(MissileWarning.OnMissileWarning e)
	{
		foreach (KeyValuePair<string, MissileAlarm> item in alarmLookup)
		{
			item.Value.SyncTime();
		}
		if (!itemLookup.ContainsKey(e.missile.persistentID))
		{
			ThreatItem component = UnityEngine.Object.Instantiate(threatItemPrefab, base.transform).GetComponent<ThreatItem>();
			component.SetItem(e.missile.persistentID);
			string seekerType = e.missile.GetSeekerType();
			if (!alarmLookup.TryGetValue(seekerType, out var value))
			{
				return;
			}
			value.AddMissile(e.missile);
			itemLookup.Add(e.missile.persistentID, component);
		}
		base.enabled = true;
		SceneSingleton<CombatHUD>.i.FlashMarker(e.missile, flash: true);
		SceneSingleton<DynamicMap>.i.FlagIncomingMissile(e.missile);
	}

	private void ThreatList_OffMissileWarning(MissileWarning.OffMissileWarning e)
	{
		SceneSingleton<CombatHUD>.i.FlashMarker(e.missile, flash: false);
		SceneSingleton<DynamicMap>.i.ClearIncomingMissile(e.missile);
		if (itemLookup.TryGetValue(e.missile.persistentID, out var value))
		{
			UnityEngine.Object.Destroy(value.gameObject);
			string seekerType = e.missile.GetSeekerType();
			alarmLookup[seekerType].RemoveMissile(e.missile);
			itemLookup.Remove(e.missile.persistentID);
		}
	}

	private void ThreatList_OnAircraftDisable(Unit unit)
	{
		MissileAlarm[] array = alarmTypes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Remove();
		}
		missileWarning.onMissileWarning -= ThreatList_OnMissileWarning;
		missileWarning.offMissileWarning -= ThreatList_OffMissileWarning;
	}

	private void Update()
	{
		if (itemLookup.Count == 0)
		{
			base.enabled = false;
		}
		foreach (KeyValuePair<PersistentID, ThreatItem> item in itemLookup)
		{
			item.Value.AnimateItem();
		}
		foreach (KeyValuePair<string, MissileAlarm> item2 in alarmLookup)
		{
			item2.Value.ManageAlarmSound();
		}
	}
}
