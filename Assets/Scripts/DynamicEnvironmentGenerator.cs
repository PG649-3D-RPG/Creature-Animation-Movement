using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Config;
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
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class DynamicEnvironmentGenerator : MonoBehaviour
{
    public const string BehaviorName = "Walker";
    public const string GroundTag = "ground";
    public const float YHeightOffset = 0.075f;
    public const int TerrainSize = 128;
    
    [Header("Editor Settings")] [SerializeField, Tooltip("Must exist in the project!")]
    public string AgentScriptName = "WalkerAgent";

    [SerializeField] private GameObject CreaturePrefab;

    [SerializeField] public NNModel NnModel;

    [HideInInspector] public GameObject TargetCubePrefab;
    [HideInInspector] public GameObject WallPrefab;
    [HideInInspector] public GameObject ObstaclePrefab;
    [HideInInspector] private ScriptableObject CreatureGeneratorSettings;
    [HideInInspector] private ScriptableObject ParametricCreatureSettings2Legged;
    [HideInInspector] private ScriptableObject ParametricCreatureSettings4Legged;
    [HideInInspector] private ArenaConfig _arenaConfig;

    void Awake()
    {
        TargetCubePrefab = Resources.Load("TargetCube", typeof(GameObject)) as GameObject;
        WallPrefab = Resources.Load("Wall", typeof(GameObject)) as GameObject;
        ObstaclePrefab = Resources.Load("Obstacle", typeof(GameObject)) as GameObject;

        CreatureGeneratorSettings =
            Resources.Load("CreatureGeneratorSettings", typeof(ScriptableObject)) as ScriptableObject;
        ParametricCreatureSettings2Legged =
            Resources.Load("BipedSettings", typeof(ScriptableObject)) as ScriptableObject;
        ParametricCreatureSettings4Legged =
            Resources.Load("QuadrupedSettings", typeof(ScriptableObject)) as ScriptableObject;

        // Small hack which assures, that the Config is really loaded before usage. Otherwise config values might be skipped. 
        // If someone finds a better method to do this, please change!
        _arenaConfig = FindObjectOfType<ArenaConfig>();
        _arenaConfig.Awake();
        var creatureConfig = FindObjectOfType<CreatureConfig>();
        creatureConfig.Awake();
        var mlAgentConfig = FindObjectOfType<MlAgentConfig>();
        mlAgentConfig.Awake();

        if (WallPrefab == null || TargetCubePrefab == null)
            throw new ArgumentException("Prefabs not set in dynamic environment creator.");
    }

    private void Start()
    {
        // Only generate Training Arena if we don't want to use the creature debugger
        if(!FindObjectOfType<CreatureDebugger>().ActivateCreatureDebugger) GenerateTrainingEnvironment();
    }

    private void GenerateTrainingEnvironment()
    {
        var arenaContainer = new GameObject
        {
            name = "Arena Container"
        };

        var xzLimit = (int)Math.Ceiling(Math.Sqrt(_arenaConfig.ArenaCount));
        for (var i = 0; i < _arenaConfig.ArenaCount; i++)
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
                localPosition = new Vector3(posXCounter * TerrainSize, 0, posZCounter * TerrainSize)
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
        navMeshSurface.agentTypeID = NavMesh.GetSettingsByIndex(_arenaConfig.NavMeshBuildSettingIndex).agentTypeID;
        navMeshSurface.collectObjects = _arenaConfig.NavMeshSurfaceCollectObjects;
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
        
        if (!Application.isEditor)
        {
            Destroy(wall1.GetComponent<MeshRenderer>());
            Destroy(wall2.GetComponent<MeshRenderer>());
            Destroy(wall3.GetComponent<MeshRenderer>());
            Destroy(wall4.GetComponent<MeshRenderer>());
            Destroy(terrain.GetComponent<MeshRenderer>());
        }

        if(_arenaConfig.GenerateObstacles)
        {
            var obstacleContainer = new GameObject
            {
                name = "ObstaclesContainer",
                transform = 
                {
                    parent = terrain.transform,
                    localPosition = Vector3.zero
                }
            };
            GenerateObstacles(obstacleContainer.transform);
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
            if (CreaturePrefab.GetComponent(Type.GetType(AgentScriptName)) != null)
            {
                throw new ArgumentException("Creature-Prefab has AgentScript attached. Delete it to proceed.");
            }
            creatureContainer = Instantiate(CreaturePrefab);
        }
        else
        {
            Debug.LogWarning($"Loading creature from generator with seed {creatureConfig.seed}!");

            creatureContainer = creatureConfig.creatureType switch
            {
                CreatureType.Biped => CreatureGenerator.ParametricBiped((CreatureGeneratorSettings)CreatureGeneratorSettings,
                                        (BipedSettings)ParametricCreatureSettings2Legged,
                                        creatureConfig.seed),
                CreatureType.Quadruped => CreatureGenerator.ParametricQuadruped((CreatureGeneratorSettings)CreatureGeneratorSettings,
                                        (QuadrupedSettings)ParametricCreatureSettings4Legged,
                                        creatureConfig.seed),
                _ => throw new ArgumentException("No creature type selected"),
            };
        }
        
        CreatureHooks(creatureContainer);
        
        if (creatureContainer.transform.Find("Orientation Cube") == null)
        {
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
        if(creatureContainer.GetComponent<ModelOverrider>() == null) creatureContainer.AddComponent<ModelOverrider>();
        if (Application.isEditor && creatureContainer.GetComponent<DebugScript>() == null ) creatureContainer.AddComponent<DebugScript>();
    }

    private void GenerateObstacles(Transform parent)
    {
        List<Vector3> takenSpawnPositions = new();
        for(int i = 0; i < _arenaConfig.numberObstacles; i++)
        {
            Vector3 spawnPosition = GetNextObstacleSpawnPosition(takenSpawnPositions);
            takenSpawnPositions.Add(spawnPosition);

            var obstacle = Instantiate(ObstaclePrefab, spawnPosition, Quaternion.identity, parent);
            obstacle.name = "Obstacle" + i;
            obstacle.transform.localPosition = spawnPosition;
        }
    }

    private Vector3 GetNextObstacleSpawnPosition(IEnumerable<Vector3> takenSpawnPositions)
    {
        var spawnPoint = RandomObstacleSpawnPoint();

        while(!IsValidOnstacleSpawnPoint(takenSpawnPositions, spawnPoint))
        {
            spawnPoint = RandomObstacleSpawnPoint();
        }

        return spawnPoint;
    }

    private void AddTargetToArena(GameObject arena)
    {
        var target = Instantiate(TargetCubePrefab, new Vector3(64, 12, 126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();
    }

    private void CreatureHooks(GameObject g)
    {
        g.AddComponent<CreatureController>();
        
        foreach (var rb in g.GetComponentsInChildren<ConfigurableJoint>())
        {
            rb.transform.AddComponent<Life>();
        }

    }

    private bool IsValidOnstacleSpawnPoint(IEnumerable<Vector3> takenSpawnPositions, Vector3 spawnPoint)
    {
        var obstacleHalfWidth = ObstaclePrefab.transform.localScale.x/2;
        var xLowerBound = 50f - obstacleHalfWidth;
        var xUpperBound = 80f + obstacleHalfWidth;

        var obstacleHalfLength = ObstaclePrefab.transform.localScale.z/2;
        var zLowerBound = 50f - obstacleHalfLength;
        var zUpperBound = 80f + obstacleHalfLength;

        return !takenSpawnPositions.Contains(spawnPoint) &&
                !(xLowerBound <= spawnPoint.x && spawnPoint.x <= xUpperBound && 
                  zLowerBound <= spawnPoint.z && spawnPoint.z <= zUpperBound);
    }

    private Vector3 RandomObstacleSpawnPoint()
    {
        var xScale = ObstaclePrefab.transform.localScale.x;
        var zScale = ObstaclePrefab.transform.localScale.z;

        var x = Random.Range(xScale, TerrainSize - xScale);
        var z = Random.Range(zScale, TerrainSize - zScale);

        return new Vector3(x, ObstaclePrefab.transform.localScale.y/2, z);
    }
}