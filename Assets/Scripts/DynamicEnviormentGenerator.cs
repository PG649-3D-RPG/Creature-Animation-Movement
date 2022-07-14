using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Unity.Barracuda;
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

    [SerializeField] public NNModel NnModel;

    [Header("Arena Settings")]
    [SerializeField]
    public int ArenaCount = 10;
    [SerializeField]
    public string GroundTag = "ground";


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
    [SerializeField]
    public Material TerrainMaterial;
    [SerializeField]
    public Material WallMaterial;

    [Header("Creature Settings")]
    [SerializeField]
    public List<BoneCategory> NotAllowedToTouchGround = new() { BoneCategory.Head };
    [SerializeField]
    public FlexibleDictionary<BoneCategory, int> PenaltiesForBodyParts = new() {};


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
    public bool UseContinuousActionSpaceOffsetAsContinuousActionSpace = true;
    [SerializeField]
    public bool UseObservationSpaceOffsetAsObservationSpace = true;

    [SerializeField]
    public int DiscreteBranches = 0;

    [SerializeField]
    public float MaxJointForceLimit = 20000;
    [SerializeField]
    public string BehaviorName = "Walker";
    [SerializeField]
    public float JointDampen = 5000;
    [SerializeField]
    public float MaxJointSpring = 40000;
    [SerializeField]
    public int MaxStep = 5000;


    void Awake()
    {
        if (WallPrefab == null || CreaturePrefab == null || TargetCubePrefab == null || ObstaclePrefab == null)
            throw new ArgumentException("Prefabs not set in dynamic environment creator.");
        GenerateTrainingEnvironment();
    }

    private void GenerateTrainingEnvironment()
    {
        var arenaContainer = new GameObject
        {
            name = "Arena Container"
        };

        var xzLimit = (int) Math.Ceiling(Math.Sqrt(ArenaCount));
        for (var i = 0; i < ArenaCount; i++)
        {
            var posZCounter = Math.DivRem(i, xzLimit, out var posXCounter);
            var arena = GenerateArena(i, posXCounter, posZCounter, xzLimit, arenaContainer);
            AddTargetToArena(arena);
            GenerateCreature(arena);
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
        terrain.AddComponent<TerrainGenerator>();
        var colliderObj = terrain.AddComponent<TerrainCollider>();
        terrain.terrainData = new TerrainData();
        terrain.tag = GroundTag;
        colliderObj.terrainData = terrain.terrainData;
        terrain.materialTemplate = TerrainMaterial;


        var wall1 = Instantiate(WallPrefab, new Vector3(64, 12, 126), Quaternion.identity, terrain.transform);
        wall1.name = "Wall North";
        wall1.transform.localPosition = new Vector3(64, 12, 126);
        wall1.GetComponent<MeshRenderer>().material = WallMaterial;

        var wall2 = Instantiate(WallPrefab, new Vector3(126, 12, 64), Quaternion.Euler(0, 90, 0), terrain.transform);
        wall2.name = "Wall East";
        wall2.transform.localPosition = new Vector3(126, 12, 64);
        wall2.GetComponent<MeshRenderer>().material = WallMaterial;

        var wall3 = Instantiate(WallPrefab, new Vector3(64, 12, 2), Quaternion.identity, terrain.transform);
        wall3.name = "Wall South";
        wall3.transform.localPosition = new Vector3(64, 12, 2);
        wall3.GetComponent<MeshRenderer>().material = WallMaterial;

        var wall4 = Instantiate(WallPrefab, new Vector3(2, 12, 64), Quaternion.Euler(0, 90, 0), terrain.transform);
        wall4.name = "Wall West";
        wall4.transform.localPosition = new Vector3(2, 12, 64);
        wall4.GetComponent<MeshRenderer>().material = WallMaterial;

        return arena;
    }

    private void GenerateCreature(GameObject arena)
    {
        var creature = Instantiate(CreaturePrefab, new Vector3(64,24,64), Quaternion.identity, arena.transform);
        creature.name = "Creature";
        creature.transform.localPosition = new Vector3(64, 24, 64);
        creature.AddComponent<WalkerAgent>();
        // TODO Check what it does
        creature.AddComponent<ModelOverrider>();
    }

    private void AddTargetToArena(GameObject arena)
    {
        var target = GameObject.Instantiate(TargetCubePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();
    }
}
