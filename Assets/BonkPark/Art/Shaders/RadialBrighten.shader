Shader "BonkPark/RadialBrighten"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Progress ("Progress", Range(0,1)) = 0
        _Softness ("Edge Softness", Float) = 0.45
        _Aspect ("Aspect", Float) = 1.7777
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float2 _Center;
            float _Progress;
            float _Softness;
            float _Aspect;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            // The picture dims to black and warms back up from the centre outward as _Progress rises; each pixel
            // ramps softly through _Softness so the middle isn't a hard disc but a glow swelling to full brightness.
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * i.color;

                float2 d = i.uv - _Center;
                d.x *= _Aspect;
                float dist = length(d);

                float2 far = float2(max(_Center.x, 1.0 - _Center.x) * _Aspect, max(_Center.y, 1.0 - _Center.y));
                float maxDist = length(far);

                float local = saturate((_Progress * (maxDist + _Softness) - dist) / _Softness);
                float bright = smoothstep(0.0, 1.0, local);

                col.rgb *= bright;
                return col;
            }
            ENDCG
        }
    }
}
