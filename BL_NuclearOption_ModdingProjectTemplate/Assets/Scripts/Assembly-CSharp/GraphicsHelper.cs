using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicsHelper
{
	public readonly GraphicsHelperSO Settings;

	private int _mipmapLevel;

	private int _shadowQuality;

	private bool _vsync;

	private int _fpsLimit;

	private float _cloudDetail;

	private int _antiAliasing;

	private int _shadowDistance;

	private bool _softshadows;

	private float _lodBias;

	private int _anisotropicFiltering;

	private int _maxLights;

	public static readonly List<string> MipmapLimitOptions = new List<string> { "High", "Medium", "Low" };

	public static readonly List<string> ShadowQualityOptions = new List<string> { "Off", "Low", "Medium", "High", "Ultra" };

	public static readonly List<string> AnisotropicOptions = new List<string> { "Off", "2x", "4x", "8x", "16x" };

	public static readonly List<string> AAOptions = new List<string> { "Off", "2x", "4x", "8x" };

	public static readonly List<string> MaxLightsOptions = new List<string> { "None", "2", "4", "8" };

	public bool Vsync
	{
		get
		{
			return _vsync;
		}
		set
		{
			_vsync = value;
			SetVSync(value);
			PlayerPrefs.SetInt("Vsync", value ? 1 : 0);
		}
	}

	public int FpsLimit
	{
		get
		{
			return _fpsLimit;
		}
		set
		{
			_fpsLimit = value;
			SetFPSLimit(value);
			PlayerPrefs.SetInt("FpsLimit", value);
		}
	}

	public int MipmapLevel
	{
		get
		{
			return _mipmapLevel;
		}
		set
		{
			_mipmapLevel = value;
			SetMipmapLimit(value);
			PlayerPrefs.SetInt("MipmapLevel", value);
		}
	}

	public int ShadowQuality
	{
		get
		{
			return _shadowQuality;
		}
		set
		{
			_shadowQuality = value;
			SetShadowQuality(value);
			PlayerPrefs.SetInt("ShadowQuality", value);
		}
	}

	public float CloudDetail
	{
		get
		{
			return _cloudDetail;
		}
		set
		{
			_cloudDetail = value;
			PlayerPrefs.SetFloat("CloudDetail", value);
		}
	}

	public int AntiAliasing
	{
		get
		{
			return _antiAliasing;
		}
		set
		{
			_antiAliasing = value;
			SetAntiAliasing(value);
			PlayerPrefs.SetInt("AntiAliasing", value);
		}
	}

	public int ShadowDistance
	{
		get
		{
			return _shadowDistance;
		}
		set
		{
			_shadowDistance = value;
			SetShadowDistance(value);
			PlayerPrefs.SetInt("ShadowDistance", value);
		}
	}

	public bool SoftShadows
	{
		get
		{
			return _softshadows;
		}
		set
		{
			_softshadows = value;
			SetSoftShadows(value);
			PlayerPrefs.SetInt("SoftShadows", value ? 1 : 0);
		}
	}

	public float LodBias
	{
		get
		{
			return _lodBias;
		}
		set
		{
			_lodBias = value;
			SetLodBias(value);
			PlayerPrefs.SetFloat("LODBias", value);
		}
	}

	public int AnisotropicFiltering
	{
		get
		{
			return _anisotropicFiltering;
		}
		set
		{
			_anisotropicFiltering = value;
			SetAnisotropic(value);
			PlayerPrefs.SetInt("AnisotropicFiltering", value);
		}
	}

	public int MaxLights
	{
		get
		{
			return _maxLights;
		}
		set
		{
			_maxLights = value;
			SetMaxLights(value);
			PlayerPrefs.SetInt("MaxLights", value);
		}
	}

	public GraphicsHelper(GraphicsHelperSO settings)
	{
		Settings = settings;
	}

	public void SetDefaults()
	{
		_mipmapLevel = Settings._mipmapLevel;
		_shadowQuality = Settings._shadowQuality;
		_vsync = Settings._vsync;
		_fpsLimit = Settings._fpsLimit;
		_cloudDetail = Settings._cloudDetail;
		_antiAliasing = Settings._antiAliasing;
		_shadowDistance = Settings._shadowDistance;
		_softshadows = Settings._softshadows;
		_lodBias = Settings._lodBias;
		_anisotropicFiltering = Settings._anisotropicFiltering;
		_maxLights = Settings._maxLights;
	}

	public void Load()
	{
		SetDefaults();
		GetBool("Vsync", ref _vsync);
		GetInt("FpsLimit", ref _fpsLimit);
		GetInt("MipmapLevel", ref _mipmapLevel);
		GetInt("ShadowQuality", ref _shadowQuality);
		GetFloat("CloudDetail", ref _cloudDetail);
		GetInt("AntiAliasing", ref _antiAliasing);
		GetInt("ShadowDistance", ref _shadowDistance);
		GetBool("SoftShadows", ref _softshadows);
		GetFloat("LODBias", ref _lodBias);
		GetInt("AnisotropicFiltering", ref _anisotropicFiltering);
		GetInt("MaxLights", ref _maxLights);
		ApplyAll();
		static void GetBool(string key, ref bool value)
		{
			if (PlayerPrefs.HasKey(key))
			{
				value = PlayerPrefs.GetInt(key) == 1;
			}
		}
		static void GetFloat(string key, ref float value)
		{
			if (PlayerPrefs.HasKey(key))
			{
				value = PlayerPrefs.GetFloat(key);
			}
		}
		static void GetInt(string key, ref int value)
		{
			if (PlayerPrefs.HasKey(key))
			{
				value = PlayerPrefs.GetInt(key);
			}
		}
	}

	private void ApplyAll()
	{
		SetVSync(_vsync);
		SetFPSLimit(_fpsLimit);
		SetMipmapLimit(_mipmapLevel);
		SetShadowQuality(_shadowQuality);
		SetShadowDistance(_shadowDistance);
		SetSoftShadows(_softshadows);
		SetLodBias(_lodBias);
		SetAnisotropic(_anisotropicFiltering);
		SetAntiAliasing(_antiAliasing);
		SetMaxLights(_maxLights);
	}

	public void Clear()
	{
		PlayerPrefs.DeleteKey("Vsync");
		PlayerPrefs.DeleteKey("FpsLimit");
		PlayerPrefs.DeleteKey("MipmapLevel");
		PlayerPrefs.DeleteKey("ShadowQuality");
		PlayerPrefs.DeleteKey("CloudDetail");
		PlayerPrefs.DeleteKey("AntiAliasing");
		PlayerPrefs.DeleteKey("ShadowDistance");
		PlayerPrefs.DeleteKey("SoftShadows");
		PlayerPrefs.DeleteKey("LODBias");
		PlayerPrefs.DeleteKey("AnisotropicFiltering");
		PlayerPrefs.DeleteKey("MaxLights");
		PlayerPrefs.Save();
		SetDefaults();
		ApplyAll();
	}

	public static void SetMipmapLimit(int level)
	{
		QualitySettings.globalTextureMipmapLimit = level;
	}

	public static void SetShadowQuality(int dropdownIndex)
	{
		int mainLightShadowResolution = dropdownIndex switch
		{
			1 => 512, 
			2 => 1024, 
			3 => 2048, 
			4 => 4096, 
			_ => 0, 
		};
		UnityGraphicsBullshit.MainLightCastShadows = dropdownIndex > 0;
		UnityGraphicsBullshit.MainLightShadowResolution = (UnityEngine.Rendering.Universal.ShadowResolution)mainLightShadowResolution;
	}

	public void SetShadowDistance(float rawValue)
	{
		UniversalRenderPipelineAsset universalRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
		if (!(universalRenderPipelineAsset == null))
		{
			float value = Mathf.Round(rawValue / 500f) * 500f;
			value = (universalRenderPipelineAsset.shadowDistance = Mathf.Clamp(value, 500f, 10000f));
			if (value <= Settings.shortRangeThreshold)
			{
				universalRenderPipelineAsset.shadowCascadeCount = 2;
				UnityGraphicsBullshit.Cascade2Split = Settings.cascade2Split;
			}
			else if (value <= Settings.midRangeThreshold)
			{
				universalRenderPipelineAsset.shadowCascadeCount = 3;
				UnityGraphicsBullshit.Cascade3Split = Settings.cascade3Split;
			}
			else
			{
				universalRenderPipelineAsset.shadowCascadeCount = 4;
				UnityGraphicsBullshit.Cascade4Split = Settings.cascade4Split;
			}
		}
	}

	public static void SetSoftShadows(bool enabled)
	{
		UnityGraphicsBullshit.SoftShadowsEnabled = enabled;
	}

	public static void SetAnisotropic(int dropdownIndex)
	{
		if (dropdownIndex <= 0)
		{
			QualitySettings.anisotropicFiltering = UnityEngine.AnisotropicFiltering.Disable;
			return;
		}
		QualitySettings.anisotropicFiltering = UnityEngine.AnisotropicFiltering.ForceEnable;
		int num = dropdownIndex switch
		{
			1 => 2, 
			2 => 4, 
			3 => 8, 
			4 => 16, 
			_ => 1, 
		};
		Texture.SetGlobalAnisotropicFilteringLimits(num, num);
	}

	public static void SetAntiAliasing(int dropdownIndex)
	{
		UniversalRenderPipelineAsset universalRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
		if (!(universalRenderPipelineAsset == null))
		{
			UniversalRenderPipelineAsset universalRenderPipelineAsset2 = universalRenderPipelineAsset;
			universalRenderPipelineAsset2.msaaSampleCount = dropdownIndex switch
			{
				1 => 2, 
				2 => 4, 
				3 => 8, 
				_ => 1, 
			};
		}
	}

	public static void SetVSync(bool enabled)
	{
		QualitySettings.vSyncCount = (enabled ? 1 : 0);
	}

	public static void SetLodBias(float value)
	{
		QualitySettings.lodBias = value;
	}

	public static void SetFPSLimit(int value)
	{
		GameManager.TargetFrameRate = value;
		GameManager.LimitFrameRate(GameManager.gameState);
	}

	public static void SetMaxLights(int value)
	{
		UniversalRenderPipelineAsset universalRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
		if (!(universalRenderPipelineAsset == null))
		{
			universalRenderPipelineAsset.maxAdditionalLightsCount = ((value != 0) ? ((int)Mathf.Pow(2f, value)) : 0);
		}
	}
}
