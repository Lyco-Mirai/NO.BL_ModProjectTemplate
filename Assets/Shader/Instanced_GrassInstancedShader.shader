Shader "Instanced/GrassInstancedShader" {
	Properties {
		[Header(Rotation)] [Toggle(_TERRAIN_TILT_ON)] _EnableTerrainTilt ("Enable Terrain Tilt", Float) = 1
		_TiltStrength ("Tilt Strength", Range(0, 1)) = 0.5
		[Header(Wind Settings)] [Toggle(_WIND_ON)] _EnableWind ("Enable Wind", Float) = 1
		_WindSpeed ("Wind Speed", Range(0, 10)) = 1
		_WindStrength ("Wind Strength", Range(0, 2)) = 0.2
		_WindDensity ("Wind Density (Wave Size)", Range(0, 5)) = 0.5
		[Header(Lighting)] _NormalYScale ("Normal Y Scale", Range(0, 2)) = 0.4
		_DiffuseScale ("Diffuse Scale", Range(0, 1)) = 0.8
		_DiffuseBias ("Diffuse Bias", Range(0, 1)) = 0.2
		[Header(Root Ambient Occlusion)] [Toggle(_ROOT_AO_ON)] _EnableRootAO ("Enable Root AO", Float) = 0
		[Toggle(_CLAMP_AO_ON)] _ClampRootAO ("Clamp Root AO 0-1", Float) = 0
		_TipBrightness ("Tip Brightness", Range(1, 2)) = 1.5
		_RootDarkness ("Root Darkness", Range(0, 1)) = 0.3
		_RootFalloff ("AO Falloff Power", Range(0.5, 5)) = 2
		[Header(Double Sided Mesh)] [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
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
}