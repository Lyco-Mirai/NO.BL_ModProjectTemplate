using System;
using System.Text.RegularExpressions;
using Rewired.Glyphs.UnityUI;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBox : MonoBehaviour
{
	[SerializeField]
	private GameObject holder;

	[SerializeField]
	private UnityUITextMeshProGlyphHelper titleText;

	[SerializeField]
	private UnityUITextMeshProGlyphHelper bodyText;

	[SerializeField]
	private UnityUITextMeshProGlyphHelper buttonText;

	[SerializeField]
	private Button button;

	private static readonly Regex bindTagRegex = new Regex("<bind=([A-Za-z0-9 _-]+)(?::([A-Za-z0-9_+-]+))?>");

	public int? CurrentId { get; private set; }

	public event Action<int> ButtonPressed;

	private static string ReplaceBindTags(string text)
	{
		return bindTagRegex.Replace(text, delegate(Match match)
		{
			string value = match.Groups[1].Value;
			string value2 = match.Groups[2].Value;
			string text2 = (string.IsNullOrEmpty(value2) ? "" : (" actionRange=\"" + value2 + "\""));
			return "<rewiredElement playerId=0 actionName=\"" + value + "\"" + text2 + " type=\"glyphOrText\">";
		});
	}

	private void Awake()
	{
		button.onClick.AddListener(InvokeButtonPress);
	}

	private void Update()
	{
		if (GameManager.playerInput.GetButtonUp("Select"))
		{
			InvokeButtonPress();
		}
	}

	private void InvokeButtonPress()
	{
		if (CurrentId.HasValue)
		{
			button.interactable = false;
			this.ButtonPressed(CurrentId.Value);
		}
		else
		{
			Debug.LogWarning("DialogueBox has no Id so button will do nothing");
		}
	}

	public void Show(int id, string title, string body, string button)
	{
		CurrentId = id;
		if (string.IsNullOrEmpty(title))
		{
			title = "Dialogue";
		}
		titleText.text = ReplaceBindTags(title);
		bodyText.text = ReplaceBindTags(body);
		buttonText.text = ReplaceBindTags(button);
		this.button.interactable = true;
		this.button.Select();
		EnableBox(show: true);
	}

	public void Hide()
	{
		CurrentId = null;
		titleText.text = "";
		EnableBox(show: false);
	}

	private void EnableBox(bool show)
	{
		holder.SetActive(show);
		GameplayUI.AllowPauseKeybind = !show;
		GameManager.flightControlsEnabled = !show;
		CursorManager.SetFlag(CursorFlags.Dialogue, show);
		if (GameManager.gameState == GameState.SinglePlayer)
		{
			TimeScaleManager.Scale = (show ? 0f : (GameplayUI.GameSlowMotion ? 0.05f : 1f));
			AudioListener.pause = show;
		}
	}
}
