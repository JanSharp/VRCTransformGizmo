Shader "Unlit/TransformGizmoRotationCircle"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _EmptyColor ("Empty Color", Color) = (0,0,0,1)
        _Angle ("Angle", float) = 60
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
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _Color;
            float4 _EmptyColor;
            float _Angle;

            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Interpolator
            {
                float4 vertex : SV_POSITION; // clip space
                float3 normal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Interpolator vert (MeshData v)
            {
                Interpolator o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (Interpolator i) : SV_Target
            {
                float2 pos = i.uv * 2 - 1;
                if (length(pos) > 1)
                    return float4(0, 0, 0, 0);
                pos = normalize(pos);

                float rad = (degrees(atan2(pos.y, pos.x)) + 360) % 360;
                float partialAngle = _Angle % 360;
                float multiplier = floor(abs(_Angle) / 360)
                    + (_Angle > 0
                    ? rad <= partialAngle
                    : 360 - rad <= -partialAngle);

                if (multiplier == 0)
                    return _EmptyColor;

                float4 color = saturate(_Color * multiplier);

                // float3 forward = -mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                float3 forward = normalize(i.viewDir);
                return float4(saturate(abs(dot(i.normal, forward)).xxx), 1) * color;
            }
            ENDCG
        }
    }
}
