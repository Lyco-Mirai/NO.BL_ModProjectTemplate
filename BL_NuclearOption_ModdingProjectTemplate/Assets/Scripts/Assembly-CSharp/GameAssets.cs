using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.DebugScripts;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameAssets : ScriptableObject
{
	private static readonly ResourcesAsyncLoader<GameAssets> loader = ResourcesAsyncLoader.Create("GameAssets", (Action<GameAssets>)null);

	[Tooltip("Font used to display players name")]
	public TMP_FontAsset playerNameFont;

	public GameObject missionEditor;

	public GameObject splash_large;

	public Material nightSky;

	public Material daySky;

	public Material blackSky;

	public Material cloudySky;

	public Gradient fogColor;

	public Gradient fogColorAltitude;

	public Gradient skyColor;

	public Gradient skyColorAltitude;

	public Gradient horizonColor;

	public Gradient groundColor;

	public Gradient groundColorAltitude;

	public Gradient sunColor;

	public Gradient redGreenGradient;

	public AnimationCurve airDensityAltitude;

	public AnimationCurve ambientIntensity;

	public GameObject debugArrow;

	public GameObject debugArrowGreen;

	public GameObject debugArrowFlat;

	public DebugControlInputsDisplay debugControlInputsDisplay;

	public GameObject debugPoint;

	public DebugText debugText;

	public GameObject debugSphere;

	public GameObject blastRadiusDebug;

	public GameObject waypointDebug;

	public GameObject settingsMenu;

	public LoadingScreen loadingScreenPrefab;

	public GameObject lobbyPasswordPrompt;

	public GameObject flashErrorMessage;

	public Sprite noWeapons;

	public Material BSOD;

	public GameObject pilotDismounted;

	public GameObject targetScreenCanvas;

	public GameObject LandingScreenCanvas;

	public PhysicMaterial terrainMaterial;

	public PhysicMaterial debrisMaterial;

	public PhysicMaterial frictionlessMaterial;

	public PhysicMaterial WaterMaterial;

	public Material targetScreenMatOn;

	public Material targetScreenMatOff;

	public GameObject targetCam;

	public GameObject tacScreen;

	public Sprite airbaseSprite;

	public Sprite hangarSprite_helipad;

	public Sprite hangarSprite_revetment;

	public Sprite hangarSprite_medium;

	public Sprite hangarSprite_shelter;

	public Sprite hangarSprite_carrier;

	public Sprite targetUnitSprite;

	public Sprite targetUnitSpriteFriendly;

	public Sprite targetUnitSpriteOld;

	public Sprite targetUnitSpriteJammed;

	public Sprite missileWarningSprite;

	public Sprite warheadSprite;

	public Sprite aircraftSprite;

	public Sprite closestAircraftSprite;

	public GameObject tracer;

	public GameObject scorchMarkDecal;

	public Color HUDFriendly;

	public Color HUDHostile;

	public Color HUDNeutral;

	public Color HUDFriendlySelected = Color.green;

	public Color HUDHostileSelected = Color.yellow;

	public Color HUDNeutralSelected = Color.yellow;

	public Color HUDAirbaseNotAvailable = Color.grey;

	public GameObject contactSmoke;

	public GameObject contactDust;

	public GameObject PIDTuner;

	public GameObject controlsFilterTuner;

	public GameObject trimDebug;

	public GameObject aircraftActionsReport;

	public GameObject killDisplay;

	public AudioClip radioStatic;

	public AudioClip deathSound;

	public AudioClip missionFailedMusic;

	public AudioClip missionSuccessMusic;

	public AudioClip nuclearEscalationMusic;

	public AudioClip hitMarkerSound;

	public AudioClip sonicBoom;

	public AudioClip errorTone;

	public GameObject exclusionZoneDisplay;

	public GameObject[] groundImpacts;

	public UniversalRenderPipelineAsset URPAsset;

	public GameObject airbasePrefab;

	public Faction[] defaultFactions;

	[Header("Particle Systems")]
	public GameObject rotorStrike_solid;

	public GameObject rotorStrike_dirt;

	public GameObject rotorStrike_water;

	public GameObject vehicleWreckDestroyed;

	[Header("Wheel Audio")]
	public AudioClip wheelRollingRoad;

	public AudioClip wheelSlidingRoad;

	public AudioClip wheelRollingDirt;

	public AudioClip wheelSlidingDirt;

	[Header("Compute Shaders")]
	public ComputeShader FrustumCullingCompute;

	public ComputeShader GroupCountCompute;

	public static GameAssets i => loader.Get();

	public static async UniTask Preload(CancellationToken cancel)
	{
		await loader.Load(cancel);
	}
}
