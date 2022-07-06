using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AI;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainGenerator : MonoBehaviour
{
    public int width { get; set; } = 128; //x-axis of the terrain
    public int length { get; set; } = 128; //z-axis

    public int depth { get; set; } = 10; //y-axis

    public float scale { get; set; } = 2.5f;

    public float offsetX { get; set; } = 100f;
    public float offsetY { get; set; } = 100f;

    private Terrain terrain { get; set; }

    private GameObject _obstaclesContainer { get; set; } 

    public bool _generateObstacles { get; set; }  = true;
    public bool _generateHeights { get; set; } = true;
    public bool _bakeNavMesh { get; set; } = true;
    public GameObject  _obstaclesPrefab { get; set; }
    public float _obstacleThreshold { get; set; } = 0.9f;
    public float _scaleObstacle { get; set; }  = 10f;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        _obstaclesContainer = new GameObject
        {
            name = "Obstacle Container",
            transform = { parent = this.transform }
        };
    }

    public void Start()
    {
        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);
        terrain = GetComponent<Terrain>();
        RegenerateTerrain();
    }

    /// <summary>
    /// 
    /// </summary>
    public void RegenerateTerrain()
    {
        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);

        // Kill obstacles
        foreach (Transform child in _obstaclesContainer.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        // Generate Terrain
        terrain.terrainData = GenerateTerrain(terrain.terrainData);

        if (!_bakeNavMesh) return; // Skipp Nav Mesh generation
        //NavMeshBuilder.ClearAllNavMeshes();
        //NavMeshBuilder.BuildNavMesh(); //Blocking Operation is slow
        //NavMeshBuilder.BuildNavMeshAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position">Position vector of object on terrain</param>
    /// <returns></returns>
    public float GetTerrainHeight(Vector3 position)
    {
        Terrain terrain = GetComponent<Terrain>();
        return terrain.SampleHeight(position);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x">x cordinate for the requested position</param>
    /// <param name="z">z cordinate for the requested position</param>
    /// <returns></returns>
    public float GetTerrainHeight(int x, int z)
    {
        var terrain = GetComponent<Terrain>();
        return terrain.terrainData.GetHeight(x, z);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="terrainData">Current TerrainData</param>
    /// <returns></returns>
    private TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, length);


        // Generate terrain data
        if (!_generateHeights) return terrainData; // Do not generate terrain with heights

        var heights = new float[width, length];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < length; y++)
            {
                heights[x, y] = Mathf.PerlinNoise((float)x / width * scale + offsetX, (float)y / length * scale + offsetY);
            }
        }

        terrainData.SetHeights(0, 0, heights);

        // Generate obstacles
        if (!_generateObstacles) return terrainData; // Do not generate obstacles

        for (var x = 1; x < width - 1; x++)
        {
            for (var y = 1; y < length - 1; y++)
            {

                if (!(Mathf.PerlinNoise((x + offsetX) / width * _scaleObstacle, (y + offsetY) / length * _scaleObstacle + offsetY) > _obstacleThreshold)) continue;
                var newObstacle = GameObject.Instantiate(_obstaclesPrefab, Vector3.zero, Quaternion.identity, _obstaclesContainer.transform);
                newObstacle.transform.localPosition = new Vector3(x, terrainData.GetHeight(x, y) + 2f, y);
            }
        }
        return terrainData;
    }
}
