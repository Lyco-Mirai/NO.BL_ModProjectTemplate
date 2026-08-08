using System;
using System.Collections.Generic;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using Unity.Profiling;
using UnityEngine;

public class SinglePlayerMenu : MonoBehaviour
{
	private static readonly ProfilerMarker awakeMarker = new ProfilerMarker("SinglePlayerMenu.Awake");

	private static readonly ProfilerMarker startMissionMarker = new ProfilerMarker("SinglePlayerMenu.StartMission");

	[SerializeField]
	private MissionsPicker picker;

	private void Awake()
	{
		using (awakeMarker.Auto())
		{
			picker.OnMissionConfirmed += StartMission;
			picker.SetPickerFilter(new MissionsPicker.Filter
			{
				RequiredTags = new List<MissionTag> { MissionTag.SinglePlayer }
			});
			picker.ShowPicker();
		}
	}

	private void StartMission(Mission mission)
	{
		using (startMissionMarker.Auto())
		{
			if (mission == null)
			{
				throw new InvalidOperationException("Start Mission should not be called while mission is null");
			}
			MissionManager.SetMission(mission, checkIfSame: false);
			NetworkManagerNuclearOption.i.StartHost(new HostOptions(SocketType.Offline, GameState.SinglePlayer, mission.MapKey));
		}
	}
}
