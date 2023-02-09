using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseData
{
    public float frequency;
    public float amplitude;
    public int seed;
}

public class ChunkObject
{
    public GameObject chunk;
    public Vector3Int position;

    public ChunkObject(GameObject chunk, Vector3Int position)
    {
        this.chunk = chunk;
        this.position = position;
    }
}

public struct Triangle
{
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3 this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
    }
}

public class HelperFunctions
{
    //simple perlin 3d perlin noise function
    public static float Noise3D(float x, float y, float z, float frequency, float amplitude, int seed)
    {
        float xy = Mathf.PerlinNoise(x * frequency + seed, y * frequency + seed) * amplitude;
        float xz = Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed) * amplitude;
        float yz = Mathf.PerlinNoise(y * frequency + seed, z * frequency + seed) * amplitude;
        float yx = Mathf.PerlinNoise(y * frequency + seed, x * frequency + seed) * amplitude;
        float zx = Mathf.PerlinNoise(z * frequency + seed, x * frequency + seed) * amplitude;
        float zy = Mathf.PerlinNoise(z * frequency + seed, y * frequency + seed) * amplitude;

        return (xy + xz + yz + yx + zx + zy) / 6.0f;
    }

    //round a vector3 to a certain number of decimal points
    public static Vector3 roundVecToNearestDec(Vector3 vec, int decpoints)
    {
        float x = vec.x;
        float y = vec.y;
        float z = vec.z;
        float factor = Mathf.Pow(10f, decpoints);

        return new Vector3(Mathf.Round(x * factor) / factor, (y * factor) / factor, (z * factor) / factor);
    }

    public static float sigmoid(float f, float a, float b, float c)
    {
        return (1 + c) / (1 + Mathf.Exp(-a * (f - b)));
    }
}
