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
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DynamicEnviormentGenerator : MonoBehaviour
{
    [Header("Editor Settings")] [Space(10)] [SerializeField, Tooltip("Must exist in the project!")]
    public string AgentScriptName = "WalkerAgent" ;

    [SerializeField] private GameObject CreaturePrefab;

    [SerializeField] public NNModel NnModel;
    
    [Header("Arena Settings")]
    [Space(10)]
    [SerializeField]
    public int ArenaCount = 10;

    [Header("Debug Settings")] [Space(10)] [SerializeField]
    public bool DebugMode = false;
    

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
    public bool TakeActionsBetweenDecisions = false;
    [SerializeField]
    public int DecisionPeriod = 0;
    

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

    [Header("Terrain settings")]
    [Space(10)]
    [SerializeField]
    public bool GenerateHeights  = true;
    [SerializeField]
    public int RegenerateTerrainAfterXEpisodes = 0;
    [SerializeField]
    public int Depth = 10;
    [SerializeField]
    public float Scale = 2.5f;


    [HideInInspector] // TODO Activate when implemented
    public CollectObjects NavMeshSurfaceCollectObjects = CollectObjects.Children;
    [HideInInspector] // TODO Activate when implemented
    public bool BakeNavMesh = false;
    [HideInInspector, Tooltip("valid range (0, Anzahl der NavMeshAgents (in Navigation->Agents) -1)")] // TODO Activate when implemented. Valid range (0, NavMesh.GetSettingsCount-1)
    public int NavMeshBuildSettingIndex = 0;
    
    public string BehaviorName => "Walker";
    public string GroundTag => "ground";
    public float YHeightOffset => 0.075f;
    public int TerrainSize => 128;
    
    [HideInInspector]
    public GameObject TargetCubePrefab;
    [HideInInspector]
    public GameObject WallPrefab;
    [HideInInspector]
    private ScriptableObject CreatureGeneratorSettings;
    [HideInInspector]
    private ScriptableObject ParametricCreatureSettings;


    void Awake()
    {
        TargetCubePrefab = Resources.Load("TargetCube", typeof(GameObject)) as GameObject;
        WallPrefab = Resources.Load("Wall", typeof(GameObject)) as GameObject;  
        CreatureGeneratorSettings = Resources.Load("CreatureGeneratorSettings", typeof(ScriptableObject)) as ScriptableObject;
        ParametricCreatureSettings = Resources.Load("ParametricCreatureSettings", typeof(ScriptableObject)) as ScriptableObject;

        
        if (WallPrefab == null || TargetCubePrefab == null)
            throw new ArgumentException("Prefabs not set in dynamic environment creator.");
        if (ArenaCount <= 0) throw new ArgumentException("We need at least one arena!");
        
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
        var navMeshSurface = terrain.AddComponent<NavMeshSurface>();
        navMeshSurface.agentTypeID = NavMesh.GetSettingsByIndex(NavMeshBuildSettingIndex).agentTypeID;
        navMeshSurface.collectObjects = NavMeshSurfaceCollectObjects;
        terrain.AddComponent<TerrainGenerator>();
        var colliderObj = terrain.AddComponent<TerrainCollider>();
        terrain.terrainData = new TerrainData();
        terrain.tag = GroundTag;
        colliderObj.terrainData = terrain.terrainData;
        terrain.materialTemplate = Resources.Load("GridMatFloor", typeof(Material)) as Material;


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

        if (DebugMode == false)
        {
            Destroy(wall1.GetComponent<MeshRenderer>());
            Destroy(wall2.GetComponent<MeshRenderer>());
            Destroy(wall3.GetComponent<MeshRenderer>());
            Destroy(wall4.GetComponent<MeshRenderer>());
            Destroy(terrain.GetComponent<MeshRenderer>());
        }

        return arena;
    }

    private void GenerateCreature(GameObject arena)
    {
        var creatureConfig = FindObjectOfType<CreatureConfig>();

        GameObject creatureContainer;
        if (CreaturePrefab != null)
        {
            Debug.LogWarning("Loading creature from prefab!");
            creatureContainer = Instantiate(CreaturePrefab);
        }
        else
        {
            Debug.LogWarning("Loading creature from generator!");
            creatureContainer = CreatureGenerator.ParametricBiped((CreatureGeneratorSettings) CreatureGeneratorSettings, (ParametricCreatureSettings) ParametricCreatureSettings, creatureConfig.seed);
            var orientationCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            orientationCube.name = "Orientation Cube";
            Destroy(orientationCube.GetComponent<Collider>());
            Destroy(orientationCube.GetComponent<MeshRenderer>());

            orientationCube.transform.parent = creatureContainer.transform;
        }
        
        creatureContainer.transform.parent = arena.transform;
        creatureContainer.name = "Creature";
        creatureContainer.transform.localPosition = new Vector3(64, 0, 64);

        if (creatureContainer.AddComponent(Type.GetType(AgentScriptName)) == null)
            throw new ArgumentException("Agent class name is wrong or does not exits in this context.");
        creatureContainer.AddComponent<ModelOverrider>();
        if (DebugMode) creatureContainer.AddComponent<DebugScript>();
    }

    private void AddTargetToArena(GameObject arena)
    {
        var target = Instantiate(TargetCubePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();
    }
}