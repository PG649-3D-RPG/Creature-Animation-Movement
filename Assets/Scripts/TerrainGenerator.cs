using Config;
using UnityEngine;
using Unity.AI.Navigation;
using Random = UnityEngine.Random;

public class TerrainGenerator : MonoBehaviour
{
    private Terrain _terrain;
    private  DynamicEnvironmentGenerator Deg { get; set; }

    private NavMeshSurface NavMeshSurface { get; set; }
    
    private Terrain Terrain { get; set; }

    private float OffsetX { get; set; } = 100f;
    
    private float OffsetY { get; set; } = 100f;

    private ArenaConfig _arenaSettings;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        _terrain = GetComponent<Terrain>();
        NavMeshSurface = GetComponent<NavMeshSurface>();
        Deg = GameObject.FindObjectOfType<DynamicEnvironmentGenerator>();
        _arenaSettings = FindObjectOfType<ArenaConfig>();
    }

    public void Start()
    {
        OffsetX = Random.Range(0f, 9999f);
        OffsetY = Random.Range(0f, 9999f);
        Terrain = GetComponent<Terrain>();
        RegenerateTerrain();
    }

    /// <summary>
    /// 
    /// </summary>
    public void RegenerateTerrain()
    {
        OffsetX = Random.Range(0f, 9999f);
        OffsetY = Random.Range(0f, 9999f);
        
        // Generate Terrain
        Terrain.terrainData = GenerateTerrain(Terrain.terrainData);

        if (!_arenaSettings.BakeNavMesh) return; // Skipp Nav Mesh generation
        NavMeshSurface.BuildNavMesh();
    }

    /// <summary>
    /// Gets height (y) of terrain at Vector x.
    /// </summary>
    /// <param name="position">Position vector of object on terrain</param>
    /// <returns></returns>
    public float GetTerrainHeight(Vector3 position)
    {
        return _terrain.SampleHeight(position);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="terrainData">Current TerrainData</param>
    /// <returns></returns>
    private TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = DynamicEnvironmentGenerator.TerrainSize + 1;
        terrainData.size = new Vector3(DynamicEnvironmentGenerator.TerrainSize, _arenaSettings.Depth, DynamicEnvironmentGenerator.TerrainSize);


        // Generate terrain data
        if (!_arenaSettings.GenerateHeights) return terrainData; // Do not generate terrain with heights

        var heights = new float[DynamicEnvironmentGenerator.TerrainSize, DynamicEnvironmentGenerator.TerrainSize];
        for (var x = 0; x < DynamicEnvironmentGenerator.TerrainSize; x++)
        {
            for (var y = 0; y < DynamicEnvironmentGenerator.TerrainSize; y++)
            {
                heights[x, y] = Mathf.PerlinNoise((float)x / DynamicEnvironmentGenerator.TerrainSize * _arenaSettings.Scale + OffsetX, (float)y / DynamicEnvironmentGenerator.TerrainSize * _arenaSettings.Scale + OffsetY);
            }
        }

        terrainData.SetHeights(0, 0, heights);
        
        return terrainData;
    }
}
