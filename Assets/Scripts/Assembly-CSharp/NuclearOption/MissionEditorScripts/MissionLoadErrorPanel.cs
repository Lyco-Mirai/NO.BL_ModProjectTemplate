using System.Collections.Generic;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionLoadErrorPanel : MonoBehaviour
	{
		[Header("Main panel")]
		[SerializeField]
		private GameObject mainHolder;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button toggleListButton;

		[SerializeField]
		private TextMeshProUGUI errorCount;

		[SerializeField]
		private TextMeshProUGUI warnCount;

		[Header("List panel")]
		[SerializeField]
		private GameObject listHolder;

		[SerializeField]
		private Transform listParent;

		[SerializeField]
		private MissionLoadErrorItem prefab;

		private readonly List<MissionLoadErrorItem> items = new List<MissionLoadErrorItem>();

		private bool listExpanded;

		private void Awake()
		{
			closeButton.onClick.AddListener(Close);
			toggleListButton.onClick.AddListener(ToggleList);
			listHolder.SetActive(value: false);
		}

		private void ToggleList()
		{
			listExpanded = !listExpanded;
			listHolder.SetActive(listExpanded);
		}

		private void Close()
		{
			mainHolder.SetActive(value: false);
		}

		public void SetErrors(LoadErrors loadError)
		{
			int count = loadError.Warnings.Count;
			int errorAndExceptionsCount = loadError.ErrorAndExceptionsCount;
			if (count + errorAndExceptionsCount == 0)
			{
				mainHolder.SetActive(value: false);
				return;
			}
			mainHolder.SetActive(value: true);
			listHolder.SetActive(value: false);
			warnCount.text = count.ToString();
			errorCount.text = errorAndExceptionsCount.ToString();
			foreach (MissionLoadErrorItem item in items)
			{
				Object.Destroy(item.gameObject);
			}
			items.Clear();
			foreach (string warning in loadError.Warnings)
			{
				MissionLoadErrorItem missionLoadErrorItem = Object.Instantiate(prefab, listParent);
				missionLoadErrorItem.SetWarning(warning);
				items.Add(missionLoadErrorItem);
			}
			foreach (string error in loadError.Errors)
			{
				MissionLoadErrorItem missionLoadErrorItem2 = Object.Instantiate(prefab, listParent);
				missionLoadErrorItem2.SetError(error);
				items.Add(missionLoadErrorItem2);
			}
			foreach (ExceptionEntry exception in loadError.Exceptions)
			{
				MissionLoadErrorItem missionLoadErrorItem3 = Object.Instantiate(prefab, listParent);
				missionLoadErrorItem3.SetException(exception);
				items.Add(missionLoadErrorItem3);
			}
		}
	}
}
