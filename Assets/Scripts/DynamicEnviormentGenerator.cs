using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;

public class DynamicEnviormentGenerator : MonoBehaviour
{

    [Header("Prefabs")]
    [SerializeField] public GameObject WallPrefab;

    [SerializeField] public GameObject CreaturePrefab;
    
    [SerializeField] public GameObject TargetCubePrefab;

    [SerializeField] public GameObject ObstaclePrefab;

    [Header("Arena Settings")]
    [SerializeField]
    public int ArenaCount = 100;


    [Header("Settings for the terrain")]
    [SerializeField] 
    public int TerrainSize = 128;
    [SerializeField]
    public int Depth = 10;
    [SerializeField]
    public float Scale = 2.5f;
    [SerializeField]
    public bool GenerateObstacles  = true;
    [SerializeField]
    public bool GenerateHeights  = true;
    [SerializeField]
    public bool BakeNavMesh = true;
    [SerializeField]
    public float ObstacleThreshold { get; set; } = 0.9f;
    [SerializeField]
    public float ScaleObstacle { get; set; } = 10f;


    // Start is called before the first frame update
    void Awake()
    {
         GenerateArena();
        //GenerateCreature(arena);
        //AddTargetToArena(arena);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GenerateArena()
    {
        var posX = (int) Math.Ceiling(Math.Sqrt(ArenaCount));
        var posZ = posX;

        var posXCounter = 0;
        var posZCounter = 0;

        var arenaContainer = new GameObject(name = "Arena Container");

        var arena = new GameObject();

        for (var i = 0; i < ArenaCount; i++, posXCounter++)
        {
            Instantiate(arena, new Vector3(posXCounter * TerrainSize, 0, posZCounter * TerrainSize),
                Quaternion.identity, arenaContainer.transform);
            arena.name = $"Arena {i}";

            var terrainObj = new GameObject
            {
                name = $"Terrain {i}",
                transform = { parent = arena.transform}
            };


            var terrain = terrainObj.AddComponent<Terrain>();
            var terrainGenerator = terrain.AddComponent<TerrainGenerator>();
            var colliderObj = terrain.AddComponent<TerrainCollider>();
            terrain.terrainData = new TerrainData();
            colliderObj.terrainData = terrain.terrainData;
            terrainGenerator.width = TerrainSize;
            terrainGenerator.length = TerrainSize;
            terrainGenerator._bakeNavMesh = BakeNavMesh;
            terrainGenerator._generateHeights = GenerateHeights;
            terrainGenerator._generateObstacles = GenerateObstacles;
            terrainGenerator._scaleObstacle = ScaleObstacle;
            terrainGenerator.depth = Depth;
            terrainGenerator.scale = Scale;
            terrainGenerator._obstaclesPrefab = ObstaclePrefab;

            var wall1 = Instantiate(WallPrefab, new Vector3(64, 12, 126), Quaternion.identity, terrain.transform);
            wall1.name = "Wall North";
            var wall2 = Instantiate(WallPrefab, new Vector3(126, 12, 64), Quaternion.Euler(0, 90, 0), terrain.transform);
            wall2.name = "Wall East";
            var wall3 = Instantiate(WallPrefab, new Vector3(64, 12, 2), Quaternion.identity, terrain.transform);
            wall3.name = "Wall South";
            var wall4 = Instantiate(WallPrefab, new Vector3(2, 12, 64), Quaternion.Euler(0, 90, 0), terrain.transform);
            wall4.name = "Wall West";

            if (posXCounter == posX - 1)
            {
                posXCounter = -1;
                posZCounter++;
            }
        }

        Destroy(arena);
    }

    public void GenerateCreature(GameObject arena)
    {
        
        var creature = Instantiate(CreaturePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        creature.name = "Creature";
        //creature.AddComponent<WalkerAgent>();
        //creature.AddComponent<DecisionRequester>();
        //creature.AddComponent<JointDriveController>();
        //creature.AddComponent<BehaviorParameters>();
    }

    private void AddTargetToArena(GameObject arena)
    {
        var target = GameObject.Instantiate(TargetCubePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
    }
}
