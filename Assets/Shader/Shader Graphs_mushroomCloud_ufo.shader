Shader "Shader Graphs/mushroomCloud_ufo" {
	Properties {
		_EmissiveStrength ("EmissiveStrength", Float) = 1
		_EmissiveRoughness ("EmissiveRoughness", Float) = 1
		_CloudColor ("CloudColor", Vector) = (0,0,0,0)
		_DisplacementStrength ("DisplacementStrength", Float) = 1
		_ScrollPosition ("ScrollPosition", Float) = -0.1
		[NoScaleOffset] _SampleTexture2DLOD_3984a51e66eb47bf8a0e63bb1f4df163_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_d359b19573b349cab1a24dd865c6e648_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] _SampleTexture2D_23da8cc485f84376bf702c0456a132a7_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_1578a3df1ac645579671f47de9b887c5_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_fef004a1c1114ea09dff1deb993bed95_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
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