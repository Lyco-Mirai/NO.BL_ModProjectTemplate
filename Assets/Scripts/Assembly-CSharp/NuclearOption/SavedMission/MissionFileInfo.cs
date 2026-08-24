using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	public struct MissionFileInfo
	{
		public MissionKey key;

		public DateTime? LastEdit;

		public DateTime? ExpandedLastEdit;

		public List<MissionFileInfo> SubItems;

		public bool HasExtraSaves => SubItems != null;

		public MissionFileInfo StripSubItems()
		{
			MissionFileInfo result = this;
			result.SubItems = null;
			result.LastEdit = ExpandedLastEdit;
			return result;
		}
	}
}
