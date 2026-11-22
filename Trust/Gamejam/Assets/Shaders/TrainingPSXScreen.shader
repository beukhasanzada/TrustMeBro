Shader "Office Chaos/Training PSX Screen"
{
    Properties
    {
        _VerticalPixels ("Virtual screen height", Range(144, 360)) = 216
        _DitherStrength ("Ordered dithering", Range(0, 2)) = 0.9
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "PSX Color"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _VerticalPixels;
                float _DitherStrength;
            CBUFFER_END

            int Bayer2(int2 pixel)
            {
                return 2 * pixel.x + 3 * pixel.y - 4 * pixel.x * pixel.y;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float height = min(_ScreenParams.y, _VerticalPixels);
                float2 grid = float2(round(height * _ScreenParams.x / _ScreenParams.y), height);
                float2 uv = (input.texcoord - _BlitScaleBias.zw) / _BlitScaleBias.xy;
                int2 pixel = int2(floor(uv * grid));
                float2 center = (pixel + 0.5) / grid;
                float2 sourceUV = center * _BlitScaleBias.xy + _BlitScaleBias.zw;
                half4 source = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, sourceUV, 0);

                int bayer = 4 * Bayer2(pixel & 1) + Bayer2((pixel >> 1) & 1);
                float dither = ((bayer + 0.5) / 16.0 - 0.5) * _DitherStrength;
                half3 color = saturate(LinearToSRGB(source.rgb));
                color = saturate(floor(color * 31.0 + 0.5 + dither) / 31.0);
                return half4(SRGBToLinear(color), source.a);
            }
            ENDHLSL
        }
    }
}
