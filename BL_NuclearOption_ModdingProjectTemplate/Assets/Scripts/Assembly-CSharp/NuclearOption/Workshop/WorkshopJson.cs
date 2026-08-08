using System;
using System.IO;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Workshop
{
	public struct WorkshopJson
	{
		public ulong Id;

		public SubscribedItemType Type;

		public string TypeHint;

		public readonly PublishedFileId_t PublishedId => new PublishedFileId_t(Id);

		private WorkshopJson(PublishedFileId_t id, SubscribedItemType type)
		{
			Id = id.m_PublishedFileId;
			Type = type;
			TypeHint = type.ToString();
		}

		private static string GetPath(string folder)
		{
			return Path.Combine(folder, "workshop.json");
		}

		private static string GetPath2(string folder)
		{
			return Path.Combine(folder, "Workshop.json");
		}

		public static WorkshopJson ReadFileOrInvalid(string folder)
		{
			return ReadFile(folder) ?? new WorkshopJson(PublishedFileId_t.Invalid, SubscribedItemType.Unknown);
		}

		public static WorkshopJson? ReadFile(string folder, bool ignoreNotFoundWarning = false)
		{
			string path = GetPath(folder);
			if (!File.Exists(path))
			{
				string path2 = GetPath2(folder);
				if (!File.Exists(path2))
				{
					if (!ignoreNotFoundWarning)
					{
						Debug.LogWarning("workshop.json file not found in " + folder);
					}
					return null;
				}
				path = path2;
			}
			try
			{
				return JsonUtility.FromJson<WorkshopJson>(File.ReadAllText(path));
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return null;
			}
		}

		public static void WriteFile(string folder, PublishedFileId_t id, SubscribedItemType type)
		{
			string contents = JsonUtility.ToJson(new WorkshopJson(id, type));
			File.WriteAllText(GetPath(folder), contents);
		}
	}
}
