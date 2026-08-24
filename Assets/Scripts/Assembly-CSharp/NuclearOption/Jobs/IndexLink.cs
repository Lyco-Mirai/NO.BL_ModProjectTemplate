using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace NuclearOption.Jobs
{
	public struct IndexLink
	{
		public NullableIndex TransformIndex;

		public override string ToString()
		{
			return TransformIndex.ToString();
		}

		public static void AddTransform(TransformAccessArray transformAccess, NativeArray<PtrRefCounter<IndexLink>> transformLinks, List<PtrRefCounter<IndexLink>> partLinks, ref PtrRefCounter<IndexLink> field, Transform transform)
		{
			JobsAllocator<IndexLink>.AllocateRefCounter(ref field);
			AddLinked(transformAccess, transformLinks, field, transform);
			partLinks.Add(field);
			field.AddRef();
		}

		private static void AddLinked(TransformAccessArray transformAccess, NativeArray<PtrRefCounter<IndexLink>> transformLinks, PtrRefCounter<IndexLink> link, Transform toAdd)
		{
			int length = transformAccess.length;
			transformAccess.Add(toAdd);
			transformLinks[length] = link;
			link.AddRef();
			link.Ref().TransformIndex = new NullableIndex(length);
		}

		public static void QueueToRemove(List<int> outToRemove, NativeArray<PtrRefCounter<IndexLink>> transformLinks, List<PtrRefCounter<IndexLink>> linksToRemove)
		{
			foreach (PtrRefCounter<IndexLink> item in linksToRemove)
			{
				NullableIndex transformIndex = item.Value().TransformIndex;
				if (transformIndex.HasValue)
				{
					outToRemove.Add(transformIndex.Index);
				}
				item.RemoveRef();
			}
			linksToRemove.Clear();
		}

		public static void RemoveLinks(TransformAccessArray transformAccess, NativeArray<PtrRefCounter<IndexLink>> transformLinks, List<int> toRemove)
		{
			toRemove.Sort();
			for (int num = toRemove.Count - 1; num >= 0; num--)
			{
				int removeIndex = toRemove[num];
				RemoveLinked(transformAccess, transformLinks, removeIndex);
			}
			toRemove.Clear();
		}

		private static void RemoveLinked(TransformAccessArray transformAccess, NativeArray<PtrRefCounter<IndexLink>> linkArray, int removeIndex)
		{
			int num = transformAccess.length - 1;
			transformAccess.RemoveAtSwapBack(removeIndex);
			PtrRefCounter<IndexLink> ptrRefCounter = linkArray[removeIndex];
			ptrRefCounter.Ref().TransformIndex = default(NullableIndex);
			ptrRefCounter.RemoveRef();
			if (num != removeIndex)
			{
				(linkArray[removeIndex] = linkArray[num]).Ref().TransformIndex = new NullableIndex(removeIndex);
			}
		}

		public static bool BurstGetTransformIndex(PtrRefCounter<IndexLink> link, out int index)
		{
			if (!link.IsCreated)
			{
				Debug.LogError("Link was not allocated");
				index = 0;
				return false;
			}
			NullableIndex transformIndex = link.Value().TransformIndex;
			if (!transformIndex.HasValue)
			{
				Debug.LogError("Not Transform index");
				index = 0;
				return false;
			}
			index = transformIndex.Index;
			return true;
		}
	}
}
