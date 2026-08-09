using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class ShowTMPLinkHoverText : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerMoveHandler
	{
		[Serializable]
		public struct LinkTooltipMapping
		{
			public string linkID;

			public string hoverText;
		}

		[Tooltip("Map the ID in your <link=ID> tag to the text you want to display.")]
		[SerializeField]
		private List<LinkTooltipMapping> linkMappings = new List<LinkTooltipMapping>();

		[SerializeField]
		private HoverText hover;

		private TextMeshProUGUI _textMeshPro;

		private Dictionary<string, string> _mappingDict;

		private int _currentLinkIndex = -1;

		private void Awake()
		{
			_textMeshPro = GetComponent<TextMeshProUGUI>();
			_mappingDict = new Dictionary<string, string>();
			foreach (LinkTooltipMapping linkMapping in linkMappings)
			{
				_mappingDict[linkMapping.linkID] = linkMapping.hoverText;
			}
		}

		public void SetHover(HoverText hoverText)
		{
			hover = hoverText;
		}

		public void SetLinkText(string linkID, string text)
		{
			_mappingDict[linkID] = text;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			ProcessHoverLogic(eventData);
		}

		public void OnPointerMove(PointerEventData eventData)
		{
			ProcessHoverLogic(eventData);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			HideCurrentTooltip();
			_currentLinkIndex = -1;
		}

		private void ProcessHoverLogic(PointerEventData eventData)
		{
			if (hover == null)
			{
				return;
			}
			int num = TMP_TextUtilities.FindIntersectingLink(_textMeshPro, eventData.position, eventData.pressEventCamera);
			if (num != -1)
			{
				if (num != _currentLinkIndex)
				{
					_currentLinkIndex = num;
					TMP_LinkInfo tMP_LinkInfo = _textMeshPro.textInfo.linkInfo[num];
					string linkID = tMP_LinkInfo.GetLinkID();
					if (_mappingDict.TryGetValue(linkID, out var value))
					{
						hover.Refresh(value);
						hover.Show(this, value);
					}
					else
					{
						HideCurrentTooltip();
					}
				}
				hover.Move(this, eventData.position);
			}
			else if (_currentLinkIndex != -1)
			{
				HideCurrentTooltip();
				_currentLinkIndex = -1;
			}
		}

		private void HideCurrentTooltip()
		{
			if (hover != null)
			{
				hover.Hide(this);
			}
		}
	}
}
