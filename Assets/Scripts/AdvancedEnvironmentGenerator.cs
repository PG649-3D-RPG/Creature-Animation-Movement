using Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using Unity.MLAgentsExamples;
using UnityEngine;
using UnityEngine.AI;

public class AdvancedEnvironmentGenerator : GenericEnvironmentGenerator
{
    public string AgentScriptName = "AgentNavMesh";

    [SerializeField] public GameObject CreaturePrefab;

    [SerializeField] public int amountCreatures = 1;

    [Header("Target Settings")]
    [SerializeField] public int TargetSpeed = 15;
    [SerializeField] public float TargetWanderTimer = 30;
    [SerializeField] public float TargetWanderRadius = 1500;

    [HideInInspector] public GameObject TargetCubePrefab;
    [HideInInspector] private ScriptableObject CreatureGeneratorSettings;
    [HideInInspector] private ScriptableObject ParametricCreatureSettings2Legged;
    [HideInInspector] private ScriptableObject BipedJointLimitOverrides;
    [HideInInspector] private ScriptableObject ParametricCreatureSettings4Legged;
    [HideInInspector] private ScriptableObject QuadrupedJointLimitOverrides;
    [HideInInspector] private ScriptableObject _worldGenertorSettingsConfig;

    [HideInInspector] private SPTC_WG sptc_wg;
    [HideInInspector] public MiscTerrainData miscTerrainData;

    [HideInInspector] private GameObject creatureContainer;
    [HideInInspector] private GameObject targetContainer;

    void Awake()
    {
        TargetCubePrefab = Resources.Load("TargetCube", typeof(GameObject)) as GameObject;

        // Load ScriptableObject Configs
        CreatureGeneratorSettings =
            Resources.Load("CreatureGeneratorSettings", typeof(ScriptableObject)) as ScriptableObject;
        ParametricCreatureSettings2Legged =
            Resources.Load("BipedSettings", typeof(ScriptableObject)) as ScriptableObject;
        BipedJointLimitOverrides =
            Resources.Load("BipedJointLimitOverrides", typeof(ScriptableObject)) as ScriptableObject;
        ParametricCreatureSettings4Legged =
            Resources.Load("QuadrupedSettings", typeof(ScriptableObject)) as ScriptableObject;
        QuadrupedJointLimitOverrides =
            Resources.Load("QuadrupedJointLimitOverrides", typeof(ScriptableObject)) as ScriptableObject;
        _worldGenertorSettingsConfig =
            Resources.Load("WorldGeneratorSettings", typeof(ScriptableObject)) as ScriptableObject;

        // Load Component Configs (necessary?)
        var creatureConfig = FindObjectOfType<CreatureConfig>();
        creatureConfig.Awake();


        //TODO: HACK, EXECUTION ORDER IS WEIRD

        // Add World Generator and generate world
        /*        if (!FindObjectOfType<CreatureDebugger>().ActivateCreatureDebugger || !Application.isEditor)
                {
                    sptc_wg = gameObject.AddComponent<SPTC_WG>();
                    sptc_wg.settings = (WorldGeneratorSettings)_worldGenertorSettingsConfig;
                }*/

        WorldGenerator.Generate((WorldGeneratorSettings)_worldGenertorSettingsConfig);
        
    }


    // Start is called before the first frame update
    void Start()
    {
        TerrainObject = GameObject.Find("Terrain");
        miscTerrainData = TerrainObject.GetComponent<MiscTerrainData>();

        creatureContainer = new GameObject("Creature Container");
        targetContainer = new GameObject("Target Container");

        // Choose Random Spawn Points
        amountCreatures = Math.Min(amountCreatures, miscTerrainData.SpawnPoints.Count - 1);
        List<System.Tuple<Vector3Int, int>> chosenSpawnPoints = miscTerrainData.SpawnPoints.OrderBy(x => UnityEngine.Random.value).Take(2 * amountCreatures).ToList();

        for(int i = 0; i < chosenSpawnPoints.Count; i = i+2)
        {
            GenerateCreature(chosenSpawnPoints[i].Item1, AddTarget(chosenSpawnPoints[i+1].Item1));
        }
    }


    private void GenerateCreature(Vector3Int spawnPosition, Transform target)
    {
        var creatureConfig = FindObjectOfType<CreatureConfig>();
        GameObject newCreature;

        if (CreaturePrefab != null)
        {
            Debug.LogWarning("Loading creature from prefab!");
            if (CreaturePrefab.GetComponent(Type.GetType(AgentScriptName)) != null)
            {
                throw new ArgumentException("Creature-Prefab has AgentScript attached. Delete it to proceed.");
            }
            newCreature = Instantiate(CreaturePrefab);
            newCreature.transform.parent = creatureContainer.transform;
        }
        else
        {
            Debug.LogWarning($"Loading creature from generator with seed {creatureConfig.seed}!");

            newCreature = creatureConfig.creatureType switch
            {
                CreatureType.Biped => CreatureGenerator.ParametricBiped((CreatureGeneratorSettings)CreatureGeneratorSettings,
                                        (BipedSettings)ParametricCreatureSettings2Legged,
                                        creatureConfig.seed,
                                        (JointLimitOverrides) BipedJointLimitOverrides),
                CreatureType.Quadruped => CreatureGenerator.ParametricQuadruped((CreatureGeneratorSettings)CreatureGeneratorSettings,
                                        (QuadrupedSettings)ParametricCreatureSettings4Legged,
                                        creatureConfig.seed,
                                        (JointLimitOverrides) QuadrupedJointLimitOverrides),
                _ => throw new ArgumentException("No creature type selected"),
            };
            newCreature.transform.parent = creatureContainer.transform;
        }


        if (newCreature.transform.Find("Orientation Cube") == null)
        {
            var orientationCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            orientationCube.name = "Orientation Cube";
            Destroy(orientationCube.GetComponent<Collider>());
            Destroy(orientationCube.GetComponent<MeshRenderer>());
            orientationCube.transform.parent = newCreature.transform;
        }

        newCreature.name = "Creature";
        newCreature.transform.localPosition = spawnPosition;

        GenericAgent agentScript = (GenericAgent) newCreature.AddComponent(Type.GetType(AgentScriptName));

        if (agentScript == null)
            throw new ArgumentException("Agent class name is wrong or does not exits in this context.");
        if (newCreature.GetComponent<ModelOverrider>() == null) newCreature.AddComponent<ModelOverrider>();
        if (Application.isEditor && newCreature.GetComponent<DebugScript>() == null) newCreature.AddComponent<DebugScript>();


        agentScript._target = target;
    }

    private Transform AddTarget(Vector3Int spawnPosition)
    {
        var target = Instantiate(TargetCubePrefab, spawnPosition, Quaternion.identity);
        target.transform.parent = targetContainer.transform;
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();
        target.AddComponent<NavMeshAgent>();

        return target.transform;
    }

}


