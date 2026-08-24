using System;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionLoadErrorItem : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI header;

		[SerializeField]
		private Image headerBackground;

		[SerializeField]
		private Button expand;

		[SerializeField]
		private GameObject bodyHolder;

		[SerializeField]
		private TextMeshProUGUI body;

		[SerializeField]
		private Image bodyBackground;

		[SerializeField]
		private Color headerWarningColor;

		[SerializeField]
		private Color bodyWarningColor;

		[SerializeField]
		private Color headerErrorColor;

		[SerializeField]
		private Color bodyErrorColor;

		private bool isExpanded;

		[SerializeField]
		private bool _debugColorError;

		private void OnValidate()
		{
			headerBackground.color = (_debugColorError ? headerErrorColor : headerWarningColor);
			bodyBackground.color = (_debugColorError ? bodyErrorColor : bodyWarningColor);
		}

		private void Awake()
		{
			expand.onClick.AddListener(ToggleExpand);
			isExpanded = false;
			bodyHolder.SetActive(isExpanded);
		}

		private void ToggleExpand()
		{
			isExpanded = !isExpanded;
			bodyHolder.SetActive(isExpanded);
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)base.transform);
		}

		public void SetWarning(string msg)
		{
			SetHeadText(msg);
			body.text = msg;
		}

		public void SetError(string msg)
		{
			SetHeadText(msg);
			body.text = msg.ToString();
		}

		public void SetException(ExceptionEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.Message))
			{
				SetHeadText(entry.Message);
				body.text = $"{entry.Message}\n\n{entry.Exception}";
			}
			else
			{
				SetHeadText(entry.Exception?.Message ?? "Exception");
				body.text = entry.Exception?.ToString() ?? "";
			}
		}

		public void SetException(Exception exception)
		{
			SetException(new ExceptionEntry(exception));
		}

		private void SetHeadText(string msg)
		{
			string text = msg;
			int num = msg.IndexOf("\n");
			if (num != -1)
			{
				text = msg.Substring(0, num);
			}
			header.text = text;
		}
	}
}
