Shader "Roguelite/StylizedSkybox"
{
    Properties
    {
        _SkyTopColor ("Sky Top Color", Color) = (0.247, 0.463, 0.710, 1.0)
        _SkyMidColor ("Sky Mid Color", Color) = (0.412, 0.651, 0.847, 1.0)
        _SkyHorizonColor ("Sky Horizon Color", Color) = (0.663, 0.780, 0.835, 1.0)
        _SunColor ("Sun Disc Color", Color) = (1.0, 0.898, 0.639, 1.0)
        _SunDir ("Sun Direction", Vector) = (0.51, 0.79, -0.34, 0.0)
        _SunSize ("Sun Size", Range(0.001, 0.1)) = 0.035
        _SunHalo ("Sun Halo Softness", Range(0.001, 0.2)) = 0.04
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldDir : TEXCOORD0;
            };

            float4 _SkyTopColor;
            float4 _SkyMidColor;
            float4 _SkyHorizonColor;
            float4 _SunColor;
            float4 _SunDir;
            float _SunSize;
            float _SunHalo;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldDir = normalize(v.vertex.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldDir);
                float y = dir.y;

                // 3-stop vertical sky gradient
                float3 skyColor;
                if (y > 0.2)
                {
                    float t = saturate((y - 0.2) / 0.8);
                    skyColor = lerp(_SkyMidColor.rgb, _SkyTopColor.rgb, t);
                }
                else
                {
                    float t = saturate((y + 0.1) / 0.3);
                    skyColor = lerp(_SkyHorizonColor.rgb, _SkyMidColor.rgb, t);
                }

                // Stylized Sun Disc
                float3 normSunDir = normalize(_SunDir.xyz);
                float sunDot = dot(dir, normSunDir);
                float distFromSun = 1.0 - sunDot;

                if (distFromSun < _SunSize + _SunHalo)
                {
                    float sunAlpha = 1.0 - saturate((distFromSun - _SunSize) / _SunHalo);
                    sunAlpha = smoothstep(0.0, 1.0, sunAlpha);
                    skyColor = lerp(skyColor, _SunColor.rgb, sunAlpha);
                }

                return fixed4(skyColor, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
