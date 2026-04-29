Shader "Custom/TransparentFlowBlue"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        [HDR]_BaseColor ("Blue Color", Color) = (0, 0.5, 1, 1)
        _FlowSpeedX ("Main Flow Speed X", Float) = 0.0
        _FlowSpeedY ("Main Flow Speed Y", Float) = 0.5

        [Space(10)]
        [Header(Distortion Noise)]
        _NoiseTex ("Noise Texture ", 2D) = "gray" {}
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.1
        _NoiseSpeedX ("Noise Speed X", Float) = 0.2
        _NoiseSpeedY ("Noise Speed Y", Float) = 0.2

        [Space(10)]
        [Header(Global)]
        _AlphaControl ("Alpha Control", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        // 叠加发光模式
        Blend SrcAlpha One 
        ZWrite Off    
        Cull Off      

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR; 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMain     : TEXCOORD0; // 主纹理UV
                float2 uvNoise    : TEXCOORD1; // 噪声图UV
                float4 color      : COLOR; 
            };

            // 声明两张纹理和采样器
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _BaseColor;
                float _FlowSpeedX;
                float _FlowSpeedY;
                float _DistortionStrength;
                float _NoiseSpeedX;
                float _NoiseSpeedY;
                float _AlphaControl;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // 计算受Tiling和Offset影响的基础UV
                output.uvMain = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.uvNoise = input.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
                
                output.color = input.color; // 透传粒子颜色
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. 计算噪声图的流动UV
                float2 noiseUV = input.uvNoise + float2(_Time.y * _NoiseSpeedX, _Time.y * _NoiseSpeedY);
                
                // 2. 采样噪声图
                half4 noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV);
                
                // 将噪声图的颜色值(0到1)映射到(-1到1)的方向偏移量上
                // 我们使用噪声图的 r 和 g 通道分别干扰 x 和 y 方向
                float2 distortion = (noiseSample.rg - 0.5) * 2.0; 
                
                // 3. 计算主纹理的流动UV
                float2 mainUV = input.uvMain + float2(_Time.y * _FlowSpeedX, _Time.y * _FlowSpeedY);
                
                // 4. 【核心】将扭曲值加到主UV上
                mainUV += distortion * _DistortionStrength;
                
                // 5. 采样主纹理（此时的UV已经被扭曲了）
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);
                
                // 6. 计算最终颜色 = 纹理 * 面板颜色 * 粒子颜色
                half4 finalColor = texColor * _BaseColor * input.color;
                finalColor.a *= _AlphaControl;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}