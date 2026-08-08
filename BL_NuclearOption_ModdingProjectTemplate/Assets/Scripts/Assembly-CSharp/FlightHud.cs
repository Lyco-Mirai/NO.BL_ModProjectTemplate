using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class FlightHud : SceneSingleton<FlightHud>
{
	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private Transform HUDCenter;

	[SerializeField]
	private RawImage compass;

	[SerializeField]
	private GameObject pitchCompassCenter;

	[SerializeField]
	private RawImage pitchCompass;

	public Transform statusAnchor;

	public Transform HMDCenter;

	public Image waterline;

	private Aircraft aircraft;

	private Transform cockpitTransform;

	private Rigidbody cockpitRB;

	private ControlInputs inputs;

	public Image virtualJoystickPos;

	[SerializeField]
	private Image virtualJoystickVector;

	public Image velocityVector;

	public static void ResetAircraft()
	{
	}

	public static void EnableCanvas(bool enable)
	{
		if (SceneSingleton<FlightHud>.i == null)
		{
			if (enable)
			{
				Debug.LogWarning("FlightHud enabled after it was destroyed");
			}
			return;
		}
		SceneSingleton<FlightHud>.i.canvas.gameObject.SetActive(enable);
		if (enable)
		{
			if (SceneSingleton<HeadMountedDisplay>.i != null)
			{
				SceneSingleton<HeadMountedDisplay>.i.RefreshSettings();
			}
			if (SceneSingleton<FlightHud>.i != null)
			{
				SceneSingleton<FlightHud>.i.RefreshSettings();
			}
			if (SceneSingleton<CombatHUD>.i != null)
			{
				SceneSingleton<CombatHUD>.i.RefreshSettings();
			}
			if (SceneSingleton<HUDAppManager>.i != null)
			{
				SceneSingleton<HUDAppManager>.i.RefreshSettings();
			}
			if (SceneSingleton<MFDAppManager>.i != null)
			{
				SceneSingleton<MFDAppManager>.i.RefreshSettings();
			}
		}
	}

	protected override void Awake()
	{
		base.Awake();
		virtualJoystickPos.transform.localPosition = Vector3.zero;
		PlayerSettings.OnApplyOptions += RefreshSettings;
	}

	public Transform GetHUDCenter()
	{
		return HUDCenter;
	}

	public void RefreshSettings()
	{
	}

	public void SetAircraft(Aircraft aircraft)
	{
		if (this.aircraft != aircraft && aircraft != null)
		{
			this.aircraft = aircraft;
			inputs = aircraft.GetInputs();
			cockpitRB = aircraft.cockpit.rb;
			cockpitTransform = aircraft.cockpit.transform;
		}
	}

	public void SetVirtualJoystick(Vector3 joystickPos)
	{
		virtualJoystickPos.transform.localPosition = joystickPos;
		float z = (0f - Mathf.Atan2(virtualJoystickPos.transform.localPosition.x, virtualJoystickPos.transform.localPosition.y)) * 57.29578f + 180f;
		virtualJoystickPos.transform.eulerAngles = new Vector3(0f, 0f, z);
		float magnitude = virtualJoystickPos.transform.localPosition.magnitude;
		virtualJoystickVector.transform.localScale = new Vector3(1f, magnitude, 1f) * (1f / virtualJoystickPos.transform.localScale.x);
		virtualJoystickPos.color = new Color(ThemeManager.Active.ColorTheme.AllClear.r, ThemeManager.Active.ColorTheme.AllClear.g, ThemeManager.Active.ColorTheme.AllClear.b, Mathf.Clamp01(magnitude * 0.01f));
	}

	private void Update()
	{
		if (!(cockpitRB != null))
		{
			return;
		}
		Vector3 vector = cockpitTransform.position + cockpitTransform.forward * 4000f;
		Vector3 vector2 = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(vector);
		if (Vector3.Dot(SceneSingleton<CameraStateManager>.i.transform.forward, vector - SceneSingleton<CameraStateManager>.i.transform.position) > 0f)
		{
			vector2 = Vector3.Scale(vector2, new Vector3(1f, 1f, 0f));
		}
		HUDCenter.transform.position = vector2;
		if (velocityVector.gameObject.activeSelf)
		{
			vector2 = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(cockpitTransform.position + cockpitRB.velocity * 1000f);
			if (Vector3.Dot(SceneSingleton<CameraStateManager>.i.transform.forward, cockpitRB.velocity) > 0f)
			{
				vector2 = Vector3.Scale(vector2, new Vector3(1f, 1f, 0f));
			}
			velocityVector.transform.position = vector2;
		}
		float magnitude = cockpitRB.velocity.magnitude;
		velocityVector.gameObject.SetActive(magnitude > 10f);
		HUDCenter.transform.eulerAngles = new Vector3(0f, 0f, 0f - (SceneSingleton<CameraStateManager>.i.mainCamera.transform.eulerAngles.z - cockpitTransform.eulerAngles.z));
		pitchCompassCenter.transform.eulerAngles = new Vector3(0f, 0f, 0f - SceneSingleton<CameraStateManager>.i.mainCamera.transform.eulerAngles.z);
		pitchCompass.transform.localScale = Vector3.one * (50f / SceneSingleton<CameraStateManager>.i.mainCamera.fieldOfView);
		pitchCompass.uvRect = new Rect(1f, (0f - cockpitRB.transform.eulerAngles.x) / 180f + 0.437f, 1f, 0.126f);
		compass.uvRect = new Rect((cockpitRB.transform.eulerAngles.y + 135f) / 360f, 0f, 0.25f, 1f);
	}

	private void OnDestroy()
	{
		PlayerSettings.OnApplyOptions -= RefreshSettings;
	}
}
