Shader "Custom/ColorOutline"
{
    Properties
    {
        // НАСТРОЙКИ ОБВОДКИ
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)  // Красная обводка
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02  // Толщина
        
        // НАСТРОЙКИ ОСНОВНОГО ЦВЕТА (если нужен)
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _OutlineOnly ("Outline Only", Range(0, 1)) = 0  // Только обводка?
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        
        // ПРОХОД 1: ОБВОДКА (рисуется ПЕРВЫМ)
        Pass
        {
            Name "OUTLINE"
            Cull Front  // Рендерим только обратную сторону (для обводки)
            ZWrite Off  // Не пишем в буфер глубины
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
            float _OutlineWidth;
            fixed4 _OutlineColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // Простое расширение вершин вдоль нормали
                float3 normal = normalize(v.normal);
                float4 pos = v.vertex;
                pos.xyz += normal * _OutlineWidth;  // Расширяем
                
                o.vertex = UnityObjectToClipPos(pos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Возвращаем ЧИСТЫЙ ЦВЕТ без текстур
                return _OutlineColor;
            }
            ENDCG
        }
        
        // ПРОХОД 2: ОСНОВНОЙ ЦВЕТ ОБЪЕКТА
        Pass
        {
            Name "MAIN"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
            fixed4 _MainColor;
            float _OutlineOnly;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Если включен режим "только обводка" - делаем невидимым
                if (_OutlineOnly > 0.5)
                    discard;
                
                return _MainColor;  // Чистый цвет объекта
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}