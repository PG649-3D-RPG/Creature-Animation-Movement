using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AI;
using UnityEngine;
using Unity.AI.Navigation;
using Random = UnityEngine.Random;

public class TerrainGenerator : MonoBehaviour
{
    private Terrain _terrain;
    private  DynamicEnviormentGenerator Deg { get; set; }

    private NavMeshSurface NavMeshSurface { get; set; }

    private GameObject ObstaclesContainer { get; set; }

    private Terrain Terrain { get; set; }

    private float OffsetX { get; set; } = 100f;
    
    private float OffsetY { get; set; } = 100f;


    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        _terrain = GetComponent<Terrain>();
        NavMeshSurface = GetComponent<NavMeshSurface>();
        Deg = GameObject.FindObjectOfType<DynamicEnviormentGenerator>();
        ObstaclesContainer = new GameObject
        {
            name = "Obstacle Container",
            transform = { parent = this.transform }
        };
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

        // Kill obstacles
        foreach (Transform child in ObstaclesContainer.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        // Generate Terrain
        Terrain.terrainData = GenerateTerrain(Terrain.terrainData);

        if (!Deg.BakeNavMesh) return; // Skipp Nav Mesh generation
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
        terrainData.heightmapResolution = Deg.TerrainSize + 1;
        terrainData.size = new Vector3(Deg.TerrainSize, Deg.Depth, Deg.TerrainSize);


        // Generate terrain data
        if (!Deg.GenerateHeights) return terrainData; // Do not generate terrain with heights

        var heights = new float[Deg.TerrainSize, Deg.TerrainSize];
        for (var x = 0; x < Deg.TerrainSize; x++)
        {
            for (var y = 0; y < Deg.TerrainSize; y++)
            {
                heights[x, y] = Mathf.PerlinNoise((float)x / Deg.TerrainSize * Deg.Scale + OffsetX, (float)y / Deg.TerrainSize * Deg.Scale + OffsetY);
            }
        }

        terrainData.SetHeights(0, 0, heights);

        // Generate obstacles
        if (!Deg.GenerateObstacles) return terrainData; // Do not generate obstacles

        for (var x = 1; x < Deg.TerrainSize - 1; x++)
        {
            for (var y = 1; y < Deg.TerrainSize - 1; y++)
            {
                if (!(Mathf.PerlinNoise((x + OffsetX) / Deg.TerrainSize * Deg.ScaleObstacle, (y + OffsetY) / Deg.TerrainSize * Deg.ScaleObstacle + OffsetY) > Deg.ObstacleThreshold)) continue;
                var newObstacle = GameObject.Instantiate(Deg.ObstaclePrefab, Vector3.zero, Quaternion.identity, ObstaclesContainer.transform);
                newObstacle.transform.localPosition = new Vector3(x, terrainData.GetHeight(x, y) + 2f, y);
            }
        }
        return terrainData;
    }
}
