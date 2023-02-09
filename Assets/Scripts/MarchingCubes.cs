using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MarchingCubes : MonoBehaviour
{
    [Header("Default 0.5. Boundary between solid and not")]
    public float threshold;
    [Header("Default 16. Larger sizes will break")]
    public int chunkSize;
    public GameObject chunkPrefab;
    public Density DensityGenerator;

    [HideInInspector]
    public List<ChunkObject> chunkList;

    private Queue<int> chunkUpdateQueue;

    float[,,] allDensityData;

    // Start is called before the first frame update
    public void SetupAndGenerateObject()
    {
        //bounds should be equal to a whole number multiple of chunksize plus one
        Vector3Int bounds = Vector3Int.RoundToInt(DensityGenerator.GetBounds() / chunkSize) * chunkSize + Vector3Int.one;

        //set data at every point
        allDensityData = new float[bounds.x, bounds.y, bounds.z];
        for (int x = 0; x < bounds.x; x++)
        {
            for (int y = 0; y < bounds.y; y++)
            {
                for (int z = 0; z < bounds.z; z++)
                {
                    allDensityData[x, y, z] = DensityGenerator.GetValue(x, y, z);
                }
            }
        }

        //find chunks for each axis
        Vector3Int chunkCount = (bounds - Vector3Int.one);

        //list to keep track of all chunks, and queue of chunks to update
        chunkList = new List<ChunkObject>();
        chunkUpdateQueue = new Queue<int>();

        //instantiate all chunks
        int index = 0;
        for (int x = 0; x < chunkCount.x - 1; x += chunkSize)
        {
            for (int y = 0; y < chunkCount.y - 1; y += chunkSize)
            {
                for (int z = 0; z < chunkCount.z - 1; z += chunkSize)
                {
                    chunkList.Add(new ChunkObject(null, new Vector3Int(x, y, z)));
                    chunkUpdateQueue.Enqueue(index++);
                }
            }
        }

    }

    private void FixedUpdate()
    {
        //generate one chunk per frame
        if (chunkUpdateQueue.Count > 0)
        {
            int index = chunkUpdateQueue.Dequeue();
            ChunkObject updatee = chunkList[index];
            GenerateChunk(updatee.position.x, updatee.position.y, updatee.position.z, index);
        }
    }
    
    //initially generate chunk
    public void GenerateChunk(int x, int y, int z, int index)
    {
        //instantiate chunk gameobject
        chunkList[index] = new ChunkObject(Instantiate(chunkPrefab, new Vector3(x, y, z) + transform.position, Quaternion.identity, transform), new Vector3Int(x, y, z));

        //create mesh 
        Mesh mesh = new Mesh();
        chunkList[index].chunk.GetComponent<MeshFilter>().mesh = mesh;

        //generate vertices and triangles
        List<Vector3> vertices;
        List<int> meshTriangles;
        vertices = ChunkDataCuber(out meshTriangles, chunkDataSubset(x, y, z, allDensityData));

        //set mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.RecalculateNormals();
    }

    //get data for a single chunk from all data from point
    public float[,,] chunkDataSubset(int x, int y, int z, float[,,] data)
    {
        //chunk should have all its own data, plus one entry on each axis from adjacent chunks
        float[,,] dataOut = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];
        for (int _x = 0; _x < chunkSize + 1; _x++)
        {
            for (int _y = 0; _y < chunkSize + 1; _y++)
            {
                for (int _z = 0; _z < chunkSize + 1; _z++)
                {
                    dataOut[_x, _y, _z] = data[x + _x, y + _y, z + _z];
                }
            }
        }
        return dataOut;
    }

    //generate triangle and vertex list for entire chunk
    public List<Vector3> ChunkDataCuber(out List<int> trigPointList, float[,,] dataIn)
    {
        //loop through each grid point and 
        List<Triangle> data = new List<Triangle>();
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y += 1)
            {
                for (int z = 0; z < chunkSize; z += 1)
                {
                    List<Triangle> trigsToAdd = MarchCube(new Vector3Int(x, y, z), threshold, dataIn);
                    foreach (Triangle t in trigsToAdd)
                    {
                        data.Add(t);
                    }
                }
            }
        }

        //dictionary of added points, so triangles can share preadded points and have smoother shading
        Dictionary<Vector3, int> vecIntPairs = new Dictionary<Vector3, int>();

        //all points and triangles for mesh
        List<Vector3> points = new List<Vector3>();
        List<int> trigs = new List<int>();

        int count = 0;
        while (data.Count > 0)
        {

            //round points to be added to nearest 2 decimal places
            Vector3[] trianglePoints =
            {
                HelperFunctions.roundVecToNearestDec(data[0].a, 2),
                HelperFunctions.roundVecToNearestDec(data[0].b, 2),
                HelperFunctions.roundVecToNearestDec(data[0].c, 2)
            };

            //if already in dictionary, point new triangle to it. if not already there, add to dict, points, and make triangle point to it
            for(int i = 0; i < 3; i++)
            {
                if (vecIntPairs.ContainsKey(trianglePoints[i]))
                {
                    trigs.Add(vecIntPairs[trianglePoints[i]]);
                }
                else
                {
                    vecIntPairs.Add(trianglePoints[i], count);
                    points.Add(i == 0 ? data[0].a : i == 1 ? data[0].b : data[0].c);
                    trigs.Add(count++);
                }
            }

            data.RemoveAt(0);
        }
        trigPointList = trigs;
        return points;
    }

    //interpolate between two points based on their values
    Vector3 Interpolate(Vector3 pointA, Vector3 pointB, float valueA, float valueB, float threshold)
    {
        float t = (threshold - valueA) / (valueB - valueA);

        return pointA + t * (pointB - pointA);
    }

    //Generate triangle list for individual cube
    public List<Triangle> MarchCube(Vector3Int index, float threshold, float[,,] chunkData)
    {
        int x = index.x;
        int y = index.y;
        int z = index.z;

        //positions and values of the 8 corners of the current grid cell, relative to index
        Vector3[] points =
        {
            new Vector3(x + 0, y + 0, z + 0),
            new Vector3(x + 1, y + 0, z + 0),
            new Vector3(x + 1, y + 0, z + 1),
            new Vector3(x + 0, y + 0, z + 1),
            new Vector3(x + 0, y + 1, z + 0),
            new Vector3(x + 1, y + 1, z + 0),
            new Vector3(x + 1, y + 1, z + 1),
            new Vector3(x + 0, y + 1, z + 1)
        };

        float[] values =
        {
            chunkData[x + 0, y + 0, z + 0],
            chunkData[x + 1, y + 0, z + 0],
            chunkData[x + 1, y + 0, z + 1],
            chunkData[x + 0, y + 0, z + 1],
            chunkData[x + 0, y + 1, z + 0],
            chunkData[x + 1, y + 1, z + 0],
            chunkData[x + 1, y + 1, z + 1],
            chunkData[x + 0, y + 1, z + 1]
        };

        //find which case this cube matches
        int cubeIndex = 0;
        cubeIndex |= values[0] < threshold ? 1 : 0;
        cubeIndex |= values[1] < threshold ? 2 : 0;
        cubeIndex |= values[2] < threshold ? 4 : 0;
        cubeIndex |= values[3] < threshold ? 8 : 0;
        cubeIndex |= values[4] < threshold ? 16 : 0;
        cubeIndex |= values[5] < threshold ? 32 : 0;
        cubeIndex |= values[6] < threshold ? 64 : 0;
        cubeIndex |= values[7] < threshold ? 128 : 0;

        //empty so no triangles
        if (cubeIndex == 0) { return new List<Triangle>(); }

        //get list of vertices by interpolating between points based on their values. Not every entry of array gets filled necessarily 
        Vector3[] vertList = new Vector3[12];
        if ((Tables.edgeTable[cubeIndex] & 1) != 0) { vertList[0] = Interpolate(points[0], points[1], values[0], values[1], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 2) != 0) { vertList[1] = Interpolate(points[1], points[2], values[1], values[2], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 4) != 0) { vertList[2] = Interpolate(points[2], points[3], values[2], values[3], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 8) != 0) { vertList[3] = Interpolate(points[3], points[0], values[3], values[0], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 16) != 0) { vertList[4] = Interpolate(points[4], points[5], values[4], values[5], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 32) != 0) { vertList[5] = Interpolate(points[5], points[6], values[5], values[6], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 64) != 0) { vertList[6] = Interpolate(points[6], points[7], values[6], values[7], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 128) != 0) { vertList[7] = Interpolate(points[7], points[4], values[7], values[4], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 256) != 0) { vertList[8] = Interpolate(points[0], points[4], values[0], values[4], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 512) != 0) { vertList[9] = Interpolate(points[1], points[5], values[1], values[5], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 1024) != 0) { vertList[10] = Interpolate(points[2], points[6], values[2], values[6], threshold); }
        if ((Tables.edgeTable[cubeIndex] & 2048) != 0) { vertList[11] = Interpolate(points[3], points[7], values[3], values[7], threshold); }

        //generate triangles using interpolated vertices and triange table
        List<Triangle> TriangleList = new List<Triangle>();
        for (int i = 0; Tables.triangleTable[cubeIndex, i] != -1; i += 3)
        {
            Triangle trigToAdd = new Triangle();
            trigToAdd.a = vertList[Tables.triangleTable[cubeIndex, i + 2]];
            trigToAdd.b = vertList[Tables.triangleTable[cubeIndex, i + 1]];
            trigToAdd.c = vertList[Tables.triangleTable[cubeIndex, i + 0]];
            TriangleList.Add(trigToAdd);
        }
        return TriangleList;
    }
}
