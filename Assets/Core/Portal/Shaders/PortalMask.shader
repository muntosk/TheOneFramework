Shader "Portals/PortalMask"
{
    Properties
    {
		_MainTex("Main Texture", 2D) = "white" {}
		_OutlineColour("Outline Colour", Color) = (1, 1, 1, 1)
		_EdgeGlow("Edge Glow", Range(0, 1)) = 0.7
		// 0..1, driven by Portal.cs so the oval grows open after being placed.
		_Open("Open", Range(0, 1)) = 1
    }
    SubShader
    {
		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		ENDHLSL

        Pass
        {
			Name "Mask"

			// Pixels outside the oval are clipped before this runs, so the stencil (and with it
			// the outline's "don't draw over the portal view" test) also follows the oval shape.
			Stencil
			{
				Ref 1
				Pass replace
			}

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
					float4 screenPos : TEXCOORD0;
					float2 uv : TEXCOORD1;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = TransformObjectToHClip(v.vertex.xyz);
					o.screenPos = ComputeScreenPos(o.vertex);
					o.uv = v.uv;
					return o;
				}

				uniform sampler2D _MainTex;
				uniform float4 _OutlineColour;
				uniform float _EdgeGlow;
				uniform float _Open;

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

				float4 frag(v2f i) : SV_Target
				{
					// Quad UVs -> -1..1, so the unit circle here is the oval filling the quad.
					float2 p = (i.uv - 0.5) * 2.0;
					float r = length(p) / max(_Open, 0.001);
					clip(1.0 - r);

					float2 uv = i.screenPos.xy / i.screenPos.w;
					float4 col = tex2D(_MainTex, uv);

					// Coloured haze bleeding in from the rim, like Portal 2's inner edge.
					float ang = atan2(p.y, p.x);
					float n = noise(float2(cos(ang), sin(ang)) * 3.0 + _Time.y * float2(0.6, -0.5));
					float glow = smoothstep(0.78 - 0.08 * n, 1.0, r) * _EdgeGlow;
					col.rgb = lerp(col.rgb, _OutlineColour.rgb, glow);
					return col;
				}
			ENDHLSL
        }
    }
}
