Shader "Universal Render Pipeline/Simple Lit" {
	Properties {
		_BaseMap ("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}
		_BaseColor ("Base Color", Vector) = (1,1,1,1)
		_Cutoff ("Alpha Clipping", Range(0, 1)) = 0.5
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
		_SpecColor ("Specular Color", Vector) = (0.5,0.5,0.5,0.5)
		_SpecGlossMap ("Specular Map", 2D) = "white" {}
		_SmoothnessSource ("Smoothness Source", Float) = 0
		_SpecularHighlights ("Specular Highlights", Float) = 1
		[HideInInspector] _BumpScale ("Scale", Float) = 1
		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		[HDR] _EmissionColor ("Emission Color", Vector) = (0,0,0,1)
		[NoScaleOffset] _EmissionMap ("Emission Map", 2D) = "white" {}
		_Surface ("__surface", Float) = 0
		_Blend ("__blend", Float) = 0
		_Cull ("__cull", Float) = 2
		[ToggleUI] _AlphaClip ("__clip", Float) = 0
		[HideInInspector] _SrcBlend ("__src", Float) = 1
		[HideInInspector] _DstBlend ("__dst", Float) = 0
		[HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
		[HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0
		[HideInInspector] _ZWrite ("__zw", Float) = 1
		[HideInInspector] _BlendModePreserveSpecular ("_BlendModePreserveSpecular", Float) = 1
		[HideInInspector] _AlphaToMask ("__alphaToMask", Float) = 0
		[ToggleUI] _ReceiveShadows ("Receive Shadows", Float) = 1
		_QueueOffset ("Queue offset", Float) = 0
		[HideInInspector] _MainTex ("BaseMap", 2D) = "white" {}
		[HideInInspector] _Color ("Base Color", Vector) = (1,1,1,1)
		[HideInInspector] _Shininess ("Smoothness", Float) = 0
		[HideInInspector] _GlossinessSource ("GlossinessSource", Float) = 0
		[HideInInspector] _SpecSource ("SpecularHighlights", Float) = 0
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Universal Render Pipeline/FallbackError"
	//CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.SimpleLitShader"
}