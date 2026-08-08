using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public readonly struct MissionKey : IEquatable<MissionKey>
	{
		public readonly string Name;

		public readonly string Key;

		public readonly PublishedFileId_t? WorkshopId;

		public readonly MissionGroup Group;

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(Key);
		}

		public MissionKey(string name, MissionGroup group)
			: this(name, name, null, group)
		{
		}

		public MissionKey(string key, string name, PublishedFileId_t? workshopId, MissionGroup group)
		{
			Name = name;
			Key = key;
			WorkshopId = workshopId;
			Group = group;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetJson(out string json)
		{
			return Group.TryGetJson(this, out json);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UniTask<Sprite> GetPreview(CancellationToken token = default(CancellationToken))
		{
			return Group.GetPreview(Key, token);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryLoad(out Mission mission, out string error)
		{
			return MissionSaveLoad.TryLoad(this, out mission, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryQuickLoad(out MissionQuickLoad mission)
		{
			return MissionSaveLoad.QuickLoadOne(this, out mission);
		}

		public override bool Equals(object obj)
		{
			if (obj is MissionKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(MissionKey other)
		{
			if (Name == other.Name && Key == other.Key)
			{
				return Group == other.Group;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Key, Group);
		}

		public void ThrowIfInvalid()
		{
			if (string.IsNullOrEmpty(Name))
			{
				throw new ArgumentException("Mission had no name");
			}
			if (string.IsNullOrEmpty(Key))
			{
				throw new ArgumentException("Mission had no key");
			}
			if (Group == null)
			{
				throw new ArgumentException("Mission had no load group");
			}
		}

		public override string ToString()
		{
			string text = Group?.Name ?? "NULL";
			if (WorkshopId.HasValue)
			{
				if (Key == Name)
				{
					return $"[{text},{Key},id:{WorkshopId.Value}]";
				}
				return $"[{text},{Key},{Name},id:{WorkshopId.Value}]";
			}
			if (Key == Name)
			{
				return "[" + text + "," + Key + "]";
			}
			return "[" + text + "," + Key + "," + Name + "]";
		}
	}
}
