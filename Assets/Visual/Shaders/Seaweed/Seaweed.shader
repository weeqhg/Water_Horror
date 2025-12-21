Shader "Custom/SeaweedSimpleTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.3, 0.7, 0.3, 1)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveAmount ("Wave Amount", Float) = 0.1
        _WaveHeight ("Wave Height Power", Range(0.1, 10)) = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

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
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _WaveSpeed;
                float _WaveAmount;
                float _WaveHeight;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Простой фактор: Y=0 - низ (нет движения), Y>0 - верх (движение)
                // clamp ограничивает от 0 до 1
                float heightFactor = clamp(input.positionOS.y, 0.0, 1.0);
                
                // Усиливаем - вверху движение сильнее
                heightFactor = pow(heightFactor, _WaveHeight);
                
                // Время
                float time = _Time.y * _WaveSpeed;
                
                // Две волны разной частоты для естественности
                float wave1 = sin(time + input.positionOS.x) * 0.7;
                float wave2 = sin(time * 1.3 + input.positionOS.x * 2.0) * 0.3;
                
                // Общая волна с учетом высоты
                float wave = (wave1 + wave2) * _WaveAmount * heightFactor;
                
                // Применяем
                input.positionOS.x += wave;
                input.positionOS.z += wave * 0.2; // Немного вперед/назад
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                
                if (col.a < 0.1) discard;
                
                col.rgb = MixFog(col.rgb, input.fogCoord);
                
                return col;
            }
            ENDHLSL
        }
    }
}