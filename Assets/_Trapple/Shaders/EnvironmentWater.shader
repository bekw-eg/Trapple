Shader "Trapple/Sprites/Environment Water Shimmer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shimmer ("Strength / Speed", Vector) = (.018,.55,0,0)
        // Conservative interior of the lake in the unchanged 1942 x 809 scenery plate.
        _WaterRegion ("Lake Interior UV Rectangle", Vector) = (.451,.391,.112,.023)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
            #pragma fragment WaterFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _Shimmer, _WaterRegion;
            fixed4 WaterFrag(v2f input) : SV_Target
            {
                // Shade a small patch of painted water, without displacing any scenery pixels.
                float2 lake = (input.texcoord - _WaterRegion.xy) / _WaterRegion.zw;
                float mask = smoothstep(0,.2,lake.x) * smoothstep(0,.2,1-lake.x);
                mask *= smoothstep(0,.25,lake.y) * smoothstep(0,.25,1-lake.y);
                float ripple = sin(lake.y * 55 + sin(lake.x * 13 + _Time.y * _Shimmer.y) + _Time.y * _Shimmer.y);
                float sparkle = pow(saturate(ripple), 8) * (.5 + .5 * sin(lake.x * 35 - _Time.y * .4));
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.rgb += fixed3(.75,.9,1) * sparkle * mask * _Shimmer.x;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
