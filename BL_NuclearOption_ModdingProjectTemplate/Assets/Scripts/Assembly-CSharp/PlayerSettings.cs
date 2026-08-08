using System;
using GameHelper;
using NuclearOption.Effects;
using NuclearOption.Networking;
using UnityEngine;
using BL.NO_Patches;

public enum HeadTrackerType
{
	TrackIR,
	None
}

public static class PlayerSettings
{
	public enum QualityLevels
	{
		NoShadows = 0,
		Low = 1,
		Medium = 2,
		High = 3
	}

	public enum UnitSystem
	{
		Metric = 0,
		Imperial = 1
	}

	public enum KillFeedFilter
	{
		None = 0,
		Player = 1,
		Friendly = 2,
		Enemy = 3,
		All = 4
	}

	public static UnitSystem unitSystem = UnitSystem.Metric;

	public static string playerName;

	public static float cockpitCamInertia = 0.5f;

	public static float defaultFoV = 50f;

	public static float defaultExternalFoV = 50f;

	public static bool zoomOnBoresight = false;

	public static bool padLockTarget = true;

	public static bool tacScreenIR = false;

	public static bool cameraAutoNVG = true;

	public static bool showAicraftDeployedMessage = true;

	public static bool lagPip = false;

	public static bool rangeCircle = false;

	public static bool gauges = true;

	public static int hudTime = 0;

	public static bool hudWeapons = false;

	public static float hmdWidth = 1920f;

	public static float hmdHeight = 1080f;

	public static float hmdTopHeight = 300f;

	public static float hmdSideDist = 350f;

	public static float hmdSideAngle = 45f;

	public static float hmdHideDist = 0.33f;

	public static float hmdIconSize = 30f;

	public static float hudTextSize = 40f;

	public static float hmdTextSize = 40f;

	public static float overlayTextSize = 32f;

	public static int hudColorR = 0;

	public static int hudColorG = 255;

	public static int hudColorB = 0;

	public static string playerName_Unsanitized;

	public static int landingCam = 1;

	public static int radialControl = 0;

	public static bool virtualJoystickEnabled = true;

	public static bool virtualJoystickInvertPitch = true;

	public static bool viewInvertPitch = false;

	public static bool invertCollective = false;

	public static float virtualJoystickSensitivity = 0.25f;

	public static float virtualJoystickCentering = 0f;

	public static float viewSensitivity = 0.5f;

	public static float viewSmoothing = 0.5f;

	public static float pressDelay = 0.15f;

	public static float clickDelay = 0.25f;

	public static bool useCompositeYaw;

	public static bool throttleUseNegative = true;

	public static bool throttleUseRelative = false;

	public static bool controllerMenuNavigation = true;

	public static bool menuWeaponSafety = true;

	public static bool useTrackIR;

	public static HeadTrackerType headTrackerType = HeadTrackerType.TrackIR;

	public static float tobiiEyeVsHeadRatio = 1f;

	public static float tobiiEyeTrackingResponsiveness = 1f;

	public static float tobiiHeadSensitivityPitchYaw = 1f;

	public static float tobiiCenterStabilization = 1f;

	public static float tobiiHeadSensitivityRoll = 1f;

	public static float tobiiHeadSensitivityPosition = 1f;

	public static bool tobiiHeadTrackingAutoCenter = true;

	public static GraphicsHelper graphics;

	public static bool debugVis = false;

	public static bool controlFiltersTuning = false;

	public static bool cinematicMode = false;

	private static ScreenResolutionHelper _screenResolutionHelper;

	public static DetailSettings DetailSettings;

	public static string themeGroupId = "vanilla";

	public static bool themeGroupEditorAdvancedMode = false;

	public static bool chatEnabled;

	public static bool chatFilter;

	public static bool chatTts;

	public static int chatTtsSpeed;

	public static int chatTtsVolume;

	public static PlayerNameDisplayScope playerIndexDisplay = PlayerNameDisplayScope.Off;

