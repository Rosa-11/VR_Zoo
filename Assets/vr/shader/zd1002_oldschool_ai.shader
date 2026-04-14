Shader "Unlit/zd1002_oldschool_ai"
{
    Properties
    {
        _NormalTex ("Normal Map", 2D) = "bump" {}
        _BaseTex("Base Texture",2D)="white"{}
        _BaseTex_ST ("Base Tex ST", Vector) = (1,1,0,0)  // 添加ST参数
        _EmissionTex("Emission Texture",2D)="black"{}
        _AOTex("AO Texture",2D)="white"{}
        _MatCapTex ("MatCap Texture", 2D) = "white" {}
        _MetallicTex("Metallic Texture",2D)="white"{}  // 修正拼写
        // 颜色
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _EnvUpColor("Environment up color",color)=(1.0,1.0,1.0,1.0)
        _EnvDownColor("Environment down color",color)=(1.0,1.0,1.0,1.0)
        _EnvralColor("Environment right and left color",color)=(1.0,1.0,1.0,1.0)
        _LightColor("Light Color",color)=(1.0,1.0,1.0,1.0)
        [HDR]_EmissionColor("Emission Color",color)=(1.0,1.0,1.0,1.0)
        // 强度
        _frenelpow("Fresnel Power", Range(0,10)) = 1
        _GlossStrength("Gloss Strength",Range(0,90))=30
        _EnvSpecStrength("Environment specular strength",Range(0,5))=1
        _FrenelStrength("Frenel specular strength",Range(0,5))=1
        _EnvDiffuseStrength("Environment diffuse strength",Range(0,1))=1
        _EmissionStrength("Emission strength",Range(0,10))=1  // 修正注释
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_shadows  // 支持阴影

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include"../cginc/test.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 normalWS : TEXCOORD5;
                SHADOW_COORDS(6)  // 阴影坐标
            };

            sampler2D _NormalTex;
            float4 _NormalTex_ST;
            sampler2D _MatCapTex;
            sampler2D _BaseTex;
            float4 _BaseTex_ST;  // 声明ST参数
            sampler2D _EmissionTex;
            sampler2D _AOTex;
            sampler2D _MetallicTex;  // 修正拼写

            float3 _EnvUpColor;
            float3 _EnvDownColor;
            float3 _EnvralColor;
            float4 _BaseColor;
            float3 _LightColor;
            float3 _EmissionColor;
            float _frenelpow;
            float _GlossStrength;
            float _EnvSpecStrength;
            float _FrenelStrength;
            float _EnvDiffuseStrength;
            float _EmissionStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseTex);  // 应用ST参数
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // 计算世界空间法线、切线和副切线（归一化）
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.tangentWS = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
                o.bitangentWS = normalize(cross(o.normalWS, o.tangentWS) * v.tangent.w);  // 归一化副切线
                
                TRANSFER_SHADOW(o);  // 传递阴影坐标
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 计算修正后的法线（世界空间）
                float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                float3 ndirTS = UnpackNormal(tex2D(_NormalTex, TRANSFORM_TEX(i.uv, _NormalTex)));
                float3 ndirWS = normalize(mul(ndirTS, TBN));
                float3 ndirVS =normalize(mul(UNITY_MATRIX_V, float4(ndirWS, 0.0)).xyz);
                // 2. 计算光照方向（修正光源类型判断）
                float3 ldir;
                if (_WorldSpaceLightPos0.w == 0.0) {
                    ldir = normalize(_WorldSpaceLightPos0.xyz);  // 平行光
                } else {
                    ldir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);  // 点光源
                }

                // 3. 计算视线方向
                float3 vdir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                // 4. 计算向量点积（修正冗余归一化）
                float ndotl = dot(ndirWS, ldir);
                float vdotn = dot(ndirWS, vdir);  // 用修正后的法线
                float3 lrdir = reflect(-ldir, ndirWS);  // 修正入射方向（-ldir）
                float vdotr = dot(vdir, lrdir);

                // 5. 采样纹理（应用ST参数）
                float3 basetex = tex2D(_BaseTex, i.uv).rgb;
                float3 emissiontex = tex2D(_EmissionTex, i.uv).rgb;
                float3 aotex = tex2D(_AOTex, i.uv).rgb;
                float3 matcaptex = tex2D(_MatCapTex, (ndirVS.xy * 0.5 + 0.5)).rgb;  // 明确二维坐标
                float metallic = tex2D(_MetallicTex, i.uv).r;  // 金属度取R通道

                // 6. 光源漫反射与镜面反射
                float3 basecol = basetex * _BaseColor.rgb;
                float3 lambert = max(0.0, ndotl);  // 漫反射
                float specularpow = max(metallic * _GlossStrength, 1.0);  // 限制最小幂次
                float3 phong = pow(max(0.0, vdotr), specularpow) * metallic;  // 镜面反射（受金属度控制）

                // 7. 阴影计算
                float shadow = SHADOW_ATTENUATION(i);  // 正确获取阴影
                float3 lighting = (basecol * lambert + phong) * _LightColor * shadow;

                // 8. 环境漫反射（修正掩码逻辑）
                float3 maskcol=envdiffusecol(_EnvUpColor,_EnvDownColor,_EnvralColor,i.normalWS);
                float3 envdiffcol = maskcol * basecol;

                // 9. 菲涅尔项（补充）
                float frenel = pow(1.0 - max(vdotn, 0.0), _frenelpow) * _FrenelStrength;

                // 10. 环境镜面反射
                float3 envspeccol = matcaptex * frenel * _EnvSpecStrength;

                // 11. 自发光
                float3 emission = emissiontex * _EmissionColor * _EmissionStrength;

                // 12. 最终颜色（整合所有项）
                float3 finalcol = (lighting + envdiffcol + envspeccol) * aotex + emission;

                // 应用雾效
                UNITY_APPLY_FOG(i.fogCoord, finalcol);

                return fixed4(finalcol, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"  // 增加 fallback 确保兼容性
}