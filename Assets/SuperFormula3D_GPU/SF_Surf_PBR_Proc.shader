Shader "Custom/SuperFormulaSurfacePBR"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("NormalMap", 2D) = "black" {}
        _RoughnessMap ("RoughnessMap", 2D) = "black" {}
        _OcclusionMap ("OcclusionMap", 2D) = "white"{} 
        _DisplacementMap ("DisplacementMap", 2D) = "black" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Displacement ("Displacement", Range(0,1)) = 0.5
        _NormalStrength ("NormalPower", Range(0,1)) = 0.5 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard vertex:vertMove  fullforwardshadows addshadow  //tessellate:tessDistance
        #include "UnityCG.cginc"

        #pragma target 4.6
        #include "Tessellation.cginc"

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

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _RoughnessMap;
        sampler2D _OcclusionMap;
        sampler2D _DisplacementMap;

        
        float _Tess;
        float _NormalStrength;

        #ifdef SHADER_API_D3D11	
            StructuredBuffer<MeshPoint> pointBuffer;
            StructuredBuffer<uint> trianglesBuffer;
        #endif

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float4 col;
            float3 normal;
        };
        
        struct myAppData
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            float4 texcoord2 : TEXCOORD2;
            float4 texcoord3 : TEXCOORD3;
            float4 tangent : TANGENT ;
            fixed4 color : COLOR;
            uint vertexId : SV_VertexID;
            uint instanceId : SV_InstanceID;
        };

        float4 tessDistance (myAppData v0, myAppData v1, myAppData v2) 
        {
            float minDist = 10.0;
            float maxDist = 25.0;
            return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
        }

        float3 safeNormalize(float3 v)
        {
            if(length(v)>0)
            {
                return normalize(v);
            }
            return float3(0,1,0);
        }

        half _Smoothness;
        half _Metallic;
        float _Displacement;
        fixed4 _Color;
        float4x4 TRSmatrix;

        void vertMove(inout myAppData v) //, out Input data) 
        {
            //Input data;
            //UNITY_INITIALIZE_OUTPUT(Input, data);
            
            #ifdef SHADER_API_D3D11
                //get vertex position            
                int positionIndex = trianglesBuffer[v.vertexId];
                float3 position = pointBuffer[positionIndex].pos ;
                // float3 floatNormalFromInt = (float3)(pointBuffer[positionIndex].normalInt) / 1000000.0;
                // float3 normal = safeNormalize(mul(TRSmatrix,floatNormalFromInt));
                float3 normal = mul(TRSmatrix, pointBuffer[positionIndex].normal);
                float4 tangent = pointBuffer[positionIndex].tangent;
                float2 uv = pointBuffer[positionIndex].uv;
                float displacement = (tex2Dlod(_DisplacementMap, float4(2 * uv,0,0)).r) * _Displacement;
                
                position += (displacement * normal);
                v.vertex = mul(TRSmatrix,float4(position,1.0));
                v.normal = safeNormalize(normal);
                v.texcoord = float4(uv,0,0);
                v.tangent = tangent;
            #endif
        }

        // UNITY_INSTANCING_BUFFER_START(Props)
        //     // put more per-instance properties here
        // UNITY_INSTANCING_BUFFER_END(Props)
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float4 c = tex2D (_MainTex, float2(IN.uv_MainTex.xy)) * _Color;
            o.Albedo = c;
            o.Emission = 0;
            o.Smoothness = _Smoothness * (1 - tex2D(_RoughnessMap, IN.uv_MainTex));
            o.Metallic = _Metallic;
            o.Occlusion = tex2D (_OcclusionMap, IN.uv_MainTex);
            
            o.Normal = lerp( UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap)), fixed3(0,0,1), -_NormalStrength + 1);

            // float3 normal = lerp(float3(0, 0, 1), pow(UnpackNormal(tex2D(_NormalMap,  IN.uv_NormalMap)),4), _NormalStrength);
            // o.Normal = normal; //lerp(IN.normal, UnpackNormal (tex2D (_NormalMap, IN.uv_NormalMap)), _NormalStrength);
            // Metallic and smoothness come from slider variables
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
