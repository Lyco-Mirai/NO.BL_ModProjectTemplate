Shader "Shader Graphs/TreeShader_Instanced" {
	Properties {
		_ScaleMinMax ("ScaleMinMax", Vector) = (0,0,0,0)
		_Y_Offset ("Y_Offset", Float) = 0
		[HDR] _BaseColor ("BaseColor", Vector) = (1,1,1,1)
		[NoScaleOffset] _BaseMap ("BaseMap", 2D) = "white" {}
		[NoScaleOffset] [Normal] _BumpMap ("NormalMap", 2D) = "bump" {}
		_Cutoff ("AlphaThreshold", Float) = 0.75
		_GlobalWeight ("GlobalWeight", Float) = 1
		[HDR] _ColorLush ("ColorLush", Vector) = (1,1,1,1)
		[HDR] _ColorAltitude ("ColorAltitude", Vector) = (1,1,1,1)
		_ValueVariation ("ValueVariation", Vector) = (0.8,1.2,0,0)
		_HueVariation ("HueVariation", Vector) = (-0.1,0.1,0,0)
		_LeakMaskSensitivity ("LeakMaskSensitivity", Float) = 0.2
		_LeafMaskOffSet ("LeafMaskOffSet", Float) = 0
		_BlastColor ("BlastColor", Vector) = (0,0,0,0)
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