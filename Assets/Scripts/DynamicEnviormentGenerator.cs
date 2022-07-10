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
    [SerializeField]
    public bool RegenerateTerrain = true;
    [SerializeField]
    public int RegenerateTerrainAfterXSteps = 1;

    [Header("Creature Settings")]
    [SerializeField]
    public List<BoneCategory> NotAllowedToTouchGround = new() { BoneCategory.Head };
    [SerializeField] 
    public bool PenalizeGroundContact = true;
    [SerializeField]
    public FlexibleDictionary<BoneCategory, int> PenaltiesForBodyParts = new() {{BoneCategory.Arm, 2}, {BoneCategory.Hand, 5},
        {BoneCategory.Head, 10}, {BoneCategory.Hip, 5}, {BoneCategory.Leg, 1}, {BoneCategory.Shoulder, 5}};


    [Header("Target Settings")] 
    [SerializeField]
    public bool IsMovingTarget = true;
    [SerializeField]
    public float MovementSpeed = 0.1f;
    [SerializeField] 
    public int TargetMaxSecondsInOneDirection = 10;
    [SerializeField]
    public float TargetWalkingSpeed = 10;
    [SerializeField]
    public float MaxWalkingSpeed = 10;
    [SerializeField]
    public bool RandomizeWalkSpeedEachEpisode;
    [SerializeField]
    public float YHeightOffset = 0.05f;
    [SerializeField]
    public bool PlaceTargetCubeRandomly = true;
    [SerializeField]
    public int PlaceTargetCubeRandomlyAfterXSteps = 1;

    [Header("ML-Agent Settings settings")] 
    [SerializeField]
    public int ContinuousActionSpaceOffset = 100;
    [SerializeField]
    public int ObservationSpaceOffset = 100;
    [SerializeField]
    public int DiscreteBranches = 0;

    void Awake()
    {
        GenerateTrainingEnvironment();
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
            AddTargetToArena(arena);
            var creature = GenerateCreature(arena);
            var skeleton = creature.GetComponentInChildren<Skeleton>();
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

    private GameObject GenerateCreature(GameObject arena)
    {
        var creature = Instantiate(CreaturePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        creature.name = "Creature";
        creature.AddComponent<WalkerAgent>();

        return creature;
    }

    private GameObject AddTargetToArena(GameObject arena)
    {
        var target = GameObject.Instantiate(TargetCubePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();

        return target;
    }
}
