using NuclearOption;
using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReserveReport : SceneSingleton<ReserveReport>
{
	[SerializeField]
	public TMP_Text noticeText;

	[SerializeField]
	private Image aircraftImage;

	[SerializeField]
	private AudioClip reserveAwardedClip;

	[SerializeField]
	private AudioClip reserveAffordedClip;

	[SerializeField]
	private AudioClip reserveCancelledClip;

	private float hideTimer;

	protected override void Awake()
	{
		base.Awake();
		if (GameManager.gameState == GameState.Editor)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void Initialize(Player localPlayer)
	{
		localPlayer.onReserveNotice += ReserveReport_OnReserveNotice;
	}

	public void SetPosition(Vector3 screenPosition)
	{
		base.transform.position = screenPosition;
	}

	private void ReserveReport_OnReserveNotice(ReserveNotice reserveNotice)
	{
		if (!PlayerSettings.cinematicMode)
		{
			if (reserveNotice.outcome == ReserveEvent.cancelledAfford)
			{
				ShowDisplay(reserveNotice);
				FlashAffordAircraft();
			}
			else if (reserveNotice.outcome == ReserveEvent.granted)
			{
				ShowDisplay(reserveNotice);
				FlashGrantedAircraft();
			}
			else if (reserveNotice.outcome == ReserveEvent.cancelledRank)
			{
				ShowDisplay(reserveNotice);
				FlashReserveCancelled();
			}
		}
	}

	public void ShowDisplay(ReserveNotice reserveNotice)
	{
		base.gameObject.SetActive(value: true);
		base.enabled = true;
		hideTimer = 5f;
		aircraftImage.sprite = reserveNotice.aircraftDefinition.mapIcon;
	}

	public void FlashGrantedAircraft()
	{
		aircraftImage.color = Color.green;
		noticeText.color = Color.green;
		SoundManager.PlayInterfaceOneShot(reserveAwardedClip);
		noticeText.text = "+1";
	}

	public void FlashAffordAircraft()
	{
		aircraftImage.color = Color.white;
		noticeText.color = Color.white;
		SoundManager.PlayInterfaceOneShot(reserveAffordedClip);
		noticeText.text = "+$";
	}

	public void FlashReserveCancelled()
	{
		aircraftImage.color = Color.red;
		noticeText.color = Color.red;
		SoundManager.PlayInterfaceOneShot(reserveCancelledClip);
		noticeText.text = "x";
	}

	private void Update()
	{
		hideTimer -= Time.deltaTime;
		if (hideTimer < 0f)
		{
			base.enabled = false;
			base.gameObject.SetActive(value: false);
		}
	}
}
