using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOption.UI
{
	public class LeaderBoardRightClickMenuTrigger : RightClickDropdownMenuTrigger
	{
		[SerializeField]
		private LeaderboardPlayerEntry entry;

		public override void OnPointerClick(PointerEventData eventData)
		{
			if (!GameManager.IsLocalPlayer(entry.Player))
			{
				base.OnPointerClick(eventData);
			}
		}
	}
}
