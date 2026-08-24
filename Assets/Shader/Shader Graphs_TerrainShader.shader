Shader "Shader Graphs/TerrainShader" {
	Properties {
		_detail_scale ("detail_scale", Vector) = (1,1,0,0)
		Vector1_8215e5f818784b058d0f93c84222a604 ("terrainBreakup", Float) = 0
		_normalBreakup ("normalBreakup", Float) = 0
		Vector1_1d51f5d8cc7b4687b932a5b404890ae6 ("breakup_splatmaps", Float) = 0
		[NoScaleOffset] _macro_basecolor ("macro_basecolor", 2D) = "white" {}
		[NoScaleOffset] [Normal] _macro_normal ("macro_normal", 2D) = "bump" {}
		[NoScaleOffset] _macro_ao ("macro_ao", 2D) = "white" {}
		[NoScaleOffset] _splat_grass ("splat_grass", 2D) = "red" {}
		[NoScaleOffset] _splat_rock ("splat_rock", 2D) = "red" {}
		[NoScaleOffset] _splat_lush ("splat_lush", 2D) = "red" {}
		[NoScaleOffset] _splat_fields ("splat_fields", 2D) = "red" {}
		[NoScaleOffset] _SampleTexture2D_dacba2ee34424733acff4f8c92baea27_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_8fd7ac9820c240c383887165ff5b5476_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_674f68b33e3644b4acf5cea7b75e681f_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_92633bd0becc4b749d681a31beaffbaa_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_5a49beebfe604041acd648420add582f_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_3bd9a1d970ff4fbc9888fa54a498dd5f_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_1a4f7e08675c47d2a231a6bf2eacea9b_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] _SampleTexture2D_f43950d91c9a4f2ea1cfb2f7a9ed314b_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_a4670c2df1194ad9a22c4dbe817ab097_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_aea52007ac564f6fbe5b78277e135f0b_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_838acaebc3fd4aa599cb97870fc87e14_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_0c6460df7d7a4405aff8c488bbffc050_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_3f967dd12e9c49af9fa80c834d7c5a19_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_959d2f3a5ff8423b93bfc1fd0a56dd49_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_5ed7e9e7492147fe9dbd547a9ad528c1_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _SampleTexture2D_a867fd84b3f5415caa454567fa1e5506_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
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