using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class ReferenceDataField : DataField
	{
		private const string NONE_DARK_TEXT = "<color=#444><size=80%><none></size></color>";

		private const string NONE_LIGHT_TEXT = "<color=#AAA><size=80%><none></size></color>";

		[SerializeField]
		private Button openButton;

		[SerializeField]
		private TextMeshProUGUI currentValueText;

		public ReferencePopup Popup;

		[SerializeField]
		private float paddingBetweenPopup;

		[Tooltip("should light text be used for <none> instead of dark")]
		[SerializeField]
		private bool useLightNone;

		private ISaveableReference current;

		private ReferenceList.ListWrapper options;

		private Action<ISaveableReference> setValue;

		private bool allowNone;

		private bool dropdownOpen;

		protected override void SetFieldInteractable(bool value)
		{
			openButton.interactable = value;
		}

		protected override void AwakeSetup()
		{
			openButton.onClick.AddListener(OpenPopup);
			Popup.Hide();
			UniTask.Void(async delegate
			{
				CancellationToken cancel = base.destroyCancellationToken;
				await UniTask.Yield();
				await UniTask.Yield();
				if (!cancel.IsCancellationRequested)
				{
					RectTransform obj = (RectTransform)Popup.transform;
					Canvas componentInParent = GetComponentInParent<Canvas>();
					obj.SetParent(componentInParent.transform, worldPositionStays: true);
				}
			});
		}

		private void Update()
		{
			if (Popup.Holder.activeSelf)
			{
				UpdatePopupPosition();
			}
		}

		private void OnValidate()
		{
			if (Popup != null && Popup.Holder.activeSelf)
			{
				UpdatePopupPosition();
			}
		}

		private void UpdatePopupPosition()
		{
			RectTransform obj = (RectTransform)Popup.transform;
			RectTransform rectTransform = (RectTransform)base.transform;
			float x = rectTransform.lossyScale.x;
			float x2 = rectTransform.position.x + (rectTransform.rect.width / 2f + paddingBetweenPopup) * x;
			float y = rectTransform.position.y;
			obj.position = new Vector2(x2, y);
		}

		private void OnDestroy()
		{
			if (Popup.Holder != null)
			{
				UnityEngine.Object.Destroy(Popup.Holder);
			}
		}

		public void Setup<T>(string label, List<T> options, T current, Action<T> setValue) where T : class, ISaveableReference
		{
			Setup(label, (IList)options, current, setValue);
		}

		public void Setup<T>(string label, IList options, T current, Action<T> setValue) where T : class, ISaveableReference
		{
			base.label.text = label;
			this.options = new ReferenceList.ListWrapper(options);
			this.setValue = delegate(ISaveableReference t)
			{
				setValue((T)t);
			};
			allowNone = true;
			SetValue(current);
			base.Interactable = true;
		}

		public void SetupReadOnly(string label, ISaveableReference current)
		{
			base.label.text = label;
			options = null;
			setValue = null;
			SetValue(current);
			base.Interactable = false;
			Popup.Hide();
		}

		public void SetupReadOnly(string label, string text)
		{
			base.label.text = label;
			options = null;
			setValue = null;
			current = null;
			currentValueText.text = text ?? (useLightNone ? "<color=#AAA><size=80%><none></size></color>" : "<color=#444><size=80%><none></size></color>");
			base.Interactable = false;
			Popup.Hide();
		}

		private void SetValue(ISaveableReference current)
		{
			this.current = current;
			currentValueText.text = current?.ToUIString(oneLine: true) ?? (useLightNone ? "<color=#AAA><size=80%><none></size></color>" : "<color=#444><size=80%><none></size></color>");
		}

		private void OpenPopup()
		{
			if (dropdownOpen)
			{
				Debug.LogWarning("Drop down already open");
				return;
			}
			dropdownOpen = true;
			Popup.ShowPickOption(current, allowNone, () => options, (ISaveableReference o) => o.ToUIString(), delegate(bool pick, ISaveableReference obj)
			{
				dropdownOpen = false;
				if (pick)
				{
					SetValue(obj);
					setValue(obj);
				}
			});
		}
	}
}
