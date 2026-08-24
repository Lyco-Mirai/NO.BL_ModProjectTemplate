using System;
using NuclearOption.SceneLoading;
using NuclearOption.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	[Serializable]
	public class NewMissionMapButton : MonoBehaviour
	{
		[SerializeField]
		private MapDetails mapDetails;

		[SerializeField]
		private TextMeshProUGUI text;

		[SerializeField]
		private Image image;

		[SerializeField]
		private Button button;

		[SerializeField]
		private Outline outline;

		[SerializeField]
		private BetterBorder boarder;

		public Button Button => button;

		public void SetMap(MapDetails details)
		{
			mapDetails = details;
			text.text = details.MapName;
			image.sprite = details.MapImage;
		}

		internal void OnMapSelected(MapDetails selectedMap)
		{
			bool flag = mapDetails == selectedMap;
			image.color = (flag ? Color.white : Color.gray);
			if (outline != null)
			{
				outline.enabled = flag;
			}
			if (boarder != null)
			{
				boarder.enabled = flag;
			}
		}
	}
}
