Shader "UI/URP/DiagonalScroll"
{
    Properties
    {
        _MainTex ("Repeating Texture", 2D) = "white" {}
        _Columns ("Columns (X Tiles)", Int) = 10
        _Rows    ("Rows (Y Tiles)", Int) = 10
        _Speed   ("Scroll Speed", Float) = 0.2
        _Color      ("Tint Color", Color)= (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanvasMaterial"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // per-material CBUFFER
            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                int _Columns;
                int _Rows;
                float _Speed;
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 tiledUV = IN.uv * float2(_Columns, _Rows);
                float  rowIdx  = floor(tiledUV.y);
                float2 cellUV  = frac(tiledUV);

                float t = _Time.y * _Speed;

                // 좌에서 우 우에서좌
                float2 dir = (fmod(rowIdx, 2) < 0.5)
                             ? float2(-1, 0)
                             : float2(+1, 0);

                //UV 오프셋
                float2 offs     = dir * t / float2(_Columns, _Rows);
                float2 sampleUV = frac(cellUV + offs);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);

                return tex * IN.color * _Color;
            }
            ENDHLSL
        }
    }
}
