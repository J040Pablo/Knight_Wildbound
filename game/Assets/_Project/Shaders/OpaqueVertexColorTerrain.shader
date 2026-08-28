Shader "Roguelite/OpaqueVertexColorTerrain"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color : COLOR;
                LIGHTING_COORDS(1, 2)
            };

            fixed4 _Color;
            fixed4 _WorldSpaceLightPos0;
            fixed4 _LightColor0;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);

                float4 vCol = v.color;
                if (vCol.r == 0.0 && vCol.g == 0.0 && vCol.b == 0.0)
                {
                    vCol = float4(1, 1, 1, 1);
                }
                o.color = vCol * _Color;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);

                float NdotL = saturate(dot(N, L));
                fixed atten = LIGHT_ATTENUATION(i);

                float3 sunLight = _LightColor0.rgb * (NdotL * atten);
                float3 ambientLight = ShadeSH9(half4(N, 1.0));

                float3 totalLighting = sunLight + ambientLight;
                float3 finalColor = i.color.rgb * totalLighting;

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Mobile/Diffuse"
}
