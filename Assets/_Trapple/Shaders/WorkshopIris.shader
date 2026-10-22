Shader "Trapple/UI/Workshop Iris"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Center ("Viewport center", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius in screen heights", Float) = 2
        _Aspect ("Screen aspect", Float) = 1.77778
        _Softness ("Edge softness", Float) = 0.012
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _Center;
            float _Radius, _Aspect, _Softness;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 delta=(i.uv-_Center.xy)*float2(_Aspect,1);
                float alpha=smoothstep(_Radius-max(0.0001,_Softness),_Radius,length(delta));
                // A negative radius covers even the exact center pixel.
                return fixed4(0,0,0,alpha);
            }
            ENDCG
        }
    }
}
