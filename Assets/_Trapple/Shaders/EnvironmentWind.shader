Shader "Trapple/Sprites/Environment Wind"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Wind ("Speed / Strength / Frequency / Phase", Vector) = (1.2, .004, 5, 0)
        _FixedBase ("Fixed Base Height", Range(0,.95)) = .15
        _SpriteUV ("Sprite UV Rectangle", Vector) = (0,0,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        // Explicit unatlased rectangles: do not allow atlas rotation/tight packing to change UV axes.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment WindFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _Wind, _SpriteUV;
            float _FixedBase;
            fixed4 WindFrag(v2f input) : SV_Target
            {
                float2 local = (input.texcoord - _SpriteUV.xy) / _SpriteUV.zw;
                float anchored = smoothstep(_FixedBase, 1, local.y);
                // Ease to zero at both sides, so even a Full Rect quad never needs extra geometry.
                float edge = smoothstep(0, .12, local.x) * smoothstep(0, .12, 1 - local.x);
                float wave = sin(_Time.y * _Wind.x + local.y * _Wind.z + _Wind.w);
                wave += .3 * sin(_Time.y * _Wind.x * .61 + local.y * _Wind.z * 1.7 + _Wind.w);
                local.x += wave * _Wind.y * anchored * anchored * edge;
                float2 uv = _SpriteUV.xy + local * _SpriteUV.zw;
                fixed4 color = SampleSpriteTexture(uv) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
