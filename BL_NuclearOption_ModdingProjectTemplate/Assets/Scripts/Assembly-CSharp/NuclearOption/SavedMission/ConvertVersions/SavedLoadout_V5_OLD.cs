using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedLoadout_V5_OLD
	{
		[Serializable]
		[Obsolete("V5", true)]
		public struct SelectedMount_V5_OLD
		{
			public string Key;
		}

		public List<SelectedMount_V5_OLD> Selected = new List<SelectedMount_V5_OLD>();
	}
}
