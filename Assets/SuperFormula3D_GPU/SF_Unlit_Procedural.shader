Shader "Unlit/SuperFormulaUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue"="Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            struct MeshPoint
            {
                float3 pos; 
                float3 normal;
                float4 tangent;
                float2 uv;
                
                int3 tanInt1;
                int3 tanInt2;
                int3 normalInt;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexId : SV_VertexID;
                uint instanceId : SV_InstanceID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<MeshPoint> pointBuffer;
            StructuredBuffer<uint> trianglesBuffer;
        
            float4x4 TRSmatrix;
            fixed4 _Color;

            float4 vert(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID) : SV_POSITION
            {
                //#ifdef SHADER_API_D3D11
                //get vertex position            
                int positionIndex = trianglesBuffer[vertex_id];
                float3 position = pointBuffer[positionIndex].pos ;
                float4 pt = mul(TRSmatrix,float4(position,1.0));
            //#endif
                return  UnityObjectToClipPos(pt); 
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
