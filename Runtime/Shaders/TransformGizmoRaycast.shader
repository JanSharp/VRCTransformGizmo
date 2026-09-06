Shader "Unlit/TransformGizmoRaycast"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Point1 ("Point 1", Vector) = (0,0,0,0)
        _Point2 ("Point 2", Vector) = (0,0,0,0)
        _Point3 ("Point 3", Vector) = (0,0,0,0)
        _PointRange ("Point Range", float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            ZWrite Off
            ZTest Off
            Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float4 _Color;
            uniform float4 _Point1;
            uniform float4 _Point2;
            uniform float4 _Point3;
            uniform float _PointRange;

            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 uv : TEXCOORD0;
            };

            struct Interpolator
            {
                float4 vertex : SV_POSITION; // clip space
                float3 normal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 uv : TEXCOORD3;
            };

            Interpolator vert (MeshData v)
            {
                Interpolator o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = _WorldSpaceCameraPos - o.worldPos;
                o.uv = v.uv;
                return o;
            }

            float4 frag (Interpolator i) : SV_Target
            {
                float depth = length(i.viewDir);
                float pointRange = _PointRange * depth / 10;
                // float3 forward = normalize(i.viewDir);
                // float4 color = float4(saturate(dot(i.normal, forward).xxx), 0) * _Color;
                float4 color = float4(_Color.rgb, 0);
                if (_Point1.a)
                    color.a = max(color.a, 1 - saturate(length((i.worldPos - _Point1.xyz) / pointRange)));
                if (_Point2.a)
                    color.a = max(color.a, 1 - saturate(length((i.worldPos - _Point2.xyz) / pointRange)));
                if (_Point3.a)
                    color.a = max(color.a, 1 - saturate(length((i.worldPos - _Point3.xyz) / pointRange)));
                color.a *= _Color.a;
                return color;
            }
            ENDCG
        }
    }
}
