using UnityEngine;

namespace JamesFrowen.ScriptableVariables.UI
{
	public abstract class ListItem<T> : MonoBehaviour
	{
		[SerializeField]
		private GameObject activeRoot;

		public T Value { get; private set; }

		public virtual bool IsActive
		{
			get
			{
				CheckActiveRoot();
				return activeRoot.activeSelf;
			}
		}

		protected virtual void Awake()
		{
		}

		private void CheckActiveRoot()
		{
			if (activeRoot == null)
			{
				activeRoot = base.gameObject;
			}
		}

		public virtual void SetActive(bool active)
		{
			CheckActiveRoot();
			if (activeRoot.activeSelf != active)
			{
				activeRoot.SetActive(active);
			}
		}

		protected abstract void SetValue(T value);

		public virtual float CalculateHeight(T value)
		{
			return ((RectTransform)base.transform).sizeDelta.y;
		}

		public void SetNewValue(T value)
		{
			Value = value;
			SetValue(value);
		}

		public void Redraw()
		{
			SetValue(Value);
		}
	}
}
