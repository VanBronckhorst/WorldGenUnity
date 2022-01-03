using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {
    
    public enum NormalizeMode { Local, Global }

    public static float[,] GenerateNoiseMap(int mapW, int mapH, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
        float[,] noiseMap = new float[mapW, mapH];

        System.Random prng = new System.Random(seed);
        Vector2[] octavesOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++) {
            float oX = prng.Next(-100000, 100000) + offset.x;
            float oY = prng.Next(-100000, 100000) - offset.y;
            octavesOffsets[i] = new Vector2(oX, oY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0) {
            scale = 0.00001f;
        }

        float maxLocalNoise = float.MinValue;
        float minLocalNoise = float.MaxValue;

        float halfW = mapW / 2f;
        float halfH = mapH / 2f;

        for (int y = 0; y < mapH; y++) {
            for (int x = 0; x < mapW; x++) {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i< octaves; i++) {
                    float sampleX = (x - halfW + octavesOffsets[i].x) / scale * frequency ;
                    float sampleY = (y - halfH + octavesOffsets[i].y) / scale * frequency ;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                noiseMap[x, y] = noiseHeight;
                maxLocalNoise = Mathf.Max(noiseHeight, maxLocalNoise);
                minLocalNoise = Mathf.Min(noiseHeight, minLocalNoise);
            }
        }

        for (int y = 0; y < mapH; y++) {
            for (int x = 0; x < mapW; x++) {
                if (normalizeMode == NormalizeMode.Local) {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoise, maxLocalNoise, noiseMap[x, y]);
                } else {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 2f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        

        return noiseMap;
    }
}
