using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts;
using UnityEngine;

namespace NuclearOption.UI
{
	public class LeaderboardFactionList : MonoBehaviour
	{
		[SerializeField]
		private LeaderboardFactionItem[] factionDisplays;

		[SerializeField]
		private RectTransform rootTransform;

		[SerializeField]
		private int rowHeightPerPlayer = 25;

		[SerializeField]
		private int rowHeightMax = 450;

		private bool hasHQs;

		private bool addedPlayerChangedFaction;

		public IReadOnlyList<LeaderboardFactionItem> Displays => factionDisplays;

		private void Awake()
		{
			FactionHQ[] sortedHQs = FactionRegistry.GetSortedHQs();
			if (sortedHQs.Length == 2)
			{
				SetupHQs(sortedHQs);
			}
			else
			{
				WaitForHQs().Forget();
			}
		}

		private async UniTaskVoid WaitForHQs()
		{
			LeaderboardFactionItem[] array = factionDisplays;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetupNoHQ();
			}
			CancellationToken cancel = base.destroyCancellationToken;
			await UniTask.Yield();
			while (!cancel.IsCancellationRequested)
			{
				FactionHQ[] sortedHQs = FactionRegistry.GetSortedHQs();
				if (sortedHQs.Length == 2)
				{
					SetupHQs(sortedHQs);
					break;
				}
				await UniTask.Yield();
			}
		}

		private void SetupHQs(FactionHQ[] allHQs)
		{
			hasHQs = true;
			for (int i = 0; i < 2; i++)
			{
				factionDisplays[i].SetupHq(allHQs[i]);
			}
			AddPlayerFactionEvent();
		}

		private void AddPlayerFactionEvent()
		{
			if (!addedPlayerChangedFaction)
			{
				addedPlayerChangedFaction = true;
				LeaderboardFactionItem[] array = factionDisplays;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].HQ.onPlayerChangedFaction += OnPlayersChanged;
				}
				OnPlayersChanged();
			}
		}

		private void RemovePlayerFactionEvent()
		{
			if (addedPlayerChangedFaction)
			{
				addedPlayerChangedFaction = false;
				LeaderboardFactionItem[] array = factionDisplays;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].HQ.onPlayerChangedFaction -= OnPlayersChanged;
				}
			}
		}

		private void OnEnable()
		{
			if (hasHQs)
			{
				AddPlayerFactionEvent();
			}
		}

		private void OnDisable()
		{
			RemovePlayerFactionEvent();
		}

		private void OnPlayersChanged()
		{
			int num = 0;
			LeaderboardFactionItem[] array = factionDisplays;
			for (int i = 0; i < array.Length; i++)
			{
				int val = array[i].DisplayPlayers();
				num = Math.Max(num, val);
			}
			int num2 = Mathf.Min(rowHeightPerPlayer * num, rowHeightMax);
			array = factionDisplays;
			foreach (LeaderboardFactionItem obj in array)
			{
				Vector2 sizeDelta = obj.ScrollView.sizeDelta;
				sizeDelta.y = num2;
				obj.ScrollView.sizeDelta = sizeDelta;
			}
			FixLayout.ForceRebuildAtEndOfFrame(rootTransform);
		}
	}
}
