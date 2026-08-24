using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public abstract class DataField : MonoBehaviour
	{
		[SerializeField]
		protected TextMeshProUGUI label;

		public LayoutElement LabelLayout;

		public LayoutElement FieldLayout;

		private bool interactable = true;

		private bool interactableSetup;

		private Color? startingLabelColor;

		public Color LabelColor
		{
			get
			{
				return label.color;
			}
			set
			{
				label.color = value;
			}
		}

		public bool Interactable
		{
			get
			{
				return interactable;
			}
			set
			{
				if (!startingLabelColor.HasValue)
				{
					startingLabelColor = LabelColor;
				}
				LabelColor = startingLabelColor.Value * (value ? 1f : 0.8f);
				interactableSetup = true;
				interactable = value;
				SetFieldInteractable(value);
			}
		}

		protected abstract void SetFieldInteractable(bool value);

		private void Awake()
		{
			if (!interactableSetup)
			{
				Interactable = false;
			}
			if (!startingLabelColor.HasValue)
			{
				startingLabelColor = LabelColor;
			}
			AwakeSetup();
		}

		protected abstract void AwakeSetup();

		public void HideLabel()
		{
			LabelLayout.gameObject.SetActive(value: false);
		}
	}
}
