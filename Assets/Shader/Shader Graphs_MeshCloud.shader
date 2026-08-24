Shader "Shader Graphs/MeshCloud" {
	Properties {
		[NoScaleOffset] _CloudShapeOpacity ("CloudShapeOpacity", 2D) = "white" {}
		[NoScaleOffset] _CloudShapeNormal ("CloudShapeNormal", 2D) = "white" {}
		[NoScaleOffset] _CloudShapeEmission ("CloudShapeEmission", 2D) = "white" {}
		[NoScaleOffset] _PatternBaseColor ("PatternBaseColor", 2D) = "white" {}
		_OpacityBoost ("OpacityBoost", Float) = 0
		[HDR] _ScatterColor ("ScatterColor", Vector) = (0,0,0,0)
		_ScatterAmount ("ScatterAmount", Float) = 1
		_SunDirection ("SunDirection", Vector) = (0,0,0,0)
		_FadeDepth ("FadeDepth", Float) = 50
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