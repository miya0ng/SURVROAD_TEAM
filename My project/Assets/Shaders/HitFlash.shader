Shader "Custom/HitFlash"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _FlashColor;
        float _FlashAmount;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex);

            // 원래 텍스처 색상과 FlashColor를 FlashAmount 비율로 섞음
            fixed3 finalColor = lerp(texColor.rgb, _FlashColor.rgb, _FlashAmount);

            o.Albedo = finalColor;
            o.Alpha = texColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
