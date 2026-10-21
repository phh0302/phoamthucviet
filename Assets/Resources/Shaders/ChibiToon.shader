// Shader hoạt hình cho nhân vật chibi 3D dựng bằng code (Visual/Chibi.cs).
// - Màu lấy từ màu đỉnh (vertex color), tô 2 bậc sáng/tối kiểu toon, có ánh viền nhẹ.
// - Pass thứ 2 vẽ viền đen bằng cách nới mặt sau ra theo pháp tuyến.
//   Kênh alpha của màu đỉnh = có vẽ viền hay không (mắt, má, miệng không cần viền).
// - Không cần đèn trong cảnh. Nằm ở hàng Transparent để xen đúng thứ tự với sprite 2D (sortingOrder).
Shader "PhoAmThuc/ChibiToon"
{
    Properties
    {
        _Outline ("Độ dày viền (đơn vị thế giới)", Float) = 0.010
        _OutlineColor ("Màu viền", Color) = (0.36, 0.27, 0.24, 1)
        _Shadow ("Độ tối vùng khuất", Range(0, 1)) = 0.84
        _Saturation ("Độ rực màu", Range(0, 1)) = 0.8
        _Lift ("Làm sáng nhẹ (pastel)", Range(0, 0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float _Outline;
            float4 _OutlineColor;
            float _Shadow;
            float _Saturation;
            float _Lift;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float3 outlineOS : TEXCOORD1; };
        ENDHLSL

        Pass
        {
            Name "Toon"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; half4 color : COLOR; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                // Đèn giả cố định: chiếu từ trên, bên trái, phía trước (camera nhìn theo +z)
                float3 lightDir = normalize(float3(-0.45, 0.75, -0.55));
                float d = dot(n, lightDir) * 0.5 + 0.5;
                float lit = lerp(_Shadow, 1.0, smoothstep(0.35, 0.60, d)); // chuyển sáng/tối mềm, đỡ gắt
                float rim = pow(saturate(1.0 + n.z), 4.0) * 0.08; // ánh viền ở mép hình
                float3 col = i.color.rgb * lit + rim;
                // Phong cách dịu mắt: bớt rực, sáng nhẹ kiểu pastel
                float l = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(float3(l, l, l), col, _Saturation);
                col = lerp(col, float3(1, 1, 1), _Lift);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings { float4 positionCS : SV_POSITION; half mask : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                // Hướng nới viền riêng (khối hộp nới từ tâm ra góc); không có thì dùng pháp tuyến
                float3 dirOS = dot(i.outlineOS, i.outlineOS) > 0.01 ? i.outlineOS : i.normalOS;
                float3 nWS = normalize(TransformObjectToWorldNormal(dirOS));
                posWS += nWS * _Outline * i.color.a;
                o.positionCS = TransformWorldToHClip(posWS);
                o.mask = i.color.a;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(i.mask - 0.5);
                return half4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
