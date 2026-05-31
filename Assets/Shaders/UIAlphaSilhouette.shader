Shader "GameKari/UIAlphaSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _OutlineColor ("Outline Color", Color) = (0.25, 0.25, 0.25, 1)
        _OutlineSize ("Outline Size", Float) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                float alpha = tex.a;

                float2 offset = _MainTex_TexelSize.xy * max(0.0, _OutlineSize);
                float neighborAlpha = 0.0;
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, 0)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(0,  offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x,  offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x,  offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2( offset.x, -offset.y)).a);
                neighborAlpha = max(neighborAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, -offset.y)).a);

                // Body uses fill color. Only the outside area around body uses outline color.
                float outlineOnlyAlpha = saturate(neighborAlpha - alpha);
                float visibleAlpha = max(alpha, outlineOnlyAlpha);

                fixed4 color = alpha > 0.001 ? IN.color : _OutlineColor;
                color.a *= IN.color.a;
                color.a *= visibleAlpha;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return color;
            }            ENDCG
        }
    }
}



