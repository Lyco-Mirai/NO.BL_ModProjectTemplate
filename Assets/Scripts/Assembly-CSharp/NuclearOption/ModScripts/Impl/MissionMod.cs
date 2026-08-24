using System;
using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using NuclearOption.Workshop;
using UnityEngine;

namespace NuclearOption.ModScripts.Impl
{
	internal sealed class MissionMod : ModType
	{
		public MissionMod()
			: base("Mission")
		{
		}

		protected override string GetRootFolder()
		{
			return MissionGroup.UserGroup.UserMissionDirectory;
		}

		public override List<string> ListItems()
		{
			return (from x in MissionGroup.User.GetMissions()
				select x.Name).ToList();
		}

		public override WorkshopUploadItem GetItem(string itemName)
		{
			if (string.IsNullOrEmpty(itemName))
			{
				throw new InvalidOperationException("Page 3 should not be active when mission name is null");
			}
			if (!MissionSaveLoad.TryLoad(new MissionKey(itemName, MissionGroup.User), out var _, out var error))
			{
				Debug.LogError(error);
				return null;
			}
			string folder = MissionGroup.UserGroup.GetFolder(itemName);
			return new WorkshopUploadItem(Tag, SubscribedItemType.Mission, folder, itemName);
		}
	}
}
