Shader "FutureCity/CurvedWorld_Mobile"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float fogCoord      : TEXCOORD2; // Sương mù tương lai
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
            CBUFFER_END

            // Đọc thông số bẻ cong từ script WorldCurver.cs
            float4 _CurveParams;

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Chuyển vị trí từ Local -> World
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // TÍNH TOÁN BẺ CONG (CURVATURE)
                float dist = positionWS.z - _WorldSpaceCameraPos.z;
                dist = max(0, dist - _CurveParams.z);
                positionWS.y -= dist * dist * _CurveParams.y;
                positionWS.x -= dist * dist * _CurveParams.x;

                // Cập nhật vị trí lên màn hình sau khi bẻ cong
                output.positionCS = TransformWorldToHClip(positionWS);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Màu gốc từ texture nhân với hệ màu
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Ánh sáng cơ bản (tương tự Simple Lit) của mặt trời
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 ambient = half3(0.5, 0.5, 0.5); // Ánh sáng môi trường cố định
                half3 diffuse = mainLight.color * NdotL;

                color.rgb *= (diffuse + ambient);
                
                // Màn sương tương lai
                color.rgb = MixFog(color.rgb, input.fogCoord);

                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
