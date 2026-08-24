Shader "Shader Graphs/Airfield" {
	Properties {
		[NoScaleOffset] _BaseColor ("BaseColor", 2D) = "white" {}
		[NoScaleOffset] _Splatmap ("Splatmap", 2D) = "white" {}
		[NoScaleOffset] _GrassBasecolorMacro ("GrassBasecolorMacro", 2D) = "white" {}
		[NoScaleOffset] _GrassBasecolorDetail ("GrassBasecolorDetail", 2D) = "white" {}
		[NoScaleOffset] _DirtBasecolorMacro ("DirtBasecolorMacro", 2D) = "white" {}
		[NoScaleOffset] _DirtBasecolorDetail ("DirtBasecolorDetail", 2D) = "white" {}
		[NoScaleOffset] [Normal] _GrassNormalMacro ("GrassNormalMacro", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _GrassNormalDetail ("GrassNormalDetail", 2D) = "bump" {}
		[NoScaleOffset] _DirtNormalMacro ("DirtNormalMacro", 2D) = "white" {}
		[NoScaleOffset] [Normal] _DirtNormalDetail ("DirtNormalDetail", 2D) = "bump" {}
		_DetailStrength ("DetailStrength", Float) = 1
		_BreakupStrength ("BreakupStrength", Float) = 0
		_MacroScale ("MacroScale", Vector) = (0,0,0,0)
		_DetailScale ("DetailScale", Vector) = (0,0,0,0)
		_BreakupScale ("BreakupScale", Vector) = (0,0,0,0)
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