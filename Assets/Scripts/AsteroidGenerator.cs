using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarchingCubes))]
public class AsteroidGenerator : MonoBehaviour
{
    [SerializeField]
    public AsteroidDensity asteroidDensityGenerator;

    MarchingCubes marchingCubes;

    void Start()
    {
        marchingCubes = GetComponent<MarchingCubes>();
        marchingCubes.DensityGenerator = asteroidDensityGenerator;
        marchingCubes.SetupAndGenerateObject();
    }
}

[System.Serializable]
public class AsteroidDensity : Density
{
    public float radius;
    [Header("Sphere density factor should be around 200")]
    public float sphereDensityFactor;
    public Vector3 scale;
    [SerializeField]
    [Header("Frequency around 0.05 and amplitude of 20 is a good starting point")]
    public NoiseData noiseData;

    public override Vector3 GetBounds()
    {
        //rouble radius plus max noise addition
        float scaleFactor = radius * 2.0f + noiseData.amplitude * 2.0f;
        return new Vector3(scale.x * scaleFactor, scale.y * scaleFactor, scale.z * scaleFactor);
    }

    public override float GetValue(int x, int y, int z)
    {
        //get sphere centre
        Vector3 bounds = GetBounds();
        float _x = x - (bounds.x / 2f);
        float _y = y - (bounds.y / 2f);
        float _z = z - (bounds.z / 2f);

        //get difference between r squared and distance from centre
        float r = radius * radius;
        float dist = (_x * _x) / (scale.x * scale.x) + (_y * _y) / (scale.y * scale.y) + (_z * _z) / (scale.z * scale.z);
        float sphereDens = r - dist;

        float noise = HelperFunctions.Noise3D(x, y, z, noiseData.frequency, noiseData.amplitude, noiseData.seed);

        //divide sphereDens to allow for better smoothing
        return Mathf.Clamp((sphereDens / sphereDensityFactor) + noise, 0f, 1f);
    }
}
