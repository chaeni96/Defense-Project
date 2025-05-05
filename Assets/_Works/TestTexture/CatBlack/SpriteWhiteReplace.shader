Shader "Custom/SpriteWhiteReplace_TintedGradientThreshold"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Threshold ("Brightness Threshold (R)", Range(0,1)) = 0.47
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Threshold;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                ixed4 texCol = tex2D(_MainTex, i.uv);

                texCol.a *= i.color.a;
                float t = step(_Threshold, texCol.r); 
                float3 tinted = texCol.rgb * i.color.rgb;
                texCol.rgb = lerp(texCol.rgb, tinted, t);

                // 3) 최종 리턴
                return texCol;
            }
            ENDCG
        }
    }
}
