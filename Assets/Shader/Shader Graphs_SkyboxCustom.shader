Shader "Shader Graphs/SkyboxCustom" {
	Properties {
		_AmbientIntensity ("AmbientIntensity", Float) = 0
		[HDR] _SunColor ("SunColor", Vector) = (0,0,0,0)
		_ScatterSizeLarge ("ScatterSizeLarge", Float) = 0
		_ScatterIntensity ("ScatterIntensity", Float) = 0
		_SunDirection ("SunDirection", Vector) = (0,0,0,0)
		_FogColor ("FogColor", Vector) = (0.764151,0.764151,0.764151,0)
		[HDR] _SkyColorBright ("SkyColorBright", Vector) = (0.08966714,0.269481,0.6132076,0)
		[HDR] _SkyColorDark ("SkyColorDark", Vector) = (0.003604487,0.01183696,0.02830189,0)
		_CloudOcclusion ("CloudOcclusion", Float) = 0
		_CameraPosition ("CameraPosition", Vector) = (0,0,0,0)
		_CirrusLayerHeight ("CirrusLayerHeight", Float) = 10000
		_Altitude ("Altitude", Float) = 0
		_Conditions ("Conditions", Float) = 0
		[NoScaleOffset] _SampleTexture2D_6a21b1efe24b4ee78cac3dae35b93db9_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_28e1c94502d34861923c9d1d4d4af81b_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_fa091e4516e6432ebd0a9df0d19ee1ba_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[HideInInspector] _BUILTIN_QueueOffset ("Float", Float) = 0
		[HideInInspector] _BUILTIN_QueueControl ("Float", Float) = -1
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