using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class UnityGraphicsBullshit
{
	private static FieldInfo MainLightCastShadows_FieldInfo;

	private static FieldInfo AdditionalLightCastShadows_FieldInfo;

	private static FieldInfo MainLightShadowmapResolution_FieldInfo;

	private static FieldInfo AdditionalLightShadowmapResolution_FieldInfo;

	private static FieldInfo Cascade2Split_FieldInfo;

	private static FieldInfo Cascade3Split_FieldInfo;

	private static FieldInfo Cascade4Split_FieldInfo;

	private static FieldInfo SoftShadowsEnabled_FieldInfo;

	public static bool MainLightCastShadows
	{
		get
		{
			return (bool)MainLightCastShadows_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			MainLightCastShadows_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	public static bool AdditionalLightCastShadows
	{
		get
		{
			return (bool)AdditionalLightCastShadows_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			AdditionalLightCastShadows_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	public static UnityEngine.Rendering.Universal.ShadowResolution MainLightShadowResolution
	{
		get
		{
			return (UnityEngine.Rendering.Universal.ShadowResolution)MainLightShadowmapResolution_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			MainLightShadowmapResolution_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	public static UnityEngine.Rendering.Universal.ShadowResolution AdditionalLightShadowResolution
	{
		get
		{
			return (UnityEngine.Rendering.Universal.ShadowResolution)AdditionalLightShadowmapResolution_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			AdditionalLightShadowmapResolution_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	public static float Cascade2Split
	{
		get
		{
			return (float)Cascade2Split_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			Cascade2Split_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	public static Vector2 Cascade3Split
	{
		get
		{
			return (Vector2)Cascade3Split_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			Cascade3Split_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	public static Vector3 Cascade4Split
	{
		get
		{
			return (Vector3)Cascade4Split_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			Cascade4Split_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	public static bool SoftShadowsEnabled
	{
		get
		{
			return (bool)SoftShadowsEnabled_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
		}
		set
		{
			SoftShadowsEnabled_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
		}
	}

	static UnityGraphicsBullshit()
	{
		Type typeFromHandle = typeof(UniversalRenderPipelineAsset);
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
		MainLightCastShadows_FieldInfo = typeFromHandle.GetField("m_MainLightShadowsSupported", bindingAttr);
		AdditionalLightCastShadows_FieldInfo = typeFromHandle.GetField("m_AdditionalLightShadowsSupported", bindingAttr);
		MainLightShadowmapResolution_FieldInfo = typeFromHandle.GetField("m_MainLightShadowmapResolution", bindingAttr);
		AdditionalLightShadowmapResolution_FieldInfo = typeFromHandle.GetField("m_AdditionalLightsShadowmapResolution", bindingAttr);
		Cascade2Split_FieldInfo = typeFromHandle.GetField("m_Cascade2Split", bindingAttr);
		Cascade3Split_FieldInfo = typeFromHandle.GetField("m_Cascade3Split", bindingAttr);
		Cascade4Split_FieldInfo = typeFromHandle.GetField("m_Cascade4Split", bindingAttr);
		SoftShadowsEnabled_FieldInfo = typeFromHandle.GetField("m_SoftShadowsSupported", bindingAttr);
	}
}
