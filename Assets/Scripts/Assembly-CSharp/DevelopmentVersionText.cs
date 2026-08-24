using System.IO;
using TMPro;
using UnityEngine;

public class DevelopmentVersionText : MonoBehaviour
{
	public TextMeshProUGUI Text;

	private void Awake()
	{
		Text.text = GetText();
	}

	private static string GetText()
	{
		if (!Debug.isDebugBuild)
		{
			return "";
		}
		if (Application.isEditor)
		{
			return "";
		}
		string text = Path.Combine(Application.dataPath, "..", "build-hash.txt");
		Debug.Log("Developer version file:" + text);
		if (!File.Exists(text))
		{
			Debug.LogWarning("Developer version file not found");
			return "";
		}
		string text2 = File.ReadAllText(text);
		text2 = text2.Trim();
		return "Private Build - " + text2;
	}
}
