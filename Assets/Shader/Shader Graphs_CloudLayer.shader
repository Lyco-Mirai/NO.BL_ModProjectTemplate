Shader "Shader Graphs/CloudLayer" {
	Properties {
		_CloudsOriginOffset ("OriginOffset", Vector) = (0,0,0,0)
		_distortion ("distortion", Float) = 0
		_distortionScale ("distortionScale", Float) = 1
		_CloudEmissive ("cloudEmissive", Float) = 0
		[NoScaleOffset] _cloudPattern ("cloudPattern", 2D) = "white" {}
		_cloudPatternScale ("cloudPatternScale", Float) = 16
		_cloudPaternDimension ("cloudPaternDimension", Float) = 1024
		_displacement ("displacement", Float) = 1
		_fadeDepth ("fadeDepth", Float) = 0
		_fadeStrength ("fadeStrength", Float) = 0
		_macroScale ("macroScale", Float) = 0
		_macroStrength ("macroStrength", Float) = 0
		[NoScaleOffset] [Normal] _SampleTexture2DLOD_6589372cff2144379bebc3bf406e7ffd_Texture_1_Texture2D ("Texture2D", 2D) = "bump" {}
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