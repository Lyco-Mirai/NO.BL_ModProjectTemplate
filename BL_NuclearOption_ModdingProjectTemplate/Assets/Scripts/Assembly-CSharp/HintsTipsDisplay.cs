using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HintsTipsDisplay : MonoBehaviour
{
	private struct HintTip
	{
		public int id;

		public string type;

		public string text;
	}

	public Button leftButton;

	public Button rightButton;

	public Text hintType;

	public Text hintText;

	public TextAsset hintsCSV;

	private List<HintTip> listHints = new List<HintTip>();

	private int index;

	private float lastChange;

	[SerializeField]
	private float refreshTime = 5f;

	private void Awake()
	{
		ReadHints();
		Shuffle();
	}

	private void Start()
	{
		PickRandom();
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad > lastChange + refreshTime)
		{
			OnButtonClick(next: true);
		}
	}

	public void PickRandom()
	{
		if (listHints.Count > 0)
		{
			index = UnityEngine.Random.Range(0, listHints.Count);
			DisplayHint(index);
		}
	}

	public void Shuffle()
	{
		for (int i = 0; i < listHints.Count; i++)
		{
			int num = UnityEngine.Random.Range(i, listHints.Count);
			HintTip value = listHints[i];
			listHints[i] = listHints[num];
			listHints[num] = value;
		}
	}

	public void OnButtonClick(bool next)
	{
		index += (next ? 1 : (-1));
		if (index < 0)
		{
			index = listHints.Count - 1;
		}
		else if (index >= listHints.Count)
		{
			index = 0;
		}
		DisplayHint(index);
	}

	public void DisplayHint(int index)
	{
		hintType.text = listHints[index].type;
		hintText.text = listHints[index].text;
		lastChange = Time.timeSinceLevelLoad;
	}

	public void ReadHints()
	{
		string[] array = hintsCSV.text.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(';');
			index++;
			listHints.Add(new HintTip
			{
				id = index,
				type = array2[0],
				text = array2[1]
			});
		}
	}
}
