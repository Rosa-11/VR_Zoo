Shader "Custom/URP/skybox1"
{
    Properties
    {
        _TopColor       ("Top Color", Color) = (0.2, 0.5, 0.8, 1)
        _MiddleColor    ("Middle Color", Color) = (0.8, 0.7, 0.5, 1)
        _BottomColor    ("Bottom Color", Color) = (0.1, 0.2, 0.3, 1)
        
        _TopBound       ("Top Boundary (Y)", Range(-1, 1)) = 0.8
        _BottomBound    ("Bottom Boundary (Y)", Range(-1, 1)) = -0.8
        
        _Intensity      ("Intensity", Range(0, 2)) = 1.0
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue"      = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldDir   : TEXCOORD0;
            };
            
            float3 _TopColor;
            float3 _MiddleColor;
            float3 _BottomColor;
            float  _TopBound;
            float  _BottomBound;
            float  _Intensity;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // 将模型空间的顶点方向（假设模型原点为中心）转换到世界方向（忽略平移）
                output.worldDir = mul((float3x3)unity_ObjectToWorld, input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.worldDir);
                float y = dir.y;
                
                float t = (y - _BottomBound) / (_TopBound - _BottomBound);
                t = clamp(t, 0.0, 1.0);
                
                float midStart = 0.35;
                float midEnd   = 0.65;
                
                float3 color;
                if (t < midStart)
                {
                    float subT = t / midStart;
                    color = lerp(_TopColor, _MiddleColor, subT);
                }
                else if (t > midEnd)
                {
                    float subT = (t - midEnd) / (1.0 - midEnd);
                    color = lerp(_MiddleColor, _BottomColor, subT);
                }
                else
                {
                    color = _MiddleColor;
                }
                
                color *= _Intensity;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    
    Fallback Off
}