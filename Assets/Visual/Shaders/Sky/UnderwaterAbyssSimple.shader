Shader "Skybox/UnderwaterAbyssSimple"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.1, 0.3, 0.5, 1)
        _DeepColor ("Deep Color", Color) = (0.0, 0.05, 0.15, 1)
        _SunDirection ("Sun Direction", Vector) = (0.3, 0.8, 0.2, 0)
        _SunColor ("Sun Color", Color) = (1.0, 0.9, 0.7, 1)
        _RayIntensity ("Ray Intensity", Range(0, 5)) = 2.0
    }
    
    SubShader
    {
        Tags { 
            "Queue"="Background" 
            "RenderType"="Background" 
            "PreviewType"="Skybox" 
        }
        Cull Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            half3 _ShallowColor;
            half3 _DeepColor;
            half3 _SunDirection;
            half3 _SunColor;
            half _RayIntensity;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.texcoord);
                float depth = saturate(-viewDir.y);
                
                // Градиент цвета
                half3 waterColor = lerp(_ShallowColor, _DeepColor, depth);
                
                // Солнечные лучи
                float3 sunDir = normalize(_SunDirection);
                float sunDot = saturate(dot(viewDir, sunDir));
                float rays = pow(sunDot, 8.0) * _RayIntensity;
                
                // Финальный цвет
                half3 finalColor = waterColor + rays * _SunColor;
                
                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    
    Fallback "Skybox/Cubemap"
}