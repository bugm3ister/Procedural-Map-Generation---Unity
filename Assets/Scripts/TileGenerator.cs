using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileGenerator : MonoBehaviour
{
    public int noiseSampleSize;
    public float scale;

    public int textureResolution = 1;
    public float maxHeight = 1.0f;

    public TerrainVisualization visualizationType;

    [HideInInspector]
    public Vector2 offset;

    [Header("Terrain Types")]
    public TerrainType[] heightTerrainTypes;
    public TerrainType[] heatTerrainTypes;
    public TerrainType[] moistureTerrainType;

    [Header("Waves")]
    public Wave[] waves;
    public Wave[] heatWaves;
    public Wave[] moistureWaves;

    [Header("Curves")]
    public AnimationCurve heightCurve;


    private MeshRenderer mRenderer;
    private MeshFilter mFilter;
    private MeshCollider mCollider;

    private MeshGenerator meshGenerator;
    private MapManager mapManager;


    // Start is called before the first frame update
    void Start()
    {
        mRenderer= GetComponent<MeshRenderer>();
        mFilter= GetComponent<MeshFilter>();
        mCollider= GetComponent<MeshCollider>();

        meshGenerator= GetComponent<MeshGenerator>();
        mapManager= FindObjectOfType<MapManager>();

        GenerateTile();
    }

    void GenerateTile()
    {
        float[,] heightMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize, scale, waves, offset);

        float[,] hdHeightMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize - 1, scale, waves, offset, textureResolution);

        Texture2D heightMapTexture = TextureBuilder.BuildTexture(hdHeightMap, heightTerrainTypes);

        mRenderer.material.mainTexture= heightMapTexture;

        Vector3[] verts = mFilter.mesh.vertices;
        for (int x = 0; x < noiseSampleSize; x++)
        {
            for (int z = 0; z < noiseSampleSize; z++)
            {
                int index = (x * noiseSampleSize) + z;
                verts[index].y = heightCurve.Evaluate(heightMap[x,z]) * maxHeight;
            }
        }

        mFilter.mesh.vertices= verts;
        mFilter.mesh.RecalculateBounds();
        mFilter.mesh.RecalculateNormals();

        mCollider.sharedMesh = mFilter.mesh;

        float[,] heatMap = GenerateHeatMap(heightMap);
        float[,] moistureMap = GenerateMoistureMap(heightMap);

        switch(visualizationType)
        {
            case TerrainVisualization.Height:
                mRenderer.material.mainTexture = TextureBuilder.BuildTexture(hdHeightMap, heightTerrainTypes);
                break;
            case TerrainVisualization.Heat:
                mRenderer.material.mainTexture = TextureBuilder.BuildTexture(heatMap, heatTerrainTypes);
                break;
            case TerrainVisualization.Moisture:
                mRenderer.material.mainTexture = TextureBuilder.BuildTexture(moistureMap, moistureTerrainType);
                break;
        }
       

    }

    float[,] GenerateHeatMap(float[,] heightMap)
    {
        float[,] uniformHeatMap = NoiseGenerator.GenerateUniformNoiseMap(noiseSampleSize, transform.position.z * (noiseSampleSize / meshGenerator.xSize), (noiseSampleSize / 2 * mapManager.numX) + 1);
        float[,] randomHeatMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize, scale, heatWaves, offset);

        float[,] heatMap = new float[noiseSampleSize, noiseSampleSize];

        for (int x = 0; x < noiseSampleSize; x++)
        {
            for (int z = 0; z < noiseSampleSize; z++)
            {
                heatMap[x, z] = randomHeatMap[x, z] * uniformHeatMap[x, z];
                heatMap[x, z] += 0.5f * heightMap[x, z];

                heatMap[x, z] = Mathf.Clamp(heatMap[x, z], 0.0f, 0.99f);
            }
        }

        return heatMap;
    }

    float[,] GenerateMoistureMap(float[,] heightMap)
    {
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize, scale, moistureWaves, offset);

        for (int x = 0; x < noiseSampleSize; x++)
        {
            for (int z = 0; z < noiseSampleSize; z++)
            {
                moistureMap[x, z] -= 0.01f * heightMap[x, z];
            }
        }

        return moistureMap;
    }
}

[System.Serializable]
public class TerrainType
{
    [Range(0.0f, 1.0f)]
    public float threshold;
    //public Color color;
    public Gradient colorGradient;
}

public enum TerrainVisualization
{
    Height,
    Heat,
    Moisture
}