	public static PlayerNameDisplayScope serverTagDisplay = PlayerNameDisplayScope.Off;

	public static bool steamRichPresenceEnabled = true;

	public static bool discordRichPresenceEnabled = true;

	public static KillFeedFilter killFeedMunition = KillFeedFilter.All;

	public static KillFeedFilter killFeedAircraft = KillFeedFilter.All;

	public static KillFeedFilter killFeedBuilding = KillFeedFilter.All;

	public static KillFeedFilter killFeedShip = KillFeedFilter.All;

	public static KillFeedFilter killFeedVehicle = KillFeedFilter.All;

	public static float killFeedMinValue = 0f;

	public static int killFeedNbLines = 2;

	public static bool showHitMarkers = true;

	public static ScreenResolutionHelper screenResolutionHelper => _screenResolutionHelper ?? (_screenResolutionHelper = new ScreenResolutionHelper());

	public static event Action OnApplyOptions;

	public static void FirstInit(GraphicsHelperSO graphicsSettings)
	{
		graphics = new GraphicsHelper(graphicsSettings);
		DetailSettings = new DetailSettings();
		if (!GameManager.IsHeadless)
		{
			ColorLog<QualityLevels>.Info($"BeforeInit QualityLevel:{(QualityLevels)QualitySettings.GetQualityLevel()}, LOD:{QualitySettings.lodBias}");
			QualitySettings.SetQualityLevel(3, applyExpensiveChanges: true);
			ColorLog<QualityLevels>.Info($"QualityLevel:{(QualityLevels)QualitySettings.GetQualityLevel()}, LOD:{QualitySettings.lodBias}");
		}
	}

