Shader "Unlit/UnlitReceiveShadow"
{
     Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _ShadowStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // Sample shadow
                fixed shadow = SHADOW_ATTENUATION(i);

                // Apply only shadow darkness, no light color
                col.rgb *= lerp(1.0, shadow, _ShadowStrength);

                return col;
            }
            ENDCG
        }

        // Optional: cast shadow
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
