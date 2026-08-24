using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Workshop
{
	public class TabController : MonoBehaviour
	{
		public delegate void TabChangedDelegate(string label, int index);

		[SerializeField]
		private Button buttonTemplate;

		[SerializeField]
		private List<string> options;

		[SerializeField]
		private List<Button> allButtons;

		[SerializeField]
		private List<ButtonStyleController> allButtonsStyle;

		[SerializeField]
		private int buttonWidth;

		[SerializeField]
		private ButtonStyle activeStyle;

		[SerializeField]
		private ButtonStyle inactiveStyle;

		[SerializeField]
		private int activeIndex;

		private bool hasSetup;

		public event TabChangedDelegate TabChanged;

		private void Awake()
		{
			for (int i = 0; i < allButtons.Count; i++)
			{
				SetupNewButton(i);
			}
		}

		private void SetupNewButton(int i)
		{
			Button button = allButtons[i];
			button.name = $"Tab button {i}";
			button.onClick.AddListener(delegate
			{
				OnClick(button);
			});
			RectTransform rectTransform = (RectTransform)button.transform;
			rectTransform.anchoredPosition = new Vector2(buttonWidth * i, rectTransform.anchoredPosition.y);
		}

		private void OnValidate()
		{
			for (int i = 0; i < allButtons.Count; i++)
			{
				if (i < options.Count)
				{
					Button button = allButtons[i];
					if (!(button == null))
					{
						button.GetComponentInChildren<TextMeshProUGUI>().text = options[i];
					}
				}
			}
		}

		public void Start()
		{
			if (!hasSetup)
			{
				Setup();
			}
		}

		public void Setup(List<string> options, int active)
		{
			this.options.Clear();
			this.options.AddRange(options);
			activeIndex = active;
			Setup();
		}

		private void Setup()
		{
			hasSetup = true;
			while (allButtons.Count < options.Count)
			{
				Button item = Object.Instantiate(buttonTemplate, base.transform);
				allButtons.Add(item);
				SetupNewButton(allButtons.Count - 1);
			}
			for (int i = 0; i < allButtons.Count; i++)
			{
				Button button = allButtons[i];
				if (i < options.Count)
				{
					button.gameObject.SetActive(value: true);
					button.GetComponentInChildren<TextMeshProUGUI>().text = options[i];
				}
				else
				{
					button.gameObject.SetActive(value: false);
				}
			}
			SetActiveButton(activeIndex);
			this.TabChanged?.Invoke(options[activeIndex], activeIndex);
		}

		private void OnClick(Button clicked)
		{
			int num = allButtons.IndexOf(clicked);
			SetActiveButton(num);
			this.TabChanged?.Invoke(options[num], num);
		}

		private void SetActiveButton(int index)
		{
			activeIndex = index;
			for (int num = options.Count - 1; num >= 0; num--)
			{
				allButtons[num].transform.SetSiblingIndex(options.Count - 1 - num);
			}
			for (int i = 0; i < options.Count; i++)
			{
				ButtonStyleController buttonStyleController = allButtonsStyle[i];
				if (activeIndex == i)
				{
					buttonStyleController.ApplyStyle(activeStyle);
					buttonStyleController.transform.SetAsLastSibling();
				}
				else
				{
					buttonStyleController.ApplyStyle(inactiveStyle);
				}
			}
		}
	}
}
