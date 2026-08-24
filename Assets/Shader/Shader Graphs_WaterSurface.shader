Shader "Shader Graphs/WaterSurface" {
	Properties {
		Strength ("Strength", Range(0, 1)) = 0
		_EdgeStrength ("EdgeStrength", Range(0, 1)) = 0
		Deep_Water_Color ("Deep Water Color", Vector) = (0.09825561,0.2451128,0.3018868,0)
		Shallow_Water_Color ("Shallow Water Color", Vector) = (0.2205411,0.7137698,0.7924528,0)
		[NoScaleOffset] [Normal] MainNormal ("MainNormal", 2D) = "bump" {}
		Normal_Strength ("Normal Strength", Range(0, 1)) = 0
		_WaterSmoothness ("WaterSmoothness", Float) = 0.9
		_HorizonSmoothness ("HorizonSmoothness", Float) = 0
		_ShoreRoughWidth ("ShoreRoughWidth", Float) = 0
		_Wind ("Wind", Float) = 0
		_Whitecaps ("Whitecaps", Float) = 0
		_WaveScale ("WaveScale", Float) = 0.005
		_WaveHeight ("WaveHeight", Float) = 0
		_OriginOffset ("OriginOffset", Vector) = (0,0,0,0)
		[NoScaleOffset] _macro_basecolor ("macro_basecolor", 2D) = "white" {}
		[NoScaleOffset] _macro_depth ("macro_depth", 2D) = "white" {}
		_size ("size", Vector) = (81920,81920,0,0)
		_breakupScale ("breakupScale", Float) = 1
		_breakupStrength ("breakupStrength", Float) = 0
		_offset ("offset", Vector) = (0,0,0,0)
		_breakupSpeed ("breakupSpeed", Float) = 1
		_waveSpeed ("waveSpeed", Float) = 1
		[NoScaleOffset] _SampleTexture2DLOD_63adaf16e7ec42c295130dc0f5a4b4b3_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2DLOD_13f64f554de64b398dcb1927eea7b23f_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _Texture2DAsset_a18afdd830cf4c9385ece00b2be7a760_Out_0_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_35a003360e7c4f91b4e3a5b79e3895e0_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}