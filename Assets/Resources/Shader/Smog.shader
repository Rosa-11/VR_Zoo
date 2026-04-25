Shader "Universal Render Pipeline/Custom/ToonBasic"
{
    Properties
    {
        [Header(Base Texture)]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        
        [Header(Toon Shading)]
        _ShadowColor ("Shadow Color", Color) = (0.5,0.5,0.5,1)
        _ShadowStep ("Shadow Step", Range(0, 1)) = 0.5
        _ShadowFeather ("Shadow Feather", Range(0, 0.1)) = 0.01
        
        [Header(Ambient Lighting)]
        _EnvUpColor ("Environment Up Color", Color) = (0.7,0.7,1.0,1.0)
        _EnvSideColor ("Environment Side Color", Color) = (0.4,0.4,0.5,1.0)
        _EnvDownColor ("Environment Down Color", Color) = (0.1,0.1,0.2,1.0)
        _EnvIntensity ("Environment Intensity", Range(0, 2)) = 0.5
        _EnvFalloff ("Environment Falloff", Range(0.1, 5)) = 2.0
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.025
        
        [Header(Receive Shadow)]
        [Toggle(_RECEIVE_SHADOW)] _ReceiveShadow ("Receive Shadow", Float) = 1

        [Header(Particle)]
        // 开启后读取 Particle System 的顶点颜色 Alpha，实现随时间透明
        [Toggle(_USE_VERTEX_COLOR)] _UseVertexColor ("Use Vertex Color (Particle)", Float) = 0
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ShadowColor;
            float _ShadowStep;
            float _ShadowFeather;
            
            float4 _EnvUpColor;
            float4 _EnvSideColor;
            float4 _EnvDownColor;
            float _EnvIntensity;
            float _EnvFalloff;
            
            float4 _OutlineColor;
            float _OutlineWidth;
        CBUFFER_END
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        ENDHLSL
        
        // 第一个Pass：轮廓线
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            // 透明混合，顶点颜色 Alpha 趋近于 0 时轮廓自动消失
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _USE_VERTEX_COLOR
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;      // ← 顶点颜色
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  alpha      : TEXCOORD0;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);
                
                float3 outlineOffset = normalWS * _OutlineWidth;
                positionWS += outlineOffset;
                
                output.positionCS = TransformWorldToHClip(positionWS);

                // 将顶点颜色 Alpha 传到 Fragment
                #if defined(_USE_VERTEX_COLOR)
                    output.alpha = input.color.a;
                #else
                    output.alpha = 1.0;
                #endif

                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 col = _OutlineColor;
                col.a    *= input.alpha; // Alpha 趋近 0 时轮廓消失
                return col;
            }
            ENDHLSL
        }
        
        // 第二个Pass：主渲染
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature _RECEIVE_SHADOW
            #pragma shader_feature _USE_VERTEX_COLOR

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
                float4 color      : COLOR;      // ← Particle System 顶点颜色
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 vertColor  : TEXCOORD3;  // ← 传到 Fragment
            };
            
            half3 CalculateEnvironmentLight(float3 worldNormal)
            {
                float upFactor   = saturate(worldNormal.y);
                float upWeight   = pow(upFactor, _EnvFalloff);
                float downWeight = pow(saturate(-worldNormal.y), _EnvFalloff);
                float sideWeight = pow(1.0 - abs(worldNormal.y), _EnvFalloff);
                
                float totalWeight = upWeight + sideWeight + downWeight;
                upWeight   /= totalWeight;
                sideWeight /= totalWeight;
                downWeight /= totalWeight;
                
                half3 envLight = _EnvUpColor.rgb   * upWeight   +
                                 _EnvSideColor.rgb * sideWeight +
                                 _EnvDownColor.rgb * downWeight;
                
                return envLight * _EnvIntensity;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs     = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS   = normalInputs.normalWS;
                output.uv         = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.vertColor  = input.color;

                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                float3 normalWS = normalize(input.normalWS);
                
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight    = GetMainLight(shadowCoord);
                
                float NdotL = dot(normalWS, mainLight.direction);
                float combinedShadow = NdotL;
                
                #if defined(_RECEIVE_SHADOW)
                    combinedShadow *= mainLight.shadowAttenuation;
                    
                    #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                        uint additionalLightCount = GetAdditionalLightsCount();
                        for (uint i = 0; i < additionalLightCount; i++)
                        {
                            Light additionalLight  = GetAdditionalLight(i, input.positionWS);
                            combinedShadow        *= additionalLight.shadowAttenuation;
                        }
                    #endif
                #endif
                
                float shadowMask = smoothstep(
                    _ShadowStep - _ShadowFeather,
                    _ShadowStep + _ShadowFeather,
                    combinedShadow
                );
                
                half3 envLight     = CalculateEnvironmentLight(normalWS);
                half3 ambient      = SampleSH(normalWS) * baseColor.rgb;
                half3 finalAmbient = ambient + envLight * baseColor.rgb;

                half3 litColor    = baseColor.rgb * mainLight.color + finalAmbient;
                half3 shadowColor = baseColor.rgb * mainLight.color * _ShadowColor.rgb
                                  + finalAmbient  * _ShadowColor.rgb;

                half3 finalColor = lerp(shadowColor, litColor, shadowMask);

                // ✅ Particle 模式：用顶点颜色叠色，Alpha 控制消散
                half finalAlpha = baseColor.a;
                #if defined(_USE_VERTEX_COLOR)
                    finalColor *= input.vertColor.rgb;
                    finalAlpha *= input.vertColor.a;
                #endif

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
        
        // ShadowCaster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            float3 _LightDirection;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);
                
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        
        // DepthOnly Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord   : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}