Shader "Trapple/Sprites/Workshop Door"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _AlphaFloor ("Background alpha floor", Range(0,1)) = .78
        _AlphaCeiling ("Solid alpha", Range(0,1)) = .98
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #include "UnitySprites.cginc"
            float _AlphaFloor, _AlphaCeiling;
            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color=SampleSpriteTexture(input.texcoord)*input.color;
                // The supplied sheet has a translucent dark matte outside the house.
                // Remove it at draw time; keep the original PNG byte-for-byte unchanged.
                color.a=smoothstep(_AlphaFloor,max(_AlphaFloor+.001,_AlphaCeiling),color.a);
                color.rgb*=color.a; return color;
            }
            ENDCG
        }
    }
}