	public static void LoadPrefs()
	{
		if (PlayerPrefs.HasKey("UnitSystem"))
		{
			unitSystem = (UnitSystem)PlayerPrefs.GetInt("UnitSystem");
		}
		if (PlayerPrefs.HasKey("CockpitCamInertia"))
		{
			cockpitCamInertia = PlayerPrefs.GetFloat("CockpitCamInertia");
		}
		if (PlayerPrefs.HasKey("DefaultFoV"))
		{
			defaultFoV = PlayerPrefs.GetFloat("DefaultFoV");
		}
		if (PlayerPrefs.HasKey("DefaultExternalFoV"))
		{
			defaultExternalFoV = PlayerPrefs.GetFloat("DefaultExternalFoV");
		}
		if (PlayerPrefs.HasKey("ZoomOnBoresight"))
		{
			zoomOnBoresight = PlayerPrefs.GetInt("ZoomOnBoresight", 0) == 1;
		}
		if (PlayerPrefs.HasKey("PadLockTarget"))
		{
			padLockTarget = PlayerPrefs.GetInt("PadLockTarget", 0) == 1;
		}
		if (PlayerPrefs.HasKey("TacScreenIR"))
		{
			tacScreenIR = PlayerPrefs.GetInt("TacScreenIR", 0) == 1;
		}
		if (PlayerPrefs.HasKey("CameraAutoNVG"))
		{
			cameraAutoNVG = PlayerPrefs.GetInt("CameraAutoNVG", 0) == 1;
		}
		if (PlayerPrefs.HasKey("LagPip"))
		{
			lagPip = PlayerPrefs.GetInt("LagPip", 0) == 1;
		}
		if (PlayerPrefs.HasKey("RangeCircle"))
		{
			rangeCircle = PlayerPrefs.GetInt("RangeCircle", 0) == 1;
		}
		if (PlayerPrefs.HasKey("Gauges"))
		{
			gauges = PlayerPrefs.GetInt("Gauges", 1) == 1;
		}
		if (PlayerPrefs.HasKey("HUDTime"))
		{
			hudTime = PlayerPrefs.GetInt("HUDTime", 0);
		}
		if (PlayerPrefs.HasKey("HUDWeapons"))
		{
			hudWeapons = PlayerPrefs.GetInt("HUDWeapons", 0) == 1;
		}
		hmdWidth = 1080f * ((float)Screen.width / (float)Screen.height);
		hmdHeight = 1080f;
		if (PlayerPrefs.HasKey("HMDHeight"))
		{
			hmdHeight = PlayerPrefs.GetFloat("HMDHeight");
		}
		if (PlayerPrefs.HasKey("HMDWidth"))
		{
			hmdWidth = PlayerPrefs.GetFloat("HMDWidth");
		}
		if (PlayerPrefs.HasKey("HMDTopHeight"))
		{
			hmdTopHeight = PlayerPrefs.GetFloat("HMDTopHeight");
		}
		if (PlayerPrefs.HasKey("HMDSideDist"))
		{
			hmdSideDist = PlayerPrefs.GetFloat("HMDSideDist");
		}
		if (PlayerPrefs.HasKey("HMDSideAngle"))
		{
			hmdSideAngle = PlayerPrefs.GetFloat("HMDSideAngle");
		}
		if (PlayerPrefs.HasKey("HMDHideDist"))
		{
			hmdHideDist = PlayerPrefs.GetFloat("HMDHideDist");
		}
		if (PlayerPrefs.HasKey("HMDIconSize"))
		{
			hmdIconSize = PlayerPrefs.GetFloat("HMDIconSize", 30f);
		}
		if (PlayerPrefs.HasKey("HUDTextSize"))
		{
			hudTextSize = PlayerPrefs.GetFloat("HUDTextSize", 40f);
		}
		if (PlayerPrefs.HasKey("HMDTextSize"))
		{
			hmdTextSize = PlayerPrefs.GetFloat("HMDTextSize", 40f);
		}
		if (PlayerPrefs.HasKey("OverlayTextSize"))
		{
			overlayTextSize = PlayerPrefs.GetFloat("OverlayTextSize", 32f);
		}
		if (PlayerPrefs.HasKey("HUDColorR"))
		{
			hudColorR = PlayerPrefs.GetInt("HUDColorR", 0);
		}
		if (PlayerPrefs.HasKey("HUDColorG"))
		{
			hudColorG = PlayerPrefs.GetInt("HUDColorG", 255);
		}
		if (PlayerPrefs.HasKey("HUDColorB"))
		{
			hudColorB = PlayerPrefs.GetInt("HUDColorB", 0);
		}
		if (PlayerPrefs.HasKey("LandingCam"))
		{
			landingCam = PlayerPrefs.GetInt("LandingCam", 1);
		}
		if (PlayerPrefs.HasKey("RadialControl"))
		{
			radialControl = PlayerPrefs.GetInt("RadialControl", 0);
		}
		graphics.Load();
		DetailSettings.Load();
		if (PlayerPrefs.HasKey("DebugVis"))
		{
			debugVis = PlayerPrefs.GetInt("DebugVis") == 1;
		}
		if (PlayerPrefs.HasKey("VirtualJoystickEnabled"))
		{
			virtualJoystickEnabled = PlayerPrefs.GetInt("VirtualJoystickEnabled") == 1;
		}
		if (PlayerPrefs.HasKey("VirtualJoystickInvertPitch"))
		{
			virtualJoystickInvertPitch = PlayerPrefs.GetInt("VirtualJoystickInvertPitch") == 1;
		}
		if (PlayerPrefs.HasKey("ViewInvertPitch"))
		{
			viewInvertPitch = PlayerPrefs.GetInt("ViewInvertPitch") == 1;
		}
		if (PlayerPrefs.HasKey("ThrottleUseNegative"))
		{
			throttleUseNegative = PlayerPrefs.GetInt("ThrottleUseNegative") == 1;
		}
		if (PlayerPrefs.HasKey("ThrottleUseRelative"))
		{
			throttleUseRelative = PlayerPrefs.GetInt("ThrottleUseRelative") == 1;
		}
		if (PlayerPrefs.HasKey("ControllerMenuNavigation"))
		{
			controllerMenuNavigation = PlayerPrefs.GetInt("ControllerMenuNavigation") == 1;
		}
		if (PlayerPrefs.HasKey("MenuWeaponSafety"))
		{
			menuWeaponSafety = PlayerPrefs.GetInt("MenuWeaponSafety") == 1;
		}
		if (PlayerPrefs.HasKey("InvertCollective"))
		{
			invertCollective = PlayerPrefs.GetInt("InvertCollective") == 1;
		}
		if (PlayerPrefs.HasKey("VirtualJoystickSensitivity"))
		{
			virtualJoystickSensitivity = PlayerPrefs.GetFloat("VirtualJoystickSensitivity");
		}
		if (PlayerPrefs.HasKey("VirtualJoystickCentering"))
		{
			virtualJoystickCentering = PlayerPrefs.GetFloat("VirtualJoystickCentering");
		}
		if (PlayerPrefs.HasKey("ViewSensitivity"))
		{
			viewSensitivity = PlayerPrefs.GetFloat("ViewSensitivity");
		}
		if (PlayerPrefs.HasKey("ViewSmoothing"))
		{
			viewSmoothing = PlayerPrefs.GetFloat("ViewSmoothing");
		}
		if (PlayerPrefs.HasKey("PressDelay"))
		{
			pressDelay = PlayerPrefs.GetFloat("PressDelay");
		}
		if (PlayerPrefs.HasKey("ClickDelay"))
		{
			clickDelay = PlayerPrefs.GetFloat("ClickDelay");
		}
		if (PlayerPrefs.HasKey("ThemeGroupId"))
		{
			themeGroupId = PlayerPrefs.GetString("ThemeGroupId", "vanilla");
		}
		if (PlayerPrefs.HasKey("ThemeGroupEditorAdvancedMode"))
		{
			themeGroupEditorAdvancedMode = PlayerPrefs.GetInt("ThemeGroupEditorAdvancedMode", 0) == 1;
		}
		useTrackIR = PlayerPrefs.GetInt("UseTrackIR", 0) == 1;
		headTrackerType = (HeadTrackerType)PlayerPrefs.GetInt("HeadTrackerType", 1);
		bool flag = false;
		if (!PlayerPrefs.HasKey("Tobii_EyeVsHeadRatio"))
		{
			PlayerPrefs.SetFloat("Tobii_EyeVsHeadRatio", 1f);
			flag = true;
		}
		if (!PlayerPrefs.HasKey("Tobii_EyeTrackingResponsiveness"))
		{
			PlayerPrefs.SetFloat("Tobii_EyeTrackingResponsiveness", 1f);
			flag = true;
		}
		if (!PlayerPrefs.HasKey("Tobii_HeadSensitivityPitchYaw"))
		{
			PlayerPrefs.SetFloat("Tobii_HeadSensitivityPitchYaw", 1f);
			flag = true;
		}
		if (!PlayerPrefs.HasKey("Tobii_CenterStabilization"))
		{
			PlayerPrefs.SetFloat("Tobii_CenterStabilization", 1f);
			flag = true;
		}
		if (!PlayerPrefs.HasKey("Tobii_HeadSensitivityRoll"))
		{
			PlayerPrefs.SetFloat("Tobii_HeadSensitivityRoll", 1f);
			flag = true;
		}
		if (!PlayerPrefs.HasKey("Tobii_HeadSensitivityPosition"))
		{
			PlayerPrefs.SetFloat("Tobii_HeadSensitivityPosition", 1f);
			flag = true;
		}
		if (!PlayerPrefs.HasKey("Tobii_HeadTrackingAutoCenter"))
		{
			PlayerPrefs.SetInt("Tobii_HeadTrackingAutoCenter", 1);
			flag = true;
		}
		if (flag)
		{
			PlayerPrefs.Save();
		}
		tobiiEyeVsHeadRatio = PlayerPrefs.GetFloat("Tobii_EyeVsHeadRatio", 1f);
		tobiiEyeTrackingResponsiveness = PlayerPrefs.GetFloat("Tobii_EyeTrackingResponsiveness", 1f);
		tobiiHeadSensitivityPitchYaw = PlayerPrefs.GetFloat("Tobii_HeadSensitivityPitchYaw", 1f);
		tobiiCenterStabilization = PlayerPrefs.GetFloat("Tobii_CenterStabilization", 1f);
		tobiiHeadSensitivityRoll = PlayerPrefs.GetFloat("Tobii_HeadSensitivityRoll", 1f);
		tobiiHeadSensitivityPosition = PlayerPrefs.GetFloat("Tobii_HeadSensitivityPosition", 1f);
		tobiiHeadTrackingAutoCenter = PlayerPrefs.GetInt("Tobii_HeadTrackingAutoCenter", 1) == 1;
		chatEnabled = PlayerPrefs.GetInt("ChatEnabled", 1) == 1;
		chatFilter = PlayerPrefs.GetInt("ChatFilter", 1) == 1;
		chatTts = PlayerPrefs.GetInt("ChatTts", 0) == 1;
		chatTtsSpeed = PlayerPrefs.GetInt("ChatTtsSpeed", 0);
		chatTtsVolume = PlayerPrefs.GetInt("ChatTtsVolume", 60);
		playerIndexDisplay = (PlayerNameDisplayScope)PlayerPrefs.GetInt("PlayerIndexDisplay", 0);
		serverTagDisplay = (PlayerNameDisplayScope)PlayerPrefs.GetInt("ServerTagDisplay", 0);
		steamRichPresenceEnabled = PlayerPrefs.GetInt("SteamRichPresenceEnabled", 1) == 1;
		discordRichPresenceEnabled = PlayerPrefs.GetInt("DiscordRichPresenceEnabled", 1) == 1;
		killFeedMunition = (KillFeedFilter)PlayerPrefs.GetInt("KillFeedMunition", 4);
		killFeedAircraft = (KillFeedFilter)PlayerPrefs.GetInt("KillFeedAircraft", 4);
		killFeedBuilding = (KillFeedFilter)PlayerPrefs.GetInt("KillFeedBuilding", 4);
		killFeedVehicle = (KillFeedFilter)PlayerPrefs.GetInt("KillFeedVehicle", 4);
		killFeedShip = (KillFeedFilter)PlayerPrefs.GetInt("KillFeedShip", 4);
		killFeedMinValue = PlayerPrefs.GetFloat("KillFeedMinValue", 0f);
		killFeedNbLines = PlayerPrefs.GetInt("KillFeedNbLines", 2);
		if (PlayerPrefs.HasKey("ShowHitMarkers"))
		{
			showHitMarkers = PlayerPrefs.GetInt("ShowHitMarkers", 1) == 1;
		}
		if (PlayerPrefs.HasKey("ShowAircraftDeployedMessage"))
		{
			showAicraftDeployedMessage = PlayerPrefs.GetInt("ShowAircraftDeployedMessage", 1) == 1;
		}
	}

	public static void ApplyPrefs()
	{
		GameManager.eventSystem.sendNavigationEvents = controllerMenuNavigation;
		Debug.Log($"Setting event system Send Navigation Events to {controllerMenuNavigation}");
		if (SceneSingleton<CameraStateManager>.i != null)
		{
			CameraStateManager i = SceneSingleton<CameraStateManager>.i;
			float num = ((i.currentState == i.cockpitState) ? defaultFoV : defaultExternalFoV);
			i.SetDesiredFoV(num, num);
		}
		ApplyTobiiSettings();
		PlayerSettings.OnApplyOptions?.Invoke();
	}

	public static void ApplyTobiiSettings()
	{
		TobiiAPI.ApplyTobiiSettings(tobiiEyeVsHeadRatio, tobiiEyeTrackingResponsiveness, tobiiHeadSensitivityPitchYaw, tobiiHeadSensitivityRoll, tobiiHeadSensitivityPosition, tobiiCenterStabilization, tobiiHeadTrackingAutoCenter);
	}
}
