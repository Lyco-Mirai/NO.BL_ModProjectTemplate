using System;
using System.Collections.Generic;
using System.Linq;
using JamesFrowen.ScriptableVariables;
using UnityEngine;

namespace NuclearOption.DedicatedServer
{
	public class MissionRotation
	{
		private List<MissionOptions> allMissions;

		private readonly RotationType rotationType;

		private int _nextIndex;

		private List<MissionOptions> randomQueue;

		private MissionOptions NextQueued;

		private MissionOptions? NextOverride;

		public MissionOptions? NextOverrideOption => NextOverride;

		public MissionRotation(MissionOptions[] missionRotation, RotationType rotationType)
		{
			if (missionRotation != null && missionRotation.Length == 0)
			{
				throw new ArgumentNullException("missionRotation", "MissionRotation must have atleast 1 mission");
			}
			allMissions = missionRotation.ToList();
			this.rotationType = rotationType;
			_nextIndex = 0;
			if (rotationType == RotationType.RandomQueue)
			{
				randomQueue = new List<MissionOptions>(missionRotation.Length);
				for (int i = 0; i < missionRotation.Length; i++)
				{
					randomQueue.Add(missionRotation[i]);
				}
				Shuffle(randomQueue, firstShuffle: true);
			}
			NextQueued = GetNextMapInternal();
		}

		private static void Shuffle<T>(IList<T> list, bool firstShuffle = false)
		{
			if (list.Count != 1)
			{
				for (int i = 0; i < list.Count - 1; i++)
				{
					int index = ((i != 0 || firstShuffle) ? UnityEngine.Random.Range(i, list.Count) : UnityEngine.Random.Range(0, list.Count - 1));
					T value = list[i];
					list[i] = list[index];
					list[index] = value;
				}
			}
		}

		private MissionOptions GetNextMapInternal()
		{
			switch (rotationType)
			{
			case RotationType.Sequence:
			{
				if (_nextIndex >= allMissions.Count)
				{
					_nextIndex = 0;
				}
				MissionOptions result2 = allMissions[_nextIndex];
				_nextIndex = (_nextIndex + 1) % allMissions.Count;
				return result2;
			}
			default:
				return allMissions.RandomElement();
			case RotationType.RandomQueue:
			{
				if (_nextIndex >= randomQueue.Count)
				{
					Shuffle(randomQueue);
					_nextIndex = 0;
				}
				MissionOptions result = randomQueue[_nextIndex];
				_nextIndex++;
				return result;
			}
			}
		}

		public MissionOptions GetNext()
		{
			if (NextOverride.HasValue)
			{
				MissionOptions value = NextOverride.Value;
				NextOverride = null;
				return value;
			}
			MissionOptions nextQueued = NextQueued;
			NextQueued = GetNextMapInternal();
			return nextQueued;
		}

		public MissionOptions PeakNext()
		{
			return NextOverride ?? NextQueued;
		}

		public void OverrideNext(MissionOptions option)
		{
			NextOverride = option;
		}

		public void ClearOverride()
		{
			NextOverride = null;
		}

		public int RemoveBrokenMap(MissionKeySaveable key)
		{
			ColorLog<DedicatedServerManager>.Info($"Failed to load mission. Removing {key} from missionRotation");
			int num = allMissions.RemoveAll((MissionOptions m) => m.Key.Equals(key));
			randomQueue?.RemoveAll((MissionOptions m) => m.Key.Equals(key));
			if (_nextIndex > 0)
			{
				_nextIndex--;
			}
			if (NextOverride.HasValue && key.Equals(NextOverride.Value.Key))
			{
				NextOverride = null;
			}
			else if (key.Equals(NextQueued.Key))
			{
				NextQueued = GetNextMapInternal();
			}
			ColorLog<DedicatedServerManager>.Info($"Removed {num} occurrences of {key} from missionRotation");
			return num;
		}
	}
}
