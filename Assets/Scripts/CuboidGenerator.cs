using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarchingCubes))]
public class CuboidGenerator : MonoBehaviour
{
    [SerializeField]
    public CuboidDensity cuboidDensityGenerator;

    MarchingCubes marchingCubes;

    void Start()
    {
        marchingCubes = GetComponent<MarchingCubes>();
        marchingCubes.DensityGenerator = cuboidDensityGenerator;
        marchingCubes.SetupAndGenerateObject();
    }
}

[System.Serializable]
public class CuboidDensity : Density
{
    [Header("Cube size")]
    public Vector3 scale;
    public bool addNoise;
    [SerializeField]
    [Header("Frequency around 0.05 and amplitude of 1 is a good starting point")]
    public NoiseData noiseData;

    public override Vector3 GetBounds()
    {
        return scale;
    }

    public override float GetValue(int x, int y, int z)
    {
        Vector3 bounds = GetBounds();
        float cubeDens = x > 1 && x < bounds.x - 2 && y > 1 && y < bounds.y - 2 && z > 1 && z < bounds.z - 2 ? 1 : 0;
        float noise = HelperFunctions.Noise3D(x, y, z, noiseData.frequency, noiseData.amplitude, noiseData.seed);
        return Mathf.Clamp(cubeDens - (addNoise ? noise : 0), 0f, 1f);
    }
}
