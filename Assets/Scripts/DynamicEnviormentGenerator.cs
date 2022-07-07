using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DynamicEnviormentGenerator : MonoBehaviour
{

    [Header("Prefabs")]
    [SerializeField] public GameObject WallPrefab;

    [SerializeField] public GameObject CreaturePrefab;
    
    [SerializeField] public GameObject TargetCubePrefab;

    [SerializeField] public GameObject ObstaclePrefab;

    [Header("Arena Settings")]
    [SerializeField]
    public int ArenaCount = 10;

    [Header("Terrain settings")]
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

    [Header("Creature Settings")]
    [SerializeField]
    public bool regenerateTerrain = true;
    [SerializeField]
    public int regenerateTerrainAfterXSteps = 1;
    [SerializeField]
    public bool placeTargetCubeRandomly = true;
    [SerializeField]
    public int placeTargetCubeRandomlyAfterXSteps = 1;
    [SerializeField]
    public bool fastResetForTheFirstEpisodes = true;
    [SerializeField]
    public int fastResetLength = 10000000;
    [SerializeField]
    public List<BoneCategory> notAllowedToTouchGround = new() { BoneCategory.Head };
    [SerializeField]
    public List<BoneCategory> notAllowedToTouchGroundInFastPhase = new() { BoneCategory.Arm, BoneCategory.Hand, BoneCategory.Torso };
    [SerializeField] 
    public bool penalizeGroundContact = true;
    [SerializeField]
    private FlexibleDictionary<BoneCategory, int> penaltiesForBodyParts = new() {{BoneCategory.Arm, 2}, {BoneCategory.Hand, 5},
        {BoneCategory.Head, 10}, {BoneCategory.Hip, 5}, {BoneCategory.Leg, 1}, {BoneCategory.Shoulder, 5}};


    [Header("Target Settings")] 
    [SerializeField]
    public bool _isMovingTarget = true;
    [SerializeField]
    public float _movementSpeed = 0.1f;
    [SerializeField] 
    public int _targetMaxSecondsInOneDirection = 10;

    // Start is called before the first frame update
    void Awake()
    {
        GenerateTrainingEnvironment();

        //GenerateCreature(arena);
        //AddTargetToArena(arena);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void Start()
    {
    }


    public void GenerateTrainingEnvironment()
    {
        var posX = (int)Math.Ceiling(Math.Sqrt(ArenaCount));

        var posXCounter = 0;
        var posZCounter = 0;

        var arenaContainer = new GameObject
        {
            name = "Arena Container"
        };

        for (var i = 0; i < ArenaCount; i++, posXCounter++)
        {
            var arena = GenerateArena(i, posXCounter, posZCounter, posX, arenaContainer);
            var target = AddTargetToArena(arena);
            GenerateCreature(arena);
            if (posXCounter == posX - 1)
            {
                posXCounter = -1;
                posZCounter++;
            }
        }
    }

    private GameObject GenerateArena(int i, int posXCounter, int posZCounter, int posX, GameObject arenaContainer)
    {
        var arena = new GameObject
        {
            name = $"Arena {i}",
            transform =
                {
                    parent = arenaContainer.transform,
                    localPosition = new Vector3(posXCounter * TerrainSize, 0, posZCounter * TerrainSize )
                }
        };

        var terrainObj = new GameObject
        {
            name = $"Terrain {i}",
            transform =
                {
                    parent = arena.transform,
                    localPosition = Vector3.zero
                }
        };


        var terrain = terrainObj.AddComponent<Terrain>();
        var terrainGenerator = terrain.AddComponent<TerrainGenerator>();
        var colliderObj = terrain.AddComponent<TerrainCollider>();
        terrain.terrainData = new TerrainData();
        colliderObj.terrainData = terrain.terrainData;
        terrainGenerator.Deg = this;

        var wall1 = Instantiate(WallPrefab, new Vector3(64, 12, 126), Quaternion.identity, terrain.transform);
        wall1.name = "Wall North";
        wall1.transform.localPosition = new Vector3(64, 12, 126);
        var wall2 = Instantiate(WallPrefab, new Vector3(126, 12, 64), Quaternion.Euler(0, 90, 0), terrain.transform);
        wall2.name = "Wall East";
        wall2.transform.localPosition = new Vector3(126, 12, 64);
        var wall3 = Instantiate(WallPrefab, new Vector3(64, 12, 2), Quaternion.identity, terrain.transform);
        wall3.name = "Wall South";
        wall3.transform.localPosition = new Vector3(64, 12, 2);
        var wall4 = Instantiate(WallPrefab, new Vector3(2, 12, 64), Quaternion.Euler(0, 90, 0), terrain.transform);
        wall4.name = "Wall West";
        wall4.transform.localPosition = new Vector3(2, 12, 64);

        return arena;
    }

    private void GenerateCreature(GameObject arena)
    {
        var creature = Instantiate(CreaturePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        creature.name = "Creature";
        creature.AddComponent<WalkerAgent>();
    }

    private GameObject AddTargetToArena(GameObject arena)
    {
        var target = GameObject.Instantiate(TargetCubePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();

        return target;
    }
}
