Shader "UI/CRT Effect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlickerIntensity ("Flicker Intensity", Range(0, 0.1)) = 0.02
        _ScanLineSize ("Scan Line Size", Range(1, 10)) = 2
        _ScanLineSpeed ("Scan Line Speed", Range(0, 100)) = 30
        _NoiseIntensity ("Noise Intensity", Range(0, 0.1)) = 0.02
        _Curvature ("Screen Curvature", Range(0, 0.1)) = 0.02
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            float _FlickerIntensity;
            float _ScanLineSize;
            float _ScanLineSpeed;
            float _NoiseIntensity;
            float _Curvature;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }
            
            sampler2D _MainTex;
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // Эффект кривизны экрана
                float2 uv = IN.texcoord - 0.5;
                uv *= 1.0 - dot(uv, uv) * _Curvature;
                uv += 0.5;
                
                // Scan lines
                float scanLines = sin(uv.y * _ScreenParams.y / _ScanLineSize + _Time.y * _ScanLineSpeed) * 0.5 + 0.5;
                scanLines = 1.0 - scanLines * 0.05;
                
                // Шум
                float noise = frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
                noise = noise * 2.0 - 1.0;
                noise *= _NoiseIntensity;
                
                // Мерцание
                float flicker = sin(_Time.y * 60.0) * _FlickerIntensity;
                
                // Итоговый цвет
                half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * IN.color;
                color.rgb *= scanLines;
                color.rgb += noise;
                color.rgb *= (1.0 + flicker);
                
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                return color;
            }
            ENDCG
        }
    }
}