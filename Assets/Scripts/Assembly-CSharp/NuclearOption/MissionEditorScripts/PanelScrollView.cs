using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class PanelScrollView : MonoBehaviour, ILayoutRebuildRoot
	{
		[SerializeField]
		private bool rebuildParent;

		[SerializeField]
		private RectTransform scrollParent;

		[SerializeField]
		private RectTransform scrollContent;

		[SerializeField]
		private float maxHeight;

		[SerializeField]
		private bool useMinWidth;

		[SerializeField]
		private float minWidth;

		private bool isRebuilding;

		private bool endOfFrameRequestRebuild;

		private RectTransform child;

		public T AddChild<T>(T prefab) where T : Component
		{
			T val = UnityEngine.Object.Instantiate(prefab, scrollContent);
			child = (RectTransform)val.transform;
			child.OnDestroyAsync().ContinueWith((Action)ChildDestroyed).Forget();
			return val;
		}

		public GameObject AddChild(GameObject prefab)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab, scrollContent);
			child = (RectTransform)gameObject.transform;
			child.OnDestroyAsync().ContinueWith((Action)ChildDestroyed).Forget();
			return gameObject;
		}

		private void ChildDestroyed()
		{
			if (this != null)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		public void SetChild(RectTransform menu, bool rebuild = true)
		{
			child = menu;
			if (rebuild)
			{
				Rebuild();
			}
		}

		void ILayoutRebuildRoot.Rebuild()
		{
			if (child != null)
			{
				Rebuild();
			}
		}

		void ILayoutRebuildRoot.RebuildEndOfFrame()
		{
			if (!endOfFrameRequestRebuild)
			{
				endOfFrameRequestRebuild = true;
				DoRebuildEndOfFrame().Forget();
			}
		}

		private async UniTaskVoid DoRebuildEndOfFrame()
		{
			await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
			endOfFrameRequestRebuild = false;
			if (child != null)
			{
				Rebuild();
			}
		}

		public void Rebuild()
		{
			if (isRebuilding)
			{
				return;
			}
			isRebuilding = true;
			try
			{
				FixLayout.ForceRebuildRecursive(child);
				float num = LayoutUtility.GetMinWidth(child);
				if (useMinWidth)
				{
					num = Mathf.Max(num, minWidth);
				}
				float minHeight = LayoutUtility.GetMinHeight(child);
				scrollParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num);
				scrollParent.SetRectHeight(Mathf.Min(minHeight, maxHeight));
				scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num);
				scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minHeight);
				if (rebuildParent)
				{
					FixLayout.ForceRebuildRecursive(base.transform.parent.AsRectTransform());
				}
			}
			finally
			{
				isRebuilding = false;
			}
		}
	}
}
