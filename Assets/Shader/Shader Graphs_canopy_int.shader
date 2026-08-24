Shader "Shader Graphs/canopy_int" {
	Properties {
		Color_ebc4234c048d46f093d81898c9ff2cd0 ("Color", Vector) = (0,0,0,0)
		Vector1_5ec4e4d573524154b63367e0518c1a6e ("Metallic", Float) = 0
		[NoScaleOffset] Texture2D_15033af04b7f42909945f09cd1b851a9 ("BaseColor", 2D) = "white" {}
		_Smoothness ("Smoothness", Float) = 0
		[NoScaleOffset] _Grime ("Grime", 2D) = "white" {}
		_Grime_Intensity ("Grime Intensity", Float) = 0
		_Grime_Tiling ("Grime Tiling", Float) = 0
		_Scratches_Intensity ("Scratches Intensity", Float) = 0
		_Scratches_Tiling ("Scratches Tiling", Float) = 0
		_Cracked_Amount ("Cracked Amount", Float) = 0
		_Cracks_Tiling ("Cracks Tiling", Vector) = (0,0,0,0)
		[NoScaleOffset] _SampleTexture2D_51ab9e981153496b807052eb4be84d33_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_5415c280ead44b35b01bd558db4d1a80_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
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