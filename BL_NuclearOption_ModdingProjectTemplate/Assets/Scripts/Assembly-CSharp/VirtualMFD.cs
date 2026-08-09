using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VirtualMFD : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI speed;

	[SerializeField]
	private TextMeshProUGUI altitude;

	[SerializeField]
	private TMP_Text missionTime;

	[SerializeField]
	private Image attitude;

	[SerializeField]
	private Image horizon;

	[SerializeField]
	private List<Button> leftButtons;

	[SerializeField]
	private List<Button> rightButtons;

	[SerializeField]
	private List<MFDScreen> leftScreens = new List<MFDScreen>();

	[SerializeField]
	private List<MFDScreen> rightScreens = new List<MFDScreen>();

	[SerializeField]
	private Vector3 showPos;

	[SerializeField]
	private Vector3 hidePos;

	private MFDScreen activeLeft;

	private MFDScreen activeRight;

	private void Start()
	{
		if (GameManager.gameState == GameState.Editor)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		showPos = Vector3.zero;
		hidePos = Screen.width * Vector3.right;
		SceneSingleton<DynamicMap>.i.onMapMaximized += VirtualMFD_onMapMaximized;
		SceneSingleton<DynamicMap>.i.onMapMinimized += VirtualMFD_onMapMinimized;
		SetupButtons();
		ToggleAllButtons(show: false);
		HideAllLeftScreens();
		HideAllRightScreens();
	}

	private void Update()
	{
		if (DynamicMap.mapMaximized)
		{
			if (SceneSingleton<CombatHUD>.i.aircraft != null)
			{
				if (missionTime.isActiveAndEnabled)
				{
					missionTime.gameObject.SetActive(value: false);
					missionTime.transform.parent.gameObject.SetActive(value: false);
				}
				if (!speed.isActiveAndEnabled)
				{
					speed.gameObject.SetActive(value: true);
				}
				speed.text = UnitConverter.SpeedReading(SceneSingleton<CombatHUD>.i.aircraft.speed);
				if (!altitude.isActiveAndEnabled)
				{
					altitude.gameObject.SetActive(value: true);
				}
				altitude.text = UnitConverter.AltitudeReading(SceneSingleton<CombatHUD>.i.aircraft.radarAlt);
				if (!attitude.isActiveAndEnabled)
				{
					attitude.gameObject.SetActive(value: true);
				}
				horizon.transform.eulerAngles = new Vector3(0f, 0f, 0f - SceneSingleton<CombatHUD>.i.aircraft.transform.eulerAngles.z);
				float num = SceneSingleton<CombatHUD>.i.aircraft.transform.eulerAngles.x;
				if (num > 180f)
				{
					num -= 360f;
				}
				horizon.fillAmount = Mathf.Clamp(0.5f + num / 180f, 0f, 1f);
				horizon.transform.localPosition = Mathf.Clamp(num, -15f, 15f) * horizon.transform.up;
			}
			else
			{
				if (!missionTime.isActiveAndEnabled)
				{
					missionTime.gameObject.SetActive(value: true);
					missionTime.transform.parent.gameObject.SetActive(value: true);
				}
				if (speed.isActiveAndEnabled)
				{
					speed.gameObject.SetActive(value: false);
				}
				if (altitude.isActiveAndEnabled)
				{
					altitude.gameObject.SetActive(value: false);
				}
				if (attitude.isActiveAndEnabled)
				{
					attitude.gameObject.SetActive(value: false);
				}
				missionTime.text = UnitConverter.TimeOfDay(NetworkSceneSingleton<MissionManager>.i.MissionTime / 3600f, includeSeconds: true);
			}
		}
		else
		{
			if (missionTime.isActiveAndEnabled)
			{
				missionTime.gameObject.SetActive(value: false);
				missionTime.transform.parent.gameObject.SetActive(value: false);
			}
			if (speed.isActiveAndEnabled)
			{
				speed.gameObject.SetActive(value: false);
			}
			if (altitude.isActiveAndEnabled)
			{
				altitude.gameObject.SetActive(value: false);
			}
			if (attitude.isActiveAndEnabled)
			{
				attitude.gameObject.SetActive(value: false);
			}
		}
	}

	public void SetupButtons()
	{
		foreach (Button leftButton in leftButtons)
		{
			int num = leftButtons.IndexOf(leftButton);
			if (num < leftScreens.Count && leftScreens[num] != null)
			{
				leftScreens[num].Setup(this, leftScreens[num].shortName);
			}
			else
			{
				leftButton.enabled = false;
			}
		}
		foreach (Button rightButton in rightButtons)
		{
			int num2 = rightButtons.IndexOf(rightButton);
			if (num2 < rightScreens.Count && rightScreens[num2] != null)
			{
				rightScreens[num2].Setup(this, rightScreens[num2].shortName);
			}
			else
			{
				rightButton.enabled = false;
			}
		}
	}

	public void VirtualMFD_onMapMaximized()
	{
		ToggleAllButtons(show: true);
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			speed.gameObject.SetActive(value: true);
			altitude.gameObject.SetActive(value: true);
			attitude.gameObject.SetActive(value: true);
		}
		if (activeLeft != null)
		{
			activeLeft.ShowScreen(-showPos);
		}
		if (activeRight != null)
		{
			activeRight.ShowScreen(showPos);
		}
	}

	public void VirtualMFD_onMapMinimized()
	{
		ToggleAllButtons(show: false);
		HideAllLeftScreens();
		HideAllRightScreens();
		speed.gameObject.SetActive(value: false);
		altitude.gameObject.SetActive(value: false);
		attitude.gameObject.SetActive(value: false);
	}

	public void PressLeftButton(Button button)
	{
		int index = leftButtons.IndexOf(button);
		MFDScreen mFDScreen = leftScreens[index];
		if (mFDScreen != null && (!(SceneSingleton<CombatHUD>.i.aircraft == null) || !mFDScreen.aircraftOnly))
		{
			if (mFDScreen.isActive)
			{
				mFDScreen.CloseScreen(-hidePos);
				activeLeft = null;
			}
			else if (!mFDScreen.isActive)
			{
				HideAllLeftScreens();
				mFDScreen.ShowScreen(-showPos);
				activeLeft = leftScreens[index];
			}
		}
	}

	public void PressRightButton(Button button)
	{
		int index = rightButtons.IndexOf(button);
		MFDScreen mFDScreen = rightScreens[index];
		if (mFDScreen != null && (!(SceneSingleton<CombatHUD>.i.aircraft == null) || !mFDScreen.aircraftOnly))
		{
			if (mFDScreen.isActive)
			{
				mFDScreen.CloseScreen(hidePos);
				activeRight = null;
			}
			else if (!mFDScreen.isActive)
			{
				HideAllRightScreens();
				mFDScreen.ShowScreen(showPos);
				activeRight = rightScreens[index];
			}
		}
	}

	public void HideAllLeftScreens()
	{
		foreach (MFDScreen leftScreen in leftScreens)
		{
			if (leftScreen != null)
			{
				leftScreen.CloseScreen(-hidePos);
			}
		}
	}

	public void HideAllRightScreens()
	{
		foreach (MFDScreen rightScreen in rightScreens)
		{
			if (rightScreen != null)
			{
				rightScreen.CloseScreen(hidePos);
			}
		}
	}

	public void ToggleAllButtons(bool show)
	{
		foreach (Button leftButton in leftButtons)
		{
			leftButton.gameObject.SetActive(show);
		}
		foreach (Button rightButton in rightButtons)
		{
			rightButton.gameObject.SetActive(show);
		}
	}
}
