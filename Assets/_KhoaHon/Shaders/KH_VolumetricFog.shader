// Sương thể tích Blackwood Manor — dùng bởi KHVolumetricFogFeature (URP Render Graph).
// Pass 0: raymarch (RGB = ánh sáng tán xạ, A = độ truyền qua). Pass 1: làm mờ giữ cạnh. Pass 2: trộn lên ảnh camera.
Shader "Hidden/KhoaHon/VolumetricFog"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        float4 _FogParams0; // density, heightFalloff, baseHeight, maxDistance
        float4 _FogParams1; // anisotropy, mainLightMul, addLightMul, startDistance
        float4 _FogParams2; // noiseScale, noiseIntensity, steps, frame
        float4 _FogTint;
        float4 _FogAmbient;
        float4 _FogWind;    // xyz gió, w thời gian
        float4 _FogBlurDir;

        float LinearDepthAt(float2 uv)
        {
            return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
        }
        ENDHLSL

        // ------------------------------------------------------------ 0: Raymarch
        Pass
        {
            Name "KH Fog Raymarch"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragRaymarch
            #pragma target 3.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash13(i), n100 = Hash13(i + float3(1, 0, 0));
                float n010 = Hash13(i + float3(0, 1, 0)), n110 = Hash13(i + float3(1, 1, 0));
                float n001 = Hash13(i + float3(0, 0, 1)), n101 = Hash13(i + float3(1, 0, 1));
                float n011 = Hash13(i + float3(0, 1, 1)), n111 = Hash13(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float Density(float3 p)
            {
                float h = max(0.0, p.y - _FogParams0.z);
                float d = _FogParams0.x * exp(-h * _FogParams0.y);
                float3 q = p * _FogParams2.x + _FogWind.xyz * _FogWind.w * _FogParams2.x;
                float n = ValueNoise(q) * 0.65 + ValueNoise(q * 2.7 + 13.1) * 0.35;
                return d * lerp(1.0, saturate(n * 1.6 - 0.2), _FogParams2.y);
            }

            float PhaseHG(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(max(1e-4, 1.0 + g2 - 2.0 * g * cosTheta), 1.5));
            }

            float InterleavedGradientNoise(float2 pixel, float frame)
            {
                pixel += frame * 5.588238;
                return frac(52.9829189 * frac(0.06711056 * pixel.x + 0.00583715 * pixel.y));
            }

            float4 FragRaymarch(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float rawDepth = SampleSceneDepth(uv);
            #if !UNITY_REVERSED_Z
                rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
            #endif
                float3 endWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 camWS = _WorldSpaceCameraPos;
                float3 ray = endWS - camWS;
                float rayLen = length(ray);
                float3 rd = ray / max(rayLen, 1e-4);
                rayLen = min(rayLen, _FogParams0.w);
                // Bỏ đoạn sát camera (tránh đốm chói ở nguồn đèn pin / tay cầm)
                float tStart = min(_FogParams1.w, rayLen);
                float march = rayLen - tStart;
                if (march <= 1e-3) return float4(0, 0, 0, 1);

                int steps = (int)_FogParams2.z;
                float dt = march / steps;
                float jitter = InterleavedGradientNoise(input.positionCS.xy, _FogParams2.w);
                float g = _FogParams1.x;

                Light mainLight = GetMainLight();
                float mainPhase = PhaseHG(dot(mainLight.direction, rd), g);

                float3 inScatter = 0;
                float transmittance = 1;
                half4 shadowMask = half4(1, 1, 1, 1);

                [loop]
                for (int i = 0; i < steps; i++)
                {
                    float t = tStart + (i + jitter) * dt;
                    float3 p = camWS + rd * t;
                    float sigma = Density(p);
                    if (sigma < 1e-5) continue;

                    // Ánh sáng chính (trăng) có bóng đổ → tia sáng qua cửa sổ
                    float4 sc = TransformWorldToShadowCoord(p);
                    float mainShadow = MainLightRealtimeShadow(sc);
                    float3 light = mainLight.color * (mainShadow * mainPhase * _FogParams1.y);

                    // Đèn phụ: đèn pin, nến, đèn treo
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.normalizedScreenSpaceUV = uv;
                    inputData.positionWS = p;
                    uint lightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light al = GetAdditionalLight(lightIndex, p, shadowMask);
                        float ph = PhaseHG(dot(al.direction, rd), g);
                        float att = min(al.distanceAttenuation, 1.5); // kẹp vùng sát nguồn sáng
                        light += al.color * (att * al.shadowAttenuation * ph * _FogParams1.z);
                    LIGHT_LOOP_END
                #endif

                    light += _FogAmbient.rgb;
                    float3 S = light * _FogTint.rgb * sigma;
                    float ext = exp(-sigma * dt);
                    inScatter += transmittance * (S - S * ext) / sigma;
                    transmittance *= ext;
                    if (transmittance < 0.01) break;
                }
                return float4(inScatter, transmittance);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------ 1: Làm mờ giữ cạnh theo depth
        Pass
        {
            Name "KH Fog Blur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlur

            float4 FragBlur(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float d0 = LinearDepthAt(uv);
                static const float w[4] = { 0.383, 0.242, 0.061, 0.006 };
                float4 sum = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * w[0];
                float wsum = w[0];
                [unroll]
                for (int i = 1; i < 4; i++)
                {
                    float2 o = _FogBlurDir.xy * i * 1.5;
                    float2 uvA = uv + o, uvB = uv - o;
                    float da = LinearDepthAt(uvA), db = LinearDepthAt(uvB);
                    float wa = w[i] * exp(-abs(da - d0) / max(0.05 * d0, 0.05));
                    float wb = w[i] * exp(-abs(db - d0) / max(0.05 * d0, 0.05));
                    sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvA) * wa;
                    sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvB) * wb;
                    wsum += wa + wb;
                }
                return sum / wsum;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------ 2: Trộn (Blend One SrcAlpha: dst = scatter + dst * T)
        Pass
        {
            Name "KH Fog Composite"
            Blend One SrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            float4 FragComposite(Varyings input) : SV_Target
            {
                float4 f = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                return float4(f.rgb, saturate(f.a));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
