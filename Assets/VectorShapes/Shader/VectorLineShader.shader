// Roughly based on
// https://blog.mapbox.com/drawing-antialiased-lines-with-opengl-8766f34192dc
// and
// https://mattdesl.svbtle.com/drawing-lines-is-hard
Shader "Unlit/VectorLineShader"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (0, 0, 0, 1)
        _LineWidth ("Line Width", Float) = 1
        _Feather ("Feather", Range (0, 1)) = 0.25
        _PixelScale ("Pixel Scale", Float) = 0.1
        
        _StripeColor ("Stripe Color", Color) = (1, 1, 1, 1)
        _StripeGap ("Stripe Gap", Float) = 10
        _StripeWidth ("Stripe Width", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
             #pragma multi_compile STRIPES_OFF STRIPES_ON

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 offset : TEXCOORD0;
            };

            fixed4 _LineColor;
            half _LineWidth;
            half _Feather;
            half _PixelScale;
            
            fixed4 _StripeColor;
            half _StripeWidth;
            half _StripeGap;
            
            static half _StripeLength = _StripeWidth + _StripeGap;

            v2f vert (appdata v)
            {
                v2f o;
                float3 normal = float3(v.normal.zw, 0.0);
                float3 pos = UnityObjectToViewPos(v.vertex);
                float3 delta = normal * _LineWidth * _PixelScale;
                o.vertex = UnityViewToClipPos(pos + delta);
                o.offset.x = abs(v.normal.x);
                o.offset.y = v.normal.y;
                
                return o;
            }

            fixed4 frag (v2f f) : SV_Target
            {
            #if STRIPES_ON
                float distX = fmod(f.offset.x, _StripeLength);
                float stripe = saturate(abs((_StripeGap - distX) / _StripeWidth) * 2.0);
                float4 color = lerp(_StripeColor, _LineColor, stripe);
            #else
                float4 color = _LineColor;
            #endif
                
                float distY = abs(f.offset.y);
                float alpha = saturate((1.0 - distY) / _Feather);

                return float4(color.rgb, alpha * color.a);
            }
            ENDCG
        }
    }
}
