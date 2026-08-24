Shader "Shader Graphs/groundShockwaveShader" {
	Properties {
		_shockwaveExpansion ("shockwaveExpansion", Float) = 1
		_decalSize ("decalSize", Float) = 1
		_opacity ("opacity", Float) = 1
		[NoScaleOffset] _SampleTexture2D_93bbb647db754e20800a41884568111b_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_12c2c9e9ab67475792d0489d4821ef70_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_a62f0623d2aa4fd9a9d2b6c325ca2b8d_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_1f294a6febea435b8f69220de37db46f_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[HideInInspector] _DrawOrder ("Draw Order", Range(-50, 50)) = 0
		[Enum(Depth Bias, 0, View Bias, 1)] [HideInInspector] _DecalMeshBiasType ("DecalMesh BiasType", Float) = 0
		[HideInInspector] _DecalMeshDepthBias ("DecalMesh DepthBias", Float) = 0
		[HideInInspector] _DecalMeshViewBias ("DecalMesh ViewBias", Float) = 0
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