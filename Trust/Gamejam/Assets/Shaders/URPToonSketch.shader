Shader "Custom/URPToonSketch"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.94, 0.91, 0.85, 1)
        _ShadowColor ("Shadow Tint", Color) = (0.58, 0.56, 0.62, 1)
        _SketchMap ("Sketch Hatching Map", 2D) = "white" {}
        _SketchScale ("Sketch Scale", Float) = 0.45
        _SketchStrength ("Sketch Strength", Range(0, 1)) = 0.45
        _ToonSteps ("Toon Steps", Range(2, 5)) = 3
        _RimColor ("Rim Color", Color) = (1, 1, 1, 0.25)
        _RimPower ("Rim Power", Range(1, 8)) = 4.0
        _OutlineColor ("Outline Color", Color) = (0.10, 0.09, 0.11, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.08)) = 0.022
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ToonSketchForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SketchMap); SAMPLER(sampler_SketchMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                float _SketchScale;
                half _SketchStrength;
                half _ToonSteps;
                half4 _RimColor;
                half _RimPower;
                half4 _OutlineColor;
                float _OutlineWidth;
                half4 _EmissionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(normInputs.normalWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half SampleTriplanarSketch(float3 posWS, float3 normalWS)
            {
                float3 blend = abs(normalWS);
                blend = blend / max(blend.x + blend.y + blend.z, 0.001);
                half sx = SAMPLE_TEXTURE2D(_SketchMap, sampler_SketchMap, posWS.zy * _SketchScale).r;
                half sy = SAMPLE_TEXTURE2D(_SketchMap, sampler_SketchMap, posWS.xz * _SketchScale).r;
                half sz = SAMPLE_TEXTURE2D(_SketchMap, sampler_SketchMap, posWS.xy * _SketchScale).r;
                return sx * blend.x + sy * blend.y + sz * blend.z;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo = baseTex.rgb * _BaseColor.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction) * 0.5h + 0.5h);
                half lit = ndotl * mainLight.shadowAttenuation;

                half steps = max(2.0h, round(_ToonSteps));
                half stepped = floor(lit * steps) / (steps - 1.0h);
                stepped = saturate(stepped);

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, IN.positionWS);
                    half addNdotL = saturate(dot(normalWS, addLight.direction));
                    half addAtten = addNdotL * addLight.distanceAttenuation;
                    half addStep = step(0.18h, addAtten) * min(addAtten * 1.4h, 0.85h);
                    albedo += baseTex.rgb * _BaseColor.rgb * addLight.color * addStep * 0.45h;
                }
                #endif

                half sketch = SampleTriplanarSketch(IN.positionWS, normalWS);
                half shadowMask = 1.0h - stepped;
                half hatchFactor = lerp(1.0h, lerp(1.0h, sketch, _SketchStrength), shadowMask * 0.85h + 0.15h);

                half3 ambient = SampleSH(normalWS) * 0.35h;
                half3 litColor = lerp(albedo * _ShadowColor.rgb, albedo * mainLight.color, stepped);
                half3 finalCol = (litColor + albedo * ambient) * hatchFactor;

                half rim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower);
                finalCol += _RimColor.rgb * (_RimColor.a * step(0.45h, rim));
                finalCol += _EmissionColor.rgb;

                finalCol = MixFog(finalCol, IN.fogFactor);
                return half4(finalCol, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                float _SketchScale;
                half _SketchStrength;
                half _ToonSteps;
                half4 _RimColor;
                half _RimPower;
                half4 _OutlineColor;
                float _OutlineWidth;
                half4 _EmissionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float  fogFactor   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);
                posWS += normWS * _OutlineWidth;
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half3 col = MixFog(_OutlineColor.rgb, IN.fogFactor);
                return half4(col, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
