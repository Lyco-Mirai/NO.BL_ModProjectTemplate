using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuMain : SceneSingleton<RadialMenuMain>
{
	public enum RadialMenuType
	{
		Closed = 0,
		Main = 1,
		Weapons = 2
	}

	[SerializeField]
	private RadialMenuAction[] actionsMain;

	[SerializeField]
	private RadialMenuAction[] actionsWeapons;

	private List<RadialMenuAction> allowedActionsMain = new List<RadialMenuAction>();

	private List<RadialMenuAction> allowedActionsWeapons = new List<RadialMenuAction>();

	[SerializeField]
	private RadialMenuAction actionWeaponPrefab;

	[SerializeField]
	private GameObject actionPrefab;

	private RadialMenuAction selectedAction;

	private Player playerInput;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private GameObject menuObject;

	[SerializeField]
	private GameObject actionsMainContainer;

	[SerializeField]
	private GameObject actionsWeaponsContainer;

	[SerializeField]
	private GameObject sectorObject;

	[SerializeField]
	private TMP_Text selectedActionText;

	[SerializeField]
	private CanvasGroup canvasGroup;

	private float lastOpen;

	private float lastSelection;

	private float degreesPerActionMain;

	private float degreesPerActionWeapons;

	private bool showWeaponWheel;

	[SerializeField]
	private List<GameObject> actionObjectsMain = new List<GameObject>();

	[SerializeField]
	private List<GameObject> actionObjectsWeapons = new List<GameObject>();

	private RadialMenuType currentState;

	private int currentIndex = -1;

	private Vector3 mousePos;

	private Vector3 mouseDelta;

	public void CloseMenu()
	{
		selectedAction = null;
		currentState = RadialMenuType.Closed;
		currentIndex = -1;
		selectedActionText.text = "";
	}

	public static bool IsInUse()
	{
		return SceneSingleton<RadialMenuMain>.i.menuObject.activeSelf;
	}

	public void OpenMenu()
	{
		GameManager.GetLocalAircraft(out var localAircraft);
		if (!(localAircraft == null))
		{
			if (localAircraft != aircraft)
			{
				aircraft = localAircraft;
				SetupMain();
				SetupWeapons();
			}
			RefreshWeapons();
			lastOpen = Time.realtimeSinceStartup;
			mousePos = Input.mousePosition;
		}
	}

	private void OnEnable()
	{
		playerInput = ReInput.players.GetPlayer(0);
		CloseMenu();
	}

	private void SetupMain()
	{
		allowedActionsMain.Clear();
		foreach (GameObject item in actionObjectsMain)
		{
			UnityEngine.Object.Destroy(item);
		}
		actionObjectsMain.Clear();
		RadialMenuAction[] array = actionsMain;
		foreach (RadialMenuAction radialMenuAction in array)
		{
			if (radialMenuAction.AllowedOnAircraft(aircraft))
			{
				allowedActionsMain.Add(radialMenuAction);
			}
		}
		degreesPerActionMain = 360f / (float)allowedActionsMain.Count;
		for (int j = 0; j < allowedActionsMain.Count; j++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(sectorObject, actionsMainContainer.transform);
			Image component = gameObject.GetComponent<Image>();
			component.fillAmount = degreesPerActionMain / 360f;
			gameObject.transform.localEulerAngles = new Vector3(0f, 0f, (0f - ((float)j - 0.5f)) * degreesPerActionMain);
			_ = allowedActionsMain[j];
			GameObject gameObject2 = UnityEngine.Object.Instantiate(actionPrefab, actionsMainContainer.transform);
			Image component2 = gameObject2.GetComponent<Image>();
			Text component3 = gameObject2.transform.Find("Text").GetComponent<Text>();
			allowedActionsMain[j].Setup(component, component2, component3);
			gameObject2.transform.localPosition = 90f * new Vector3(Mathf.Sin((float)j * degreesPerActionMain * (MathF.PI / 180f)), Mathf.Cos((float)j * degreesPerActionMain * (MathF.PI / 180f)), 0f);
			actionObjectsMain.Add(gameObject2);
			actionObjectsMain.Add(gameObject);
		}
	}

	private void SetupWeapons()
	{
		showWeaponWheel = false;
		allowedActionsWeapons.Clear();
		foreach (GameObject actionObjectsWeapon in actionObjectsWeapons)
		{
			UnityEngine.Object.Destroy(actionObjectsWeapon);
		}
		actionObjectsWeapons.Clear();
		RadialMenuAction[] array = actionsWeapons;
		foreach (RadialMenuAction radialMenuAction in array)
		{
			if (radialMenuAction.AllowedOnAircraft(aircraft))
			{
				allowedActionsWeapons.Add(radialMenuAction);
				showWeaponWheel = true;
			}
		}
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			RadialMenuAction radialMenuAction2 = UnityEngine.Object.Instantiate(actionWeaponPrefab);
			radialMenuAction2.SetWeapon(weaponStation.WeaponInfo, weaponStation.Number);
			allowedActionsWeapons.Add(radialMenuAction2);
		}
		if (allowedActionsWeapons.Count > 2)
		{
			showWeaponWheel = true;
		}
		degreesPerActionWeapons = 360f / (float)allowedActionsWeapons.Count;
		for (int j = 0; j < allowedActionsWeapons.Count; j++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(sectorObject, actionsWeaponsContainer.transform);
			Image component = gameObject.GetComponent<Image>();
			component.fillAmount = degreesPerActionWeapons / 360f;
			gameObject.transform.localEulerAngles = new Vector3(0f, 0f, (0f - ((float)j - 0.5f)) * degreesPerActionWeapons);
			_ = allowedActionsWeapons[j];
			GameObject gameObject2 = UnityEngine.Object.Instantiate(actionPrefab, actionsWeaponsContainer.transform);
			Image component2 = gameObject2.GetComponent<Image>();
			Text component3 = gameObject2.transform.Find("Text").GetComponent<Text>();
			allowedActionsWeapons[j].Setup(component, component2, component3);
			gameObject2.transform.localPosition = 90f * new Vector3(Mathf.Sin((float)j * degreesPerActionWeapons * (MathF.PI / 180f)), Mathf.Cos((float)j * degreesPerActionWeapons * (MathF.PI / 180f)), 0f);
			actionObjectsWeapons.Add(gameObject2);
			actionObjectsWeapons.Add(gameObject);
		}
	}

	public void RefreshWeapons()
	{
		for (int i = 0; i < allowedActionsWeapons.Count; i++)
		{
			RadialMenuAction radialMenuAction = allowedActionsWeapons[i];
			if (radialMenuAction.GetActionType() == RadialMenuAction.ActionType.SelectWeapon && !aircraft.weaponStations[radialMenuAction.weapon_number].WeaponInfo.sling && !aircraft.weaponStations[radialMenuAction.weapon_number].WeaponInfo.energy)
			{
				radialMenuAction.RefreshWeapon(aircraft.weaponStations[radialMenuAction.weapon_number].GetAmmoReadout(), aircraft.weaponStations[radialMenuAction.weapon_number].GetAmmoLevel());
			}
		}
	}

	private void Update()
	{
		if (currentState != RadialMenuType.Closed)
		{
			if (Time.realtimeSinceStartup - lastOpen > 0.05f && canvasGroup.alpha < 1f)
			{
				canvasGroup.alpha += 10f * Time.deltaTime;
			}
			if (!menuObject.activeSelf)
			{
				menuObject.SetActive(value: true);
			}
			switch (currentState)
			{
			case RadialMenuType.Main:
				if (!actionsMainContainer.activeSelf)
				{
					actionsMainContainer.SetActive(value: true);
				}
				if (actionsWeaponsContainer.activeSelf)
				{
					actionsWeaponsContainer.SetActive(value: false);
				}
				CheckAction(allowedActionsMain, degreesPerActionMain, "Radial Menu");
				break;
			case RadialMenuType.Weapons:
				if (actionsMainContainer.activeSelf)
				{
					actionsMainContainer.SetActive(value: false);
				}
				if (!actionsWeaponsContainer.activeSelf)
				{
					actionsWeaponsContainer.SetActive(value: true);
				}
				CheckAction(allowedActionsWeapons, degreesPerActionWeapons, "Weapon Wheel");
				break;
			}
		}
		else
		{
			if (playerInput.GetButtonTimedPressDown("Radial Menu", PlayerSettings.pressDelay))
			{
				currentState = RadialMenuType.Main;
			}
			else if (playerInput.GetButtonTimedPressDown("Weapon Wheel", PlayerSettings.pressDelay))
			{
				currentState = RadialMenuType.Weapons;
			}
			if (currentState != RadialMenuType.Closed)
			{
				OpenMenu();
			}
			if (currentState == RadialMenuType.Weapons && !showWeaponWheel)
			{
				currentState = RadialMenuType.Closed;
			}
			if (canvasGroup.alpha > 0f)
			{
				canvasGroup.alpha -= 5f * Time.deltaTime;
			}
			else if (menuObject.activeSelf)
			{
				menuObject.SetActive(value: false);
			}
		}
	}

	private async UniTask FlashSelection(RadialMenuAction action)
	{
		float time = 0f;
		CancellationToken cancel = base.destroyCancellationToken;
		while (time < 2f)
		{
			time += Time.deltaTime;
			action.Flash();
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	private void CheckAction(List<RadialMenuAction> allowedActions, float degreesPerAction, string buttonName)
	{
		if (aircraft == null)
		{
			CloseMenu();
		}
		else if (PlayerSettings.radialControl < 2)
		{
			ControlAxis(allowedActions, degreesPerAction, buttonName);
		}
		else
		{
			ControlButtons(allowedActions, degreesPerAction, buttonName);
		}
	}

	private void ControlAxis(List<RadialMenuAction> allowedActions, float degreesPerAction, string buttonName)
	{
		if (!playerInput.GetButton(buttonName))
		{
			if (selectedAction != null)
			{
				selectedAction.UnHover();
				selectedAction.TriggerAction(aircraft);
				FlashSelection(selectedAction).Forget();
			}
			CloseMenu();
			return;
		}
		mouseDelta += (playerInput.GetAxis("Pan View") * Vector3.right - playerInput.GetAxis("Tilt View") * Vector3.up) * 0.5f;
		mouseDelta = Vector3.Lerp(mouseDelta, Vector3.zero, 0.05f);
		float x = ((PlayerSettings.radialControl == 0) ? playerInput.GetAxis("Radial Menu Horizontal") : mouseDelta.x);
		float y = ((PlayerSettings.radialControl == 0) ? (0f - playerInput.GetAxis("Radial Menu Vertical")) : mouseDelta.y);
		Vector2 vector = new Vector2(x, y);
		if (vector.sqrMagnitude > 0.1f)
		{
			lastSelection = Time.realtimeSinceStartup;
			float num = 0f - Vector2.SignedAngle(Vector2.up, vector.normalized);
			if (num < 0f)
			{
				num += 360f;
			}
			num = Mathf.Repeat(num + degreesPerAction * 0.5f, 360f);
			int value = Mathf.FloorToInt(num / degreesPerAction);
			value = Mathf.Clamp(value, 0, allowedActions.Count - 1);
			RadialMenuAction radialMenuAction = allowedActions[value];
			if (radialMenuAction != selectedAction)
			{
				if (selectedAction != null)
				{
					selectedAction.UnHover();
				}
				selectedAction = radialMenuAction;
				selectedAction.Hover();
				selectedActionText.text = selectedAction.DisplayName;
			}
		}
		else if (Time.realtimeSinceStartup - lastSelection > 0.05f && selectedAction != null)
		{
			selectedAction.UnHover();
			selectedAction = null;
			selectedActionText.text = "";
		}
	}

	private void ControlButtons(List<RadialMenuAction> allowedActions, float degreesPerAction, string buttonName)
	{
		if (!playerInput.GetButton(buttonName))
		{
			if (selectedAction != null)
			{
				selectedAction.UnHover();
				selectedAction.TriggerAction(aircraft);
				FlashSelection(selectedAction).Forget();
			}
			CloseMenu();
			return;
		}
		bool buttonDown = playerInput.GetButtonDown("Radial Menu Horizontal");
		bool negativeButtonDown = playerInput.GetNegativeButtonDown("Radial Menu Horizontal");
		bool buttonDown2 = playerInput.GetButtonDown("Radial Menu Vertical");
		bool negativeButtonDown2 = playerInput.GetNegativeButtonDown("Radial Menu Vertical");
		if (buttonDown || negativeButtonDown || buttonDown2 || negativeButtonDown2)
		{
			lastSelection = Time.realtimeSinceStartup;
			if (currentIndex < 0)
			{
				if (buttonDown2)
				{
					currentIndex = 0;
				}
				else if (negativeButtonDown2)
				{
					currentIndex = Mathf.RoundToInt(0.5f * (float)allowedActions.Count);
				}
				if (buttonDown)
				{
					currentIndex = Mathf.RoundToInt(0.25f * (float)allowedActions.Count);
				}
				else if (negativeButtonDown)
				{
					currentIndex = Mathf.RoundToInt(0.75f * (float)allowedActions.Count);
				}
			}
			float num = (float)currentIndex * degreesPerAction;
			if (num > 225f && num < 315f)
			{
				if (buttonDown2)
				{
					currentIndex++;
				}
				else if (negativeButtonDown2)
				{
					currentIndex--;
				}
			}
			else if (num > 135f)
			{
				if (buttonDown)
				{
					currentIndex--;
				}
				else if (negativeButtonDown)
				{
					currentIndex++;
				}
			}
			else if (num > 45f)
			{
				if (buttonDown2)
				{
					currentIndex--;
				}
				else if (negativeButtonDown2)
				{
					currentIndex++;
				}
			}
			else if (buttonDown)
			{
				currentIndex++;
			}
			else if (negativeButtonDown)
			{
				currentIndex--;
			}
			if (currentIndex < 0)
			{
				currentIndex = allowedActions.Count - 1;
			}
			else if (currentIndex > allowedActions.Count - 1)
			{
				currentIndex = 0;
			}
			RadialMenuAction radialMenuAction = allowedActions[currentIndex];
			if (radialMenuAction != selectedAction)
			{
				if (selectedAction != null)
				{
					selectedAction.UnHover();
				}
				selectedAction = radialMenuAction;
				selectedAction.Hover();
				selectedActionText.text = selectedAction.DisplayName;
			}
		}
		else if (Time.realtimeSinceStartup - lastSelection > 0.5f && selectedAction != null)
		{
			currentIndex = -1;
			selectedAction.UnHover();
			selectedAction = null;
			selectedActionText.text = "";
		}
	}

	private void OnDestroy()
	{
		CloseMenu();
		aircraft = null;
		actionObjectsMain.Clear();
		actionObjectsWeapons.Clear();
	}
}
