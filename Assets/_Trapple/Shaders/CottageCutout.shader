Shader "Trapple/Sprites/Cottage Cutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _CutoutTex ("Exterior Cutout", 2D) = "white" {}
        _CutoutThreshold ("Cutout Threshold", Range(0,1)) = 0.5
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        // The mask uses the full, unatlased source canvas UVs.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment CutoutFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            sampler2D _CutoutTex;
            fixed _CutoutThreshold;
            fixed4 CutoutFrag(v2f input) : SV_Target
            {
                fixed4 color=SampleSpriteTexture(input.texcoord)*input.color;
                // Keep source RGB and crisp source pixels; use only the generated cutout's alpha.
                color.a*=step(_CutoutThreshold,tex2D(_CutoutTex,input.texcoord).a);
                color.rgb*=color.a;
                return color;
            }
            ENDCG
        }
    }
}
