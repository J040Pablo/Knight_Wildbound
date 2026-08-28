Shader "Roguelite/StylizedCloud"
{
    Properties
    {
        _TopColor ("Cloud Primary Color", Color) = (0.949, 0.961, 0.949, 1.0)
        _ShadowColor ("Cloud Shadow Color", Color) = (0.796, 0.851, 0.875, 1.0)
        _SunDir ("Sun Direction", Vector) = (0.51, 0.79, -0.34, 0.0)
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100
        Cull Back ZWrite On

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            float4 _TopColor;
            float4 _ShadowColor;
            float4 _SunDir;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 norm = normalize(i.worldNormal);
                float3 sunDir = normalize(_SunDir.xyz);

                // Blend based on upward normal and sun direction
                float upWeight = saturate(norm.y * 0.6 + dot(norm, sunDir) * 0.4);
                float3 finalColor = lerp(_ShadowColor.rgb, _TopColor.rgb, upWeight);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
