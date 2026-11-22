Shader "Office Chaos/Training PSX Surface"
{
    Properties
    {
        _BaseColor ("Surface color", Color) = (0.35, 0.42, 0.48, 1)
        _SeamColor ("Seam color", Color) = (0.18, 0.21, 0.24, 1)
        _TileSize ("Tile size (metres)", Vector) = (1.5, 1.5, 0, 0)
        _Panel ("Wall panel details", Range(0, 1)) = 0
        _AffineAmount ("Texture warp", Range(0, 1)) = 0.24
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "PSX Surface"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _SeamColor;
                float4 _TileSize;
                half _Panel;
                half _AffineAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 tileUV : TEXCOORD2;
                float2 affineUV : TEXCOORD3;
                float fog : TEXCOORD4;
                half3 vertexLight : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                Varyings output;
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(position.positionCS.z);
                output.vertexLight = VertexLighting(position.positionWS, output.normalWS);

                float3 axis = abs(output.normalWS);
                float3 world = position.positionWS;
                float2 planar = axis.y > 0.5 ? world.xz : (axis.x > 0.5 ? world.zy : world.xy);
                output.tileUV = planar / _TileSize.xy;
                output.affineUV = output.tileUV * position.positionCS.w;

                // Keep the depth and near-plane clipping from the original projection.
                if (position.positionCS.w > 0.2)
                {
                    float2 grid = float2(216.0 * _ScreenParams.x / _ScreenParams.y, 216.0);
                    float2 ndc = position.positionCS.xy / position.positionCS.w;
                    float2 cell = (ndc * 0.5 + 0.5) * grid;
                    float2 snapped = (round(cell) / grid * 2.0 - 1.0) * position.positionCS.w;
                    output.positionCS.xy = lerp(output.positionCS.xy, snapped, 0.8);
                }
                return output;
            }

            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            half3 Surface(float2 uv)
            {
                float2 texel = floor(uv * 32.0);
                float2 cell = floor(texel / 32.0);
                float2 local = texel - cell * 32.0;
                float2 edge = min(local, 31.0 - local);
                float seam = 1.0 - step(1.0, min(edge.x, edge.y));
                float bevel = 1.0 - step(2.0, min(local.x, local.y));
                float grain = Hash(texel) - 0.5;
                float mottling = Hash(floor(texel * 0.25)) - 0.5;
                float tileShade = Hash(cell) - 0.5;
                float variation = 1.0 + grain * 0.09 + mottling * 0.14 + tileShade * 0.17;
                float bolts = step(2.0, edge.x) * (1.0 - step(4.0, edge.x))
                    * step(2.0, edge.y) * (1.0 - step(4.0, edge.y));
                half3 tile = _BaseColor.rgb * (variation + bevel * 0.11);
                tile *= 1.0 - bolts * _Panel * 0.44;
                return lerp(tile, _SeamColor.rgb, seam);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Limit warping so long training corridors do not pull tiles across neighbouring rooms.
                float2 affineUV = input.affineUV * input.positionCS.w;
                float2 uv = input.tileUV + clamp(affineUV - input.tileUV, -0.8, 0.8) * _AffineAmount;
                half3 normalWS = normalize(input.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half facing = saturate(dot(normalWS, sun.direction));
                half stepped = floor(facing * 3.0 + 0.5) / 3.0;
                half3 lighting = half3(0.57, 0.62, 0.69) + sun.color * stepped * lerp(0.13, 0.39, sun.shadowAttenuation);
                lighting += input.vertexLight * 0.22;

                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint i = 0; i < count; i++)
                {
                    Light lamp = GetAdditionalLight(i, input.positionWS);
                    lighting += lamp.color * saturate(dot(normalWS, lamp.direction)) * lamp.distanceAttenuation * 0.28;
                }
                #endif

                return half4(MixFog(Surface(uv) * lighting, input.fog), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
