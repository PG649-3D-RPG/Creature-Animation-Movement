using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Debug = System.Diagnostics.Debug;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DynamicEnviormentGenerator : MonoBehaviour
{
    [Header("Materials")]
    [Space(10)]
    [SerializeField]
    public Material TerrainMaterial;
    [SerializeField]
    public Material WallMaterial;

    [Header("Scripts")] [Space(10)] [SerializeField, Tooltip("Must exist in the project!")]
    public string AgentScriptName = "WalkerAgent" ;
    
    [Header("Prefabs")]
    [Space(10)]
    [SerializeField] public GameObject CreaturePrefab;

    [SerializeField] public NNModel NnModel;

    [SerializeField] public GameObject ObstaclePrefab;

    [SerializeField] public GameObject TargetCubePrefab;

    [SerializeField] public GameObject WallPrefab;


    [Header("Arena Settings")]
    [Space(10)]
    [SerializeField]
    public int ArenaCount = 10;
    public string GroundTag = "ground";


    [Header("Debug Settings")] [Space(10)] [SerializeField]
    public bool DebugMode = false;
    [SerializeField]
    public float TimeScale = 1f;
    
    [Header("Creature Settings")] [Space(10)] 
    [SerializeField]
    public float ArmsGroundContactPenalty = 0;
    [SerializeField]
    public float HandsGroundContactPenalty = 0;
    [SerializeField]
    public float HeadGroundContactPenalty = 0;
    [SerializeField]
    public float HipsGroundContactPenalty = 0;
    [SerializeField]
    public float LegsGroundContactPenalty = 0;
    [SerializeField]
    public float TorsoGroundContactPenalty = 0;
    [SerializeField]
    public List<BoneCategory> ResetOnGroundContactParts = new() { BoneCategory.Head };
    [HideInInspector]
    public readonly Dictionary<BoneCategory, float> PenaltiesForBodyParts = new() {};


    [Header("ML-Agent Settings settings")]
    [Space(10)]
    [SerializeField]
    public int ContinuousActionSpaceOffset = 100;
    [SerializeField]
    public int DiscreteBranches = 0;
    [SerializeField]
    public float JointDampen = 5000;
    [SerializeField]
    public float MaxJointForceLimit = 20000;
    [SerializeField]
    public float MaxJointSpring = 40000;
    [SerializeField]
    public int MaxStep = 5000;
    [SerializeField]
    public int ObservationSpaceOffset = 100;
    [SerializeField]
    public bool UseContinuousActionSpaceOffsetAsContinuousActionSpace = true;
    [SerializeField]
    public bool UseObservationSpaceOffsetAsObservationSpace = true;
    public string BehaviorName = "Walker";


    [Header("Target Cube Settings")]
    [Space(10)]
    [SerializeField]
    public float MaxWalkingSpeed = 10;
    [SerializeField]
    public int EpisodeCountToRandomizeTargetCubePosition = 0;
    [SerializeField]
    public bool RandomizeWalkSpeedEachEpisode = false;
    [SerializeField] 
    public int TargetMaxSecondsInOneDirection = 10;
    [SerializeField]
    public float TargetMovementSpeed = 0f;
    [SerializeField]
    public float TargetWalkingSpeed = 10;
    [SerializeField]
    public float YHeightOffset = 0.05f;


    [Header("Terrain settings")]
    [Space(10)]
    [SerializeField]
    public bool GenerateHeights  = true;
    [HideInInspector] // TODO Activate when implemented
    public bool GenerateObstacles  = true;
    [SerializeField]
    public int RegenerateTerrainAfterXEpisodes = 0;
    [HideInInspector] // TODO Activate when implemented
    public bool BakeNavMesh = false;
    [HideInInspector, Tooltip("valid range (0, Anzahl der NavMeshAgents (in Navigation->Agents) -1)")] // TODO Activate when implemented
    //valid range (0, NavMesh.GetSettingsCount-1)
    public int NavMeshBuildSettingIndex = 0;
    [HideInInspector]
    public CollectObjects NavMeshSurfaceCollectObjects = CollectObjects.Children;
    [SerializeField]
    public int Depth = 10;
    [HideInInspector] // TODO Activate when implemented
    public float ObstacleThreshold = 0.9f;
    [SerializeField]
    public float Scale = 2.5f;
    [SerializeField]
    public float ScaleObstacle  = 10f;
    [HideInInspector] // Ist nicht wirklich �berall implementiert. Brauchen wir wahrscheinlich auch nicht?
    public int TerrainSize = 128;
    
    void Awake()
    {
        if (WallPrefab == null || CreaturePrefab == null || TargetCubePrefab == null || ObstaclePrefab == null)
            throw new ArgumentException("Prefabs not set in dynamic environment creator.");
        if (ArenaCount <= 0) throw new ArgumentException("We need at least one arena!");
        
        
        if(HeadGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Head, HeadGroundContactPenalty);
        if(TorsoGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Torso, TorsoGroundContactPenalty);
        if(HipsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Hip, HipsGroundContactPenalty);
        if(LegsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Leg, LegsGroundContactPenalty);
        if(ArmsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Arm, ArmsGroundContactPenalty);
        if(HandsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Hand, HandsGroundContactPenalty);

        if(DebugMode) Time.timeScale = TimeScale;
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
        NavMeshSurface navMeshSurface = terrain.AddComponent<NavMeshSurface>();
        navMeshSurface.agentTypeID = NavMesh.GetSettingsByIndex(NavMeshBuildSettingIndex).agentTypeID;
        navMeshSurface.collectObjects = NavMeshSurfaceCollectObjects;
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
        if (creature.AddComponent(Type.GetType("WalkerAgent")) == null)
            throw new ArgumentException("Agent class name is wrong or does not exits in this context.");
        creature.AddComponent<ModelOverrider>(); // TODO Check what it does
        if (DebugMode) creature.AddComponent<DebugScript>();
    }

    private void AddTargetToArena(GameObject arena)
    {
        var target = GameObject.Instantiate(TargetCubePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();
    }
}

public enum WalkerAgentType
{
    Default,
    
}