Shader "Portals/Outline"
{
    Properties
    {
		_OutlineColour("Outline Colour", Color) = (1, 1, 1, 1)
		_MaskID("Mask ID", Int) = 1
		// Size of the portal oval relative to this outline quad (the outline is a child of the
		// portal scaled 1.2 x 1.1, so the portal itself is 1/1.2 x 1/1.1 of it).
		_InnerScale("Inner Scale", Vector) = (0.8333, 0.9091, 0, 0)
		_RimWidth("Rim Width (m)", Float) = 0.035
		_GlowWidth("Outer Glow Width (m)", Float) = 0.08
		// 0..1, driven by Portal.cs so the oval grows open after being placed.
		_Open("Open", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags
		{
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		ENDHLSL

		// Never draws over the portal view itself (stencil 1, written by PortalMask). While the
		// other portal isn't placed yet the mask renderer is disabled, so the inside of the oval
		// gets this shader's swirling fill instead - same as an unlinked portal in Portal 2.
		Stencil
		{
			Ref 0
			Comp equal
		}

        Pass
        {
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                // Position on the quad in metres, and the oval's radii in metres.
                float2 pos : TEXCOORD0;
                float2 radii : TEXCOORD1;
            };

			uniform float4 _OutlineColour;
			uniform float4 _InnerScale;
			uniform float _RimWidth;
			uniform float _GlowWidth;
			uniform float _Open;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);

                // World size of this quad, so rim/glow widths stay in metres whatever the scale.
                float2 size = float2(length(unity_ObjectToWorld._m00_m10_m20),
                                     length(unity_ObjectToWorld._m01_m11_m21));
                o.pos = (v.uv - 0.5) * size;
                o.radii = max(0.5 * size * _InnerScale.xy * _Open, 0.001);
                return o;
            }

			float hash(float2 p)
			{
				return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
			}

			float noise(float2 p)
			{
				float2 i = floor(p);
				float2 f = frac(p);
				f = f * f * (3.0 - 2.0 * f);
				return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
							lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
			}

			float fbm(float2 p)
			{
				return noise(p) * 0.6 + noise(p * 2.1 + 3.7) * 0.3 + noise(p * 4.3 + 9.1) * 0.1;
			}

            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 q = i.pos / i.radii;
                float k0 = length(q);
                float k1 = length(i.pos / (i.radii * i.radii));
                // Approximate distance to the oval in metres (negative = inside).
                float d = k0 * (k0 - 1.0) / max(k1, 0.0001);

                float ang = atan2(q.y, q.x);
                float2 ring = float2(cos(ang), sin(ang));

                // Rim: a bright band whose thickness flickers around the edge.
                float n = fbm(ring * 2.5 + float2(t * 0.7, -t * 0.55));
                float rimWidth = _RimWidth * (0.6 + 0.8 * n);
                float rim = 1.0 - smoothstep(0.0, rimWidth, abs(d - rimWidth * 0.3));

                // Soft glow fading outward, gone well before the quad's edge so no rectangle shows.
                float outside = max(d, 0.0);
                float glow = exp(-outside / (_GlowWidth * 0.35)) * (1.0 - smoothstep(_GlowWidth * 0.7, _GlowWidth, outside));
                glow *= 0.55 + 0.45 * fbm(ring * 4.0 + float2(-t * 1.1, t * 0.8) + outside * 30.0);

                float3 rimColour = lerp(_OutlineColour.rgb, float3(1, 1, 1), 0.45 * rim);

                if (d < 0.0)
                {
                    // Unlinked portal: swirling fill (only visible when PortalMask isn't drawing).
                    float swirlAng = ang + k0 * 3.5 - t * 1.6;
                    float s = fbm(float2(cos(swirlAng), sin(swirlAng)) * 2.5 * k0 + t * 0.25);
                    float3 fill = _OutlineColour.rgb * (0.35 + 0.9 * s + 0.5 * k0 * k0);
                    return float4(lerp(fill, rimColour, rim), 1.0);
                }

                float alpha = saturate(rim + glow * 0.7);
                return float4(rimColour, alpha);
            }
            ENDHLSL
        }
    }
}
