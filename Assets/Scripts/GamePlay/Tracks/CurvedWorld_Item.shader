Shader "FutureCity/CurvedWorld_Item"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)
        
        _Cutoff("Alpha Cutout", Range(0.0, 1.0)) = 0.5
        
        [Header(Animation)]
        _RotationSpeed("Auto Rotation Speed", Float) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="AlphaTest" 
            "IgnoreProjector"="True"
            "DisableBatching"="True" // QUAN TRỌNG: Ngăn chặn lỗi gom cụm tọa độ gây bay lơ lửng
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float fogCoord      : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _RotationSpeed;
            CBUFFER_END

            // Tham số bẻ cong toàn cục từ WorldCurver.cs
            float4 _CurveParams;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                // 1. CHẾ ĐỘ TỰ XOAY (AUTO-ROTATION)
                // Xoay trong Object Space trước khi chuyển sang World Space
                float3 posOS = input.positionOS.xyz;
                float3 normOS = input.normalOS;

                if (abs(_RotationSpeed) > 0.01)
                {
                    float angle = _Time.y * _RotationSpeed;
                    float s, c;
                    sincos(angle, s, c);
                    
                    // Ma trận xoay quanh trục Y
                    float3x3 rotMatrix = float3x3(
                        c, 0, s,
                        0, 1, 0,
                        -s, 0, c
                    );
                    
                    posOS = mul(rotMatrix, posOS);
                    normOS = mul(rotMatrix, normOS);
                }

                // 2. CHUYỂN LOCAL -> WORLD
                float3 positionWS = TransformObjectToWorld(posOS);

                // 3. TÍNH TOÁN BẺ CONG (CURVATURE)
                float dist = positionWS.z - _WorldSpaceCameraPos.z;
                dist = max(0, dist - _CurveParams.z);
                
                positionWS.y -= dist * dist * _CurveParams.y;
                positionWS.x -= dist * dist * _CurveParams.x;

                // 4. CHUYỂN WORLD -> CLIP
                output.positionCS = TransformWorldToHClip(positionWS);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(normOS);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // 1. SAMPLE TEXTURE & ALPHA TEST
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = texColor * _BaseColor;
                
                // Alpha Cutout
                clip(color.a - _Cutoff);
                
                // 2. ÁNH SÁNG CƠ BẢN (SIMPLE LIT)
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                
                // Mix Diffuse + Ambient
                half3 ambient = half3(0.4, 0.4, 0.4); // Ambient hơi sáng cho item nổi bật
                half3 diffuse = mainLight.color * NdotL;

                color.rgb *= (diffuse + ambient);
                
                // 3. SƯƠNG MÙ
                color.rgb = MixFog(color.rgb, input.fogCoord);

                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
