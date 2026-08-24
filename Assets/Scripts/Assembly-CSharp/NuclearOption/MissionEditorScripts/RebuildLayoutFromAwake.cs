using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class RebuildLayoutFromAwake : MonoBehaviour
	{
		[SerializeField]
		private RectTransform target;

		private void OnValidate()
		{
			if (target == null)
			{
				target = base.transform.AsRectTransform();
			}
		}

		private void Awake()
		{
			if (target == null)
			{
				target = base.transform.AsRectTransform();
			}
			FixLayout.ForceRebuildAtEndOfFrame(target);
		}
	}
}
