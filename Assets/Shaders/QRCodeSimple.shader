Shader "Unlit/QRCodeSimple"
{
    Properties
    {
        _QRMask ("QR Mask", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (1, 1, 1, 1)
        _ForegroundColor ("Foreground Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100
        Cull Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_QRMask);
            SAMPLER(sampler_QRMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _QRMask_ST;
                half4 _BackgroundColor;
                half4 _ForegroundColor;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _QRMask);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 mask = SAMPLE_TEXTURE2D(_QRMask, sampler_QRMask, i.uv);
                return mask.a > 0.5 ? _ForegroundColor : _BackgroundColor;
            }
            ENDHLSL
        }
    }
}
