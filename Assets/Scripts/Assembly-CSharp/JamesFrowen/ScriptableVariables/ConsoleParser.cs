using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JamesFrowen.ScriptableVariables
{
	public static class ConsoleParser
	{
		private static List<string> tokenizeCahce = new List<string>();

		public static List<string> TokenizeNonAlloc(string input)
		{
			tokenizeCahce.Clear();
			Tokenize(input, tokenizeCahce);
			return tokenizeCahce;
		}

		public static List<string> Tokenize(string input)
		{
			List<string> list = new List<string>();
			Tokenize(input, list);
			return list;
		}

		public static void Tokenize(string input, List<string> results, bool clearList = false)
		{
			if (input.Length > 10000)
			{
				throw new ArgumentException("Input string too long. Max length 10000");
			}
			if (clearList)
			{
				results.Clear();
			}
			int pos = 0;
			while (pos < input.Length)
			{
				if (isWhiteSpace(input, pos))
				{
					pos++;
				}
				else if (isNonEscapedQuote(input, pos))
				{
					results.Add(parseQuoted(input, ref pos));
				}
				else
				{
					results.Add(parse(input, ref pos));
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool isWhiteSpace(string input, int pos)
		{
			return input[pos] switch
			{
				' ' => true, 
				'\t' => true, 
				_ => false, 
			};
		}

		private static bool isNonEscapedQuote(string input, int pos)
		{
			bool flag = input[pos] == '"';
			if (pos == 0)
			{
				return flag;
			}
			bool flag2 = input[pos - 1] == '\\';
			if (flag)
			{
				return !flag2;
			}
			return false;
		}

		private static string parseQuoted(string input, ref int pos)
		{
			pos++;
			int num = pos;
			while (pos < input.Length)
			{
				if (isNonEscapedQuote(input, pos))
				{
					pos++;
					return input.Substring(num, pos - num - 1);
				}
				pos++;
			}
			return input.Substring(num);
		}

		private static string parse(string input, ref int pos)
		{
			int num = pos;
			while (pos < input.Length)
			{
				if (isWhiteSpace(input, pos))
				{
					return input.Substring(num, pos - num);
				}
				pos++;
			}
			return input.Substring(num);
		}
	}
}
