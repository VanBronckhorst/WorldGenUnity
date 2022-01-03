using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode normalizeMode;

    public int octaves;
    public int seed;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public float noiseScale;
    public Vector2 offset;

    protected override void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }

        if (octaves < 0) {
            octaves = 0;
        }

        base.OnValidate();
    }
}
