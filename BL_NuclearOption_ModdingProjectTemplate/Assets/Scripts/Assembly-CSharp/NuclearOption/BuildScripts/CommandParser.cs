using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NuclearOption.BuildScripts
{
	public static class CommandParser
	{
		public delegate void HandleArgDelegate(CommandArguments arguments);

		public delegate UniTask HandleArgDelegateCoroutine(CommandArguments arguments);

		public struct ArgCommand
		{
			public string Start;

			public HandleArgDelegate Handler;

			public ArgCommand(string start, HandleArgDelegateCoroutine handler)
			{
				Start = (start ?? throw new ArgumentNullException("start")).ToLower();
				Handler = delegate(CommandArguments a)
				{
					handler(a).Forget();
				};
			}

			public ArgCommand(string start, HandleArgDelegate handler)
			{
				Start = (start ?? throw new ArgumentNullException("start")).ToLower();
				Handler = handler ?? throw new ArgumentNullException("handler");
			}
		}

		public struct CommandArguments
		{
			public readonly int start;

			public readonly List<string> AllArgs;

			public CommandArguments(int start, List<string> allArgs)
			{
				this.start = start;
				AllArgs = allArgs;
			}

			public string GetNext(int offset)
			{
				int num = start + offset;
				if (num < 0 || num >= AllArgs.Count)
				{
					throw new ArgumentOutOfRangeException("offset", "Argument offset is out of bounds.");
				}
				string text = AllArgs[num];
				if (text.StartsWith("-"))
				{
					throw new InvalidOperationException("Expected a value but next argument was a command. Value = '" + text + "'");
				}
				return text;
			}

			public bool TryGetNext(int offset, out string value)
			{
				int num = start + offset;
				if (num < 0 || num >= AllArgs.Count)
				{
					value = null;
					return false;
				}
				string text = AllArgs[num];
				if (text.StartsWith("-"))
				{
					value = null;
					return false;
				}
				value = text;
				return true;
			}

			public bool GetNextBool(int offset)
			{
				return bool.Parse(GetNext(offset));
			}

			public bool TryGetNextBool(int offset, out bool value)
			{
				if (TryGetNext(offset, out var value2))
				{
					return bool.TryParse(value2, out value);
				}
				value = false;
				return false;
			}

			public int GetNextInt(int offset)
			{
				return int.Parse(GetNext(offset), CultureInfo.InvariantCulture);
			}

			public bool TryGetNextInt(int offset, out int value)
			{
				if (TryGetNext(offset, out var value2))
				{
					return int.TryParse(value2, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
				}
				value = 0;
				return false;
			}

			public ulong GetNextULong(int offset)
			{
				return ulong.Parse(GetNext(offset), CultureInfo.InvariantCulture);
			}

			public bool TryGetNextULong(int offset, out ulong value)
			{
				if (TryGetNext(offset, out var value2))
				{
					return ulong.TryParse(value2, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
				}
				value = 0uL;
				return false;
			}

			public TEnum GetNextEnum<TEnum>(int offset) where TEnum : struct, Enum
			{
				return Enum.Parse<TEnum>(GetNext(offset), ignoreCase: true);
			}

			public bool TryGetNextEnum<TEnum>(int offset, out TEnum value) where TEnum : struct, Enum
			{
				if (TryGetNext(offset, out var value2))
				{
					return Enum.TryParse<TEnum>(value2, ignoreCase: true, out value);
				}
				value = default(TEnum);
				return false;
			}
		}

		public static void Parse(List<string> stringArgs, List<ArgCommand> argCommands)
		{
			Debug.Log($"Command Line Args: {stringArgs.Count}");
			foreach (string stringArg in stringArgs)
			{
				Debug.Log("  " + stringArg);
			}
			for (int i = 0; i < stringArgs.Count; i++)
			{
				string text = stringArgs[i];
				if (!text.StartsWith("-"))
				{
					continue;
				}
				string text2 = text.ToLower();
				foreach (ArgCommand argCommand in argCommands)
				{
					if (text2 == argCommand.Start)
					{
						try
						{
							Debug.Log("Found command:" + argCommand.Start);
							argCommand.Handler(new CommandArguments(i, stringArgs));
						}
						catch (Exception arg)
						{
							Debug.LogError($"Command failed with error: {arg}");
						}
					}
				}
			}
		}
	}
}
