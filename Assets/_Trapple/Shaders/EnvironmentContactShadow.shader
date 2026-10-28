Shader "Trapple/Sprites/Environment Contact Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SpriteUV ("Sprite UV Rectangle", Vector) = (0,0,1,1)
        _Feather ("Soft Edge Width", Range(.1,1)) = .85
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            float4 _SpriteUV;
            float _Feather;
            fixed4 ShadowFrag(v2f input) : SV_Target
            {
                float2 footprint = ((input.texcoord - _SpriteUV.xy) / _SpriteUV.zw - .5) * 2;
                // A broad soft footprint: the exposed sides remain visible around Appi's feet.
                // Reusing the narrow lantern glow's squared alpha hid almost all of the shadow.
                float coverage = smoothstep(0, _Feather, saturate(1 - dot(footprint, footprint)));
                fixed4 color = input.color;
                color.a *= coverage;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
