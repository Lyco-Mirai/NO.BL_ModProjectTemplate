using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NuclearOption.BuildScripts.DebugTests
{
	public static class DescriptionTests
	{
		public static void RunAllTests()
		{
			TestShortString();
			TestVeryShortString();
			TestLongString();
			TestMaxLength();
			TestSpecialCharacters();
			TestJapaneseText();
			TestEmptyString();
		}

		private static void TestShortString()
		{
			string text = "This is a short description that should fit easily into a single chunk.";
			List<string> list = StringHelper.SplitStringByByteCount(text, includeEmptyLast: true, 255, 10);
			var (flag, text2) = Validate(text, list);
			if (flag && list.Count == 2 && list[0] == text && list[1] == "")
			{
				Debug.Log("Test 1a Passed: Short string handled correctly. ✅");
				return;
			}
			Debug.LogError(string.Format("Test 1a Failed: {0}. Expected 2 chunks with first chunk containing '{1}' and second being empty. Got {2} chunks. First value: '{3}'. ❌", text2 ?? "Custom check failed", text, list.Count, (list.Count > 0) ? list[0] : "N/A"));
		}

		private static void TestVeryShortString()
		{
			string text = "Kinda short, so it fits in 1";
			List<string> list = StringHelper.SplitStringByByteCount(text, includeEmptyLast: true, 255, 10);
			var (flag, text2) = Validate(text, list);
			if (flag && list.Count == 2 && list[0] == text && list[1] == "")
			{
				Debug.Log("Test 1b Passed: Short string handled correctly. ✅");
				return;
			}
			Debug.LogError(string.Format("Test 1b Failed: {0}. Expected 2 chunks with first chunk containing '{1}' and second being empty. Got {2} chunks. First value: '{3}'. ❌", text2 ?? "Custom check failed", text, list.Count, (list.Count > 0) ? list[0] : "N/A"));
		}

		private static void TestLongString()
		{
			string text = new string('a', 500);
			List<string> list = StringHelper.SplitStringByByteCount(text, includeEmptyLast: true, 255, 10);
			var (flag, text2) = Validate(text, list);
			if (flag && list.Count == 3 && Encoding.UTF8.GetByteCount(list[0]) <= 255 && Encoding.UTF8.GetByteCount(list[1]) <= 255 && list[2] == "")
			{
				Debug.Log("Test 2 Passed: Long string split into multiple chunks correctly. ✅");
				return;
			}
			Debug.LogError(string.Format("Test 2 Failed: {0}. Expected 3 chunks. Got {1} chunks. First length: {2}, second length: {3}, third length: {4}. ❌", text2 ?? "Custom check failed", list.Count, list[0].Length, list[1].Length, (list.Count > 2) ? list[2].Length.ToString() : "N/A"));
		}

		private static void TestMaxLength()
		{
			string text = new string('a', 2000);
			List<string> list = StringHelper.SplitStringByByteCount(text, includeEmptyLast: true, 255, 10);
			(bool pass, string failReason) tuple = Validate(text, list);
			bool item = tuple.pass;
			string item2 = tuple.failReason;
			string text2 = string.Join("", list.Take(list.Count - 1));
			if (item && text2.Length <= 1000)
			{
				Debug.Log("Test 3 Passed: Long string correctly truncated to max length. ✅");
			}
			else
			{
				Debug.LogError(string.Format("Test 3 Failed: {0}. Expected merged length to be {1}. Got {2}. ❌", item2 ?? "Custom check failed", 1000, text2.Length));
			}
		}

		private static void TestSpecialCharacters()
		{
			string text = "Hello, world! \ud83d\ude80\ud83c\udf0c✨ This text uses emojis, which have a different byte count.";
			List<string> list = StringHelper.SplitStringByByteCount(text, includeEmptyLast: true, 255, 10);
			(bool pass, string failReason) tuple = Validate(text, list);
			bool item = tuple.pass;
			string item2 = tuple.failReason;
			int num = list.Sum((string x) => Encoding.UTF8.GetByteCount(x));
			if (item && list.Count > 1 && num <= Encoding.UTF8.GetByteCount(text) + 2 && list.Last() == "")
			{
				Debug.Log("Test 4 Passed: String with special characters handled correctly. ✅");
			}
			else
			{
				Debug.LogError(string.Format("Test 4 Failed: {0}. Expected multiple chunks for special characters, ending with an empty string. Got {1} chunks. First value: '{2}'. ❌", item2 ?? "Custom check failed", list.Count, (list.Count > 0) ? list[0] : "N/A"));
			}
		}

		private static void TestJapaneseText()
		{
			List<string> results = StringHelper.SplitStringByByteCount("これは日本語のテキストであり、UTF-8エンコーディングでは文字ごとに複数のバイトが使用されます。このテストケースは、関数が正しくテキストを分割し、バイト制限を尊重していることを確認するためにあります。", includeEmptyLast: true, 255, 10);
			var (flag, text) = Validate("これは日本語のテキストであり、UTF-8エンコーディングでは文字ごとに複数のバイトが使用されます。このテストケースは、関数が正しくテキストを分割し、バイト制限を尊重していることを確認するためにあります。", results);
			if (flag)
			{
				Debug.Log("Test 5 Passed: Japanese text correctly split into chunks under the 255-byte limit. ✅");
			}
			else
			{
				Debug.LogError("Test 5 Failed: " + text + ". ❌");
			}
		}

		private static void TestEmptyString()
		{
			List<string> list = StringHelper.SplitStringByByteCount("", includeEmptyLast: true, 255, 10);
			var (flag, text) = Validate("", list);
			if (flag && list.Count == 1 && list[0] == "")
			{
				Debug.Log("Test 6 Passed: Empty string handled correctly. ✅");
			}
			else
			{
				Debug.LogError(string.Format("Test 6 Failed: {0}. Expected 1 empty chunk. Got {1} chunks. First value: '{2}'. ❌", text ?? "Custom check failed", list.Count, (list.Count > 0) ? list[0] : "N/A"));
			}
		}

		private static (bool pass, string failReason) Validate(string text, List<string> results)
		{
			if (!results.All((string x) => Encoding.UTF8.GetByteCount(x) <= 255))
			{
				return (pass: false, failReason: "At least one chunk exceeded the 255-byte limit.");
			}
			if (results.Count > 10)
			{
				return (pass: false, failReason: $"Resulting chunks ({results.Count}) exceeded the maximum allowed chunks ({10}).");
			}
			if (results.Count < 10 && results.Last().Length > 0)
			{
				return (pass: false, failReason: "Last chunk should be empty if under max number of chunks");
			}
			string text2 = string.Join("", results);
			if (!text.StartsWith(text2))
			{
				return (pass: false, failReason: "Text did not start with merged result\ntext=" + text + "\nresult=" + text2);
			}
			return (pass: true, failReason: null);
		}
	}
}
