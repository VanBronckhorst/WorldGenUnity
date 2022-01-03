Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture", 2D) = "white"{}
        testScale("Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8;
    const static float epsilon = 1E-4;

        int layerCount;
        float3 baseColors[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColorsStrength[maxLayerCount];
        float baseTexturesScale[maxLayerCount];

        float minHeight;
        float maxHeight;

        sampler2D testTexture;
        float testScale;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        float inverseLerp(float a, float b, float val) {
            return saturate((val - a) / (b - a));
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            float3 scaledWorldPos = worldPos / scale;

            float3 xProj = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProj = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProj = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProj + yProj + zProj;
        }

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            for (int i = 0; i < layerCount; i++)
            {
                float drawStrength = inverseLerp(-baseBlends[i]/ 2 - epsilon, baseBlends[i]/2 + epsilon,heightPercent - baseStartHeights[i]);
                float3 baseColor = baseColors[i] * baseColorsStrength[i];
                float3 texColor = triplanar(IN.worldPos, baseTexturesScale[i], blendAxes, i) * (1- baseColorsStrength[i]);
                o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + texColor) * drawStrength;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
