using Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using Unity.MLAgentsExamples;
using UnityEngine;

public class AdvancedEnvironmentGenerator : GenericEnvironmentGenerator
{
    public string AgentScriptName = "AgentNavMesh";

    [SerializeField] private GameObject CreaturePrefab;

    [SerializeField] public int amountCreatures = 1;

    [HideInInspector] public GameObject TargetCubePrefab;
    [HideInInspector] private ScriptableObject CreatureGeneratorSettings;
    [HideInInspector] private ScriptableObject ParametricCreatureSettings2Legged;
    [HideInInspector] private ScriptableObject ParametricCreatureSettings4Legged;
    [HideInInspector] private ScriptableObject _worldGenertorSettingsConfig;

    [HideInInspector] private SPTC_WG sptc_wg;
    [HideInInspector] private MiscTerrainData miscTerrainData;

    [HideInInspector] private GameObject creatureContainer;

    void Awake()
    {
        TargetCubePrefab = Resources.Load("TargetCube", typeof(GameObject)) as GameObject;

        // Load ScriptableObject Configs
        CreatureGeneratorSettings =
            Resources.Load("CreatureGeneratorSettings", typeof(ScriptableObject)) as ScriptableObject;
        ParametricCreatureSettings2Legged =
            Resources.Load("BipedSettings", typeof(ScriptableObject)) as ScriptableObject;
        ParametricCreatureSettings4Legged =
            Resources.Load("QuadrupedSettings", typeof(ScriptableObject)) as ScriptableObject;
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

        // Choose Random Spawn Points
        List<System.Tuple<Vector3Int, int>> chosenSpawnPoints = miscTerrainData.SpawnPoints.OrderBy(x => UnityEngine.Random.value).Take(amountCreatures + 1).ToList();
        AddTarget(chosenSpawnPoints.First().Item1);
        chosenSpawnPoints.RemoveAt(0);

        foreach (System.Tuple<Vector3Int, int> spawnPoint in chosenSpawnPoints)
        {
            GenerateCreature(spawnPoint.Item1);
        }
    }


    private void GenerateCreature(Vector3Int spawnPosition)
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
                                        creatureConfig.seed),
                CreatureType.Quadruped => CreatureGenerator.ParametricQuadruped((CreatureGeneratorSettings)CreatureGeneratorSettings,
                                        (QuadrupedSettings)ParametricCreatureSettings4Legged,
                                        creatureConfig.seed),
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


        if (newCreature.AddComponent(Type.GetType(AgentScriptName)) == null)
            throw new ArgumentException("Agent class name is wrong or does not exits in this context.");
        if (newCreature.GetComponent<ModelOverrider>() == null) newCreature.AddComponent<ModelOverrider>();
        if (Application.isEditor && newCreature.GetComponent<DebugScript>() == null) newCreature.AddComponent<DebugScript>();
    }

    private void AddTarget(Vector3Int spawnPosition)
    {
        var target = Instantiate(TargetCubePrefab, spawnPosition, Quaternion.identity);
        target.name = "Creature Target";
        target.AddComponent<WalkTargetScript>();
    }
}
