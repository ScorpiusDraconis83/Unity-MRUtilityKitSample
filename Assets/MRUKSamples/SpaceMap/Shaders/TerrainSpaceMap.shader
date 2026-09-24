// Copyright(c) Meta Platforms, Inc. and affiliates.
// All rights reserved.
//
// Licensed under the Oculus SDK License Agreement (the "License");
// you may not use the Oculus SDK except in compliance with the License,
// which is provided at the time of installation or download, or which
// otherwise accompanies this software in either electronic or hard copy form.
//
// You may obtain a copy of the License at
//
// https://developer.oculus.com/licenses/oculussdk/
//
// Unless required by applicable law or agreed to in writing, the Oculus SDK
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

Shader "Meta/MRUK/TerrainSpaceMap"
{
    Properties
    {
        _TerrainTex ("Terrain Texture", 2D) = "white" {}
        _GrassMask ("Grass Mask", 2D) = "white" {}
        _DirtTex ("Dirt Texture", 2D) = "white" {}
        _SpaceMap ("Space Map", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Tags {"Queue"="Opaque" "LightMode"="UniversalForward"}


            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float4 diff : COLOR0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
            };

            sampler2D _TerrainTex;
            sampler2D _GrassMask;
            sampler2D _DirtTex;
            sampler2D _SpaceMap;

            CBUFFER_START(UnityPerMaterial)
            float4 _TerrainTex_ST;
            float4 _GrassMask_ST;
            float4 _DirtTex_ST;
            float4x4 _SpaceMapProjectionViewMatrix;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv.xy = TRANSFORM_TEX (v.uv, _TerrainTex);
                o.uv.zw = TRANSFORM_TEX (v.uv, _DirtTex);

                half3 worldNormal = TransformObjectToWorld(v.normal);
                const half nl = max(0, dot(worldNormal, _MainLightPosition.xyz));
                o.diff = nl * _MainLightColor;
                const float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos;

                // in addition to the diffuse lighting from the main light,
                // add illumination from ambient or light probes
                // ShadeSH9 function from UnityCG.cginc evaluates it,
                // using world space normal
                o.diff.rgb += SampleSHVertex(worldNormal);
                return o;
            }


            half4 frag (v2f i) : SV_Target
            {
                float4 clipPos = mul(_SpaceMapProjectionViewMatrix, i.worldPos);
                clipPos /= clipPos.w;
                float2 uv = clipPos.xy * 0.5 + 0.5;

                const half4 colSpaceMap = tex2D(_SpaceMap, uv);

                const float4 terrain = tex2D(_TerrainTex, i.uv.xy);
                const float4 grass = tex2D(_GrassMask, i.worldPos.xz);
                const float4 dirt = tex2D(_DirtTex, i.uv.zw) * colSpaceMap.r;


                float4 final = lerp(terrain * grass, dirt, colSpaceMap.r);
                final.a = 1;
                return final;
            }

            ENDHLSL
        }
    }
}
