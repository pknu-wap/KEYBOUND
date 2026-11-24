Shader "KEYBOUND/UI/Tutorial Key Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Key Sprite", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            VertexOutput vert(appdata_full input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord.xy;
                output.color = input.color;
                return output;
            }

            half Coverage(float2 uv)
            {
                return smoothstep(0.2h, 0.8h, tex2D(_MainTex, uv).a);
            }

            half4 frag(VertexOutput input) : SV_Target
            {
                float2 uv = input.uv;
                float2 radius = 8.0 * _MainTex_TexelSize.xy;
                float2 horizontal = float2(radius.x, 0.0);
                float2 vertical = float2(0.0, radius.y);
                float2 diagonal = radius * 0.7071;
                float2 oppositeDiagonal = float2(diagonal.x, -diagonal.y);

                // Subtract the eroded silhouette to keep the border inside the sprite.
                half interior = min(Coverage(uv + horizontal), Coverage(uv - horizontal));
                interior = min(interior, min(Coverage(uv + vertical), Coverage(uv - vertical)));
                interior = min(interior, min(Coverage(uv + diagonal), Coverage(uv - diagonal)));
                interior = min(interior, min(Coverage(uv + oppositeDiagonal), Coverage(uv - oppositeDiagonal)));

                half silhouette = Coverage(uv);
                half outline = silhouette - min(silhouette, interior);
                return half4(input.color.rgb, outline * input.color.a);
            }
            ENDCG
        }
    }
}
