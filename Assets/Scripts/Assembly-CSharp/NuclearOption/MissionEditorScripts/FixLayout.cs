using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class FixLayout
	{
		private static bool requestRebuild;

		private static readonly List<RectTransform> toRebuild = new List<RectTransform>();

		private static List<LayoutGroup> cache = new List<LayoutGroup>();

		private static Queue<RectTransform> forceRebuildQueue = new Queue<RectTransform>();

		private static int rebuildCount = 0;

		private static int totalRebuilds = 0;

		private const int MAX_REBUILDS = 10;

		private FixLayout()
		{
		}

		public static void ForceRebuildAtEndOfFrame(RectTransform target)
		{
			if (!toRebuild.Contains(target))
			{
				toRebuild.Add(target);
				if (!requestRebuild)
				{
					requestRebuild = true;
					DelayRebuild().Forget();
				}
			}
		}

		private static async UniTask DelayRebuild()
		{
			await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
			requestRebuild = false;
			foreach (RectTransform item in toRebuild)
			{
				if (!(item == null))
				{
					ForceRebuildRecursive(item);
				}
			}
			toRebuild.Clear();
		}

		public static void ForceRebuildRecursive(RectTransform target)
		{
			forceRebuildQueue.Enqueue(target);
			if (rebuildCount == 0)
			{
				RebuildNow();
			}
		}

		private static void RebuildNow()
		{
			try
			{
				while (forceRebuildQueue.Count > 0)
				{
					rebuildCount++;
					if (rebuildCount > 10)
					{
						Debug.LogError("FixLayout reached max rebuilds");
						break;
					}
					RebuildNext();
				}
			}
			finally
			{
				rebuildCount = 0;
				totalRebuilds = 0;
				forceRebuildQueue.Clear();
			}
		}

		private static void RebuildNext()
		{
			forceRebuildQueue.Dequeue().GetComponentsInChildren(cache);
			cache.Reverse();
			foreach (LayoutGroup item in cache)
			{
				totalRebuilds++;
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)item.transform);
			}
		}

		public static void RebuildRoot(GameObject gameObject)
		{
			gameObject.GetComponentInParent<ILayoutRebuildRoot>()?.Rebuild();
		}

		public static void RebuildRootEndOfFrame(GameObject gameObject)
		{
			gameObject.GetComponentInParent<ILayoutRebuildRoot>()?.RebuildEndOfFrame();
		}
	}
}
