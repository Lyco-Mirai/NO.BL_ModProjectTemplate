using System;
using UnityEngine;
using UnityEngine.UI;

public class HeadMountedDisplay : SceneSingleton<HeadMountedDisplay>
{
	[SerializeField]
	private Image bearing_img;

	[SerializeField]
	private HUDApp horizon;

	[SerializeField]
	private HUDApp altitude;

	[SerializeField]
	private HUDApp speed;

	[SerializeField]
	private HUDApp bearing;

	private Aircraft aircraftPrev;

	[SerializeField]
	private float posX = 350f;

	[SerializeField]
	private float posY = 300f;

	[SerializeField]
	private float topY = 300f;

	[SerializeField]
	private float sizeX = 1920f;

	[SerializeField]
	private float sizeY = 1080f;

	private float hideDistance = 600f;

	private void Start()
	{
		aircraftPrev = SceneSingleton<CombatHUD>.i.aircraft;
		horizon.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
		altitude.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
		speed.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
		bearing.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
		PlayerSettings.OnApplyOptions += RefreshSettings;
		RefreshSettings();
	}

	public void RefreshSettings()
	{
		sizeX = PlayerSettings.hmdWidth;
		sizeY = PlayerSettings.hmdHeight;
		hideDistance = PlayerSettings.hmdHideDist * 0.5f * (sizeX + sizeY);
		posX = PlayerSettings.hmdSideDist * Mathf.Cos(MathF.PI / 180f * PlayerSettings.hmdSideAngle);
		posY = PlayerSettings.hmdSideDist * Mathf.Sin(MathF.PI / 180f * PlayerSettings.hmdSideAngle);
		topY = PlayerSettings.hmdTopHeight;
		GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX, sizeY);
		altitude.transform.localPosition = new Vector2(posX, posY);
		speed.transform.localPosition = new Vector2(0f - posX, posY);
		bearing.transform.localPosition = new Vector2(0f, topY + 50f);
		horizon.transform.localPosition = new Vector2(0f, topY);
		horizon.RefreshSettings();
		altitude.RefreshSettings();
		speed.RefreshSettings();
		bearing.RefreshSettings();
	}

	private void Update()
	{
		if (SceneSingleton<CombatHUD>.i.aircraft == null)
		{
			return;
		}
		if (SceneSingleton<CombatHUD>.i.aircraft != aircraftPrev)
		{
			bearing.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
			horizon.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
			altitude.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
			speed.Initialize(SceneSingleton<CombatHUD>.i.aircraft);
			aircraftPrev = SceneSingleton<CombatHUD>.i.aircraft;
		}
		Vector3 position = SceneSingleton<FlightHud>.i.GetHUDCenter().position;
		bool flag = FastMath.InRange(position, altitude.transform.position, hideDistance);
		bool flag2 = FastMath.InRange(position, speed.transform.position, hideDistance);
		bool flag3 = FastMath.InRange(position, bearing.transform.position, hideDistance);
		bool num = FastMath.InRange(position, horizon.transform.position, hideDistance);
		horizon.Refresh();
		altitude.Refresh();
		speed.Refresh();
		bearing.Refresh();
		if (flag)
		{
			if (altitude.gameObject.activeSelf)
			{
				altitude.gameObject.SetActive(value: false);
			}
		}
		else if (!altitude.gameObject.activeSelf)
		{
			altitude.gameObject.SetActive(value: true);
		}
		if (flag2)
		{
			if (speed.gameObject.activeSelf)
			{
				speed.gameObject.SetActive(value: false);
			}
		}
		else if (!speed.gameObject.activeSelf)
		{
			speed.gameObject.SetActive(value: true);
		}
		if (flag3)
		{
			if (bearing.gameObject.activeSelf)
			{
				bearing.gameObject.SetActive(value: false);
			}
		}
		else if (!bearing.gameObject.activeSelf)
		{
			bearing.gameObject.SetActive(value: true);
		}
		if (num)
		{
			if (horizon.gameObject.activeSelf)
			{
				horizon.gameObject.SetActive(value: false);
			}
		}
		else if (!horizon.gameObject.activeSelf)
		{
			horizon.gameObject.SetActive(value: true);
		}
	}

	private void OnDestroy()
	{
		PlayerSettings.OnApplyOptions -= RefreshSettings;
	}
}
