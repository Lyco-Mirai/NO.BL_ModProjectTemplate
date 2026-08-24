using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AircraftActionsReport : SceneSingleton<AircraftActionsReport>
{
	[SerializeField]
	private Image background;

	[SerializeField]
	private TextMeshProUGUI actionsText;

	private Aircraft aircraft;

	private int messageLines;

	public void ReportText(string report, float displayTime)
	{
		if (!(actionsText == null))
		{
			if (actionsText.text.Length > 0)
			{
				actionsText.text += "\n";
			}
			actionsText.text += report;
			background.enabled = true;
			TrimMessages(displayTime).Forget();
		}
	}

	public void Initialize(Aircraft aircraft)
	{
		if (SceneSingleton<AircraftActionsReport>.i == null)
		{
			SetupSingleton();
		}
		this.aircraft = aircraft;
		aircraft.onDisableUnit += AircraftActionsReport_OnDisable;
		ClearMessages();
	}

	private void AircraftActionsReport_OnDisable(Unit unit)
	{
		aircraft.onDisableUnit -= AircraftActionsReport_OnDisable;
		if (this != null)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void ClearMessages()
	{
		actionsText.text = "";
		messageLines = 0;
		background.enabled = false;
	}

	private async UniTask TrimMessages(float messageTime)
	{
		background.enabled = true;
		messageLines++;
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)(messageTime * 1000f));
		if (!cancel.IsCancellationRequested)
		{
			if (messageLines > 1)
			{
				int num = actionsText.text.IndexOf("\n");
				string text = actionsText.text.Substring(num + 1);
				actionsText.text = text;
			}
			else
			{
				actionsText.text = string.Empty;
			}
			messageLines--;
			if (messageLines <= 0)
			{
				background.enabled = false;
			}
		}
	}
}
