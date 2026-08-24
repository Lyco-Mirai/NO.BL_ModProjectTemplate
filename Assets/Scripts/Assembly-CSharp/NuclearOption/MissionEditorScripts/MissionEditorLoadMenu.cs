using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorLoadMenu : MonoBehaviour
	{
		[SerializeField]
		private MissionsPicker missionsPicker;

		[SerializeField]
		private GameObject loadingNotification;

		private void Awake()
		{
			missionsPicker.OnMissionConfirmed += OnMissionConfirmed;
			List<MissionGroup> second = new List<MissionGroup>
			{
				MissionGroup.Default,
				MissionGroup.User,
				MissionGroup.BuiltIn
			};
			List<MissionGroup> disallowedGroups = MissionGroup.AllGroups.Except(second).ToList();
			missionsPicker.SetPickerFilter(new MissionsPicker.Filter
			{
				DisallowedGroups = disallowedGroups
			});
		}

		private void OnMissionConfirmed(Mission mission)
		{
			if (loadingNotification != null)
			{
				loadingNotification.SetActive(value: true);
			}
			MissionEditor.LoadEditor(mission).Forget();
		}
	}
}
