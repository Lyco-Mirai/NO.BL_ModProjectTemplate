using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public class FileUtils
	{
		public static readonly ProfilerMarker ReadAllTextMarker = new ProfilerMarker("File.ReadAllText");

		private static readonly char[] trimChar = new char[2] { '.', ' ' };

		private static readonly string[] osReserved = new string[27]
		{
			"CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$", "CLOCK$", "COM0", "COM1", "COM2",
			"COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2",
			"LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
		};

		private static HashSet<char> _invalidChars;

		private static bool? _isCaseSensitive;

		public static bool IsCaseSensitive
		{
			get
			{
				if (!_isCaseSensitive.HasValue)
				{
					string persistentDataPath = Application.persistentDataPath;
					string path = persistentDataPath.ToUpperInvariant();
					string path2 = persistentDataPath.ToLowerInvariant();
					_isCaseSensitive = !Directory.Exists(path) || !Directory.Exists(path2);
				}
				return _isCaseSensitive.Value;
			}
		}

		private FileUtils()
		{
		}

		private static HashSet<char> GetInvalidChars()
		{
			if (_invalidChars == null)
			{
				_invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
				_invalidChars.Add('/');
				_invalidChars.Add('\\');
			}
			return _invalidChars;
		}

		public static char ValidateSaveName(string text, int charIndex, char addedChar)
		{
			if (GetInvalidChars().Contains(addedChar))
			{
				return '\0';
			}
			if (charIndex == 0 && (addedChar == '.' || char.IsWhiteSpace(addedChar)))
			{
				return '\0';
			}
			return addedChar;
		}

		public static bool CheckOsSafeName(ref string saveName)
		{
			if (string.IsNullOrEmpty(saveName))
			{
				return false;
			}
			string text = saveName;
			foreach (char invalidChar in GetInvalidChars())
			{
				if (saveName.Contains(invalidChar))
				{
					saveName = saveName.Replace(invalidChar.ToString(), "");
				}
			}
			saveName = saveName.Trim(trimChar);
			string[] array = osReserved;
			foreach (string b in array)
			{
				if (string.Equals(saveName, b, StringComparison.OrdinalIgnoreCase))
				{
					saveName += "_Safe";
					Debug.LogWarning("'" + text + "' is a system reserved name. Renamed to '" + saveName + "'");
					break;
				}
			}
			return saveName != text;
		}

		public static bool DeleteFile(string path)
		{
			if (File.Exists(path))
			{
				ColorLog<FileUtils>.Info("Deleting " + path);
				File.Delete(path);
				return true;
			}
			return false;
		}

		public static bool DeleteDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				ColorLog<FileUtils>.Info("Deleting " + path);
				Directory.Delete(path, recursive: true);
				return true;
			}
			return false;
		}

		public static void MoveDirectory(string pathOld, string pathNew)
		{
			if (NamesEqual(pathOld, pathNew))
			{
				ColorLog<FileUtils>.Info("Move paths the same skipping: " + pathOld + " -> " + pathNew);
				return;
			}
			if (!Directory.Exists(pathOld))
			{
				ColorLog<FileUtils>.InfoWarn("Move failed: Source " + pathOld + " does not exist.");
				return;
			}
			if (Directory.Exists(pathNew))
			{
				ColorLog<FileUtils>.InfoWarn("Destination Path Exists, deleting it first: " + pathNew);
				Directory.Delete(pathNew, recursive: true);
			}
			ColorLog<FileUtils>.Info("Moving " + pathOld + " -> " + pathNew);
			CheckDirectoryExists(Path.GetDirectoryName(pathNew));
			Directory.Move(pathOld, pathNew);
		}

		public static void CheckDirectoryExists(string path)
		{
			if (!Directory.Exists(path))
			{
				ColorLog<FileUtils>.Info("Creating " + path);
				Directory.CreateDirectory(path);
			}
		}

		public static bool NamesEqual(string a, string b)
		{
			if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
			{
				return a == b;
			}
			StringComparison comparisonType = (IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
			return string.Equals(a, b, comparisonType);
		}
	}
}
