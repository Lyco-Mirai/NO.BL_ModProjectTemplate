using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Steamworks;

namespace NuclearOption.MissionEditorScripts
{
	public static class EnumNames
	{
		private static Regex pattern = new Regex("(\\B[A-Z])");

		private static readonly int eResultStartIndex = "k_EResult".Length;

		public static string Nicify(string name)
		{
			return pattern.Replace(name, " $1");
		}

		public static string ToNicifyString<T>(this T value) where T : struct, Enum
		{
			return Nicify(value.ToString());
		}

		public static string Nicify(this EResult eResult)
		{
			return Nicify(eResult.ToString().Substring(eResultStartIndex));
		}
	}
	public static class EnumNames<T> where T : struct, Enum
	{
		private static List<string> realNames;

		private static List<string> names;

		public static T Parse(string name)
		{
			int index = names.IndexOf(name);
			return Enum.Parse<T>(realNames[index]);
		}

		public static List<string> GetNames()
		{
			if (names == null)
			{
				realNames = new List<string>(Enum.GetNames(typeof(T)));
				names = new List<string>(realNames.Select(EnumNames.Nicify));
			}
			return names;
		}
	}
}
