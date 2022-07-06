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

    [Header("Settings for the terrain")]
    [SerializeField] 
    public int TerrainXSize = 128;
    [SerializeField]
    public int TerrainZSize = 128;
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
        var arena = GenerateArena();
        //GenerateCreature(arena);
        //AddTargetToArena(arena);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private GameObject GenerateArena()
    {
        var arena = new GameObject
        {
            name = "Creature Arena"
        };

        var terrainObj = new GameObject
        {
            name = "Terrain",
            transform = { parent = arena.transform }
        };


        var terrain = terrainObj.AddComponent<Terrain>();
        var terrainGenerator = terrain.AddComponent<TerrainGenerator>();
        var colliderObj =  terrain.AddComponent<TerrainCollider>(); 
        terrain.terrainData = new TerrainData();
        colliderObj.terrainData = terrain.terrainData;
        terrainGenerator.width = 128;
        terrainGenerator.length = 128;
        terrainGenerator._bakeNavMesh = BakeNavMesh;
        terrainGenerator._generateHeights = GenerateHeights;
        terrainGenerator._generateObstacles = GenerateObstacles;
        terrainGenerator._scaleObstacle = ScaleObstacle;
        terrainGenerator.depth = Depth;
        terrainGenerator.scale = Scale;
        terrainGenerator._obstaclesPrefab = ObstaclePrefab;
        
        var wall1 = Instantiate(WallPrefab, new Vector3(64,12,126), Quaternion.identity, terrain.transform);
        wall1.name = "Wall North";
        var wall2 = Instantiate(WallPrefab, new Vector3(126,12,64), Quaternion.Euler(0,90,0), terrain.transform);
        wall2.name = "Wall East";
        var wall3 = Instantiate(WallPrefab, new Vector3(64,12,2), Quaternion.identity, terrain.transform);
        wall3.name = "Wall South";
        var wall4 = Instantiate(WallPrefab, new Vector3(2,12,64), Quaternion.Euler(0,90,0), terrain.transform);
        wall4.name = "Wall West";

        return arena;
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
