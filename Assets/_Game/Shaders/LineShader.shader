Shader "Custom/LineShader"
{
    Properties
    {
        _ColorA("Color A", Color) = (1,0,0,1)
        _ColorB("Color B", Color) = (0,0,1,1)
        _Direction("Split Direction", Vector) = (0,1,0,0)
        _Split("Split Offset", Float) = 0
        _Smooth("Smooth Blend", Range(0,1)) = 0.05
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; };

            float4 _ColorA, _ColorB;
            float3 _Direction;
            float _Split, _Smooth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Chuẩn hóa hướng chia
                float3 dir = normalize(_Direction);

                // Tính vị trí chiếu lên hướng chia
                float projection = dot(i.worldPos, dir);

                // So sánh để pha trộn hai màu
                float t = smoothstep(_Split - _Smooth, _Split + _Smooth, projection);
                return lerp(_ColorA, _ColorB, t);
            }
            ENDCG
        }
    }
}
