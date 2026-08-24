Shader "Shader Graphs/ShipTilingShader" {
	Properties {
		[NoScaleOffset] _BaseColor ("BaseColor", 2D) = "white" {}
		[NoScaleOffset] _BaseColorDamage ("BaseColorDamage", 2D) = "white" {}
		[NoScaleOffset] _Normal ("Normal", 2D) = "white" {}
		[NoScaleOffset] _NormalDamage ("NormalDamage", 2D) = "white" {}
		[NoScaleOffset] _MetallicRoughness ("MetallicRoughness", 2D) = "white" {}
		[NoScaleOffset] _MetallicRoughnessDamage ("MetallicRoughnessDamage", 2D) = "white" {}
		_HitPoints ("HitPoints", Float) = 0
		[NoScaleOffset] _DamageMask ("DamageMask", 2D) = "white" {}
		_DamageTiling ("DamageTiling", Vector) = (0,0,0,0)
		[NoScaleOffset] _LeakMask ("LeakMask", 2D) = "white" {}
		[NoScaleOffset] _Normal_Crinkle ("Normal Crinkle", 2D) = "white" {}
		[NoScaleOffset] _PaintMask ("PaintMask", 2D) = "white" {}
		[NoScaleOffset] _AO ("AO", 2D) = "red" {}
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