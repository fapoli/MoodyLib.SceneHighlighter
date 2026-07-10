Shader "MoodyLib/HighlightOverlay" {
    Properties {
        _Color ("Overlay Color", Color) = (0, 0, 0, 0.75)
        _FeatherAmount ("Feather Amount", Range(0, 1)) = 0.15
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_HIGHLIGHTS 8

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _FeatherAmount;
            int _HighlightCount;
            // 2 float4 per quad: (A.xy, B.xy), (C.xy, D.xy) - corners in perimeter order.
            float4 _HighlightCorners[MAX_HIGHLIGHTS * 2];

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Projects p into the quad's local basis (using edge AB/AD as axes) and returns
            // normalized coordinates where the unit circle traces the ellipse inscribed in the quad.
            float2 EllipseCoords(float2 p, float2 a, float2 b, float2 d) {
                float2 u = b - a;
                float2 v = d - a;
                float2 rel = p - a;

                float det = u.x * v.y - u.y * v.x;
                float uCoord = (rel.x * v.y - rel.y * v.x) / det;
                float vCoord = (u.x * rel.y - u.y * rel.x) / det;

                return float2(uCoord, vCoord) * 2 - 1;
            }

            fixed4 frag(v2f i) : SV_Target {
                float alpha = 1;

                for (int idx = 0; idx < _HighlightCount; idx++) {
                    float4 ab = _HighlightCorners[idx * 2];
                    float4 cd = _HighlightCorners[idx * 2 + 1];

                    float2 coords = EllipseCoords(i.uv, ab.xy, ab.zw, cd.zw);
                    float dist = length(coords);
                    float feather = max(_FeatherAmount, 0.0001);
                    float holeAlpha = smoothstep(1 - feather, 1, dist);

                    alpha = min(alpha, holeAlpha);
                }

                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
