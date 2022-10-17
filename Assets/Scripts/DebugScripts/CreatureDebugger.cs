using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class CreatureDebugger : MonoBehaviour
{
    [SerializeField] public bool ActivateCreatureDebugger = false;
    [SerializeField] public int Seed = 0;
    [SerializeField] public ScriptableObject CreatureGeneratorSettings;
    [SerializeField] public ScriptableObject ParametricCreatureSettings;
    [SerializeField] public bool DisablePhysics;
    [SerializeField] public CreatureType CreatureType;
    

    private GameObject creature;
    private GameObject debugCreature;


    private void Start()
    {
        if(CreatureGeneratorSettings is null || ParametricCreatureSettings is null) Debug.LogError("No config object for creature.");
        if(DisablePhysics) Physics.autoSimulation = false;
        else CreateDummyTerrain();
    }

    private void CreateDummyTerrain()
    {
        var terrainObj = new GameObject
        {
            name = $"Terrain",
            transform =
            {
                localPosition = new Vector3(-50, -1, -50)
            }
        };
        var terrain = terrainObj.AddComponent<Terrain>();
        terrain.terrainData = new TerrainData();
        var colliderObj = terrain.AddComponent<TerrainCollider>();
        colliderObj.terrainData = terrain.terrainData;
        colliderObj.terrainData.size = new Vector3(250, 0, 250);
        terrain.materialTemplate = Resources.Load("GridMatFloor", typeof(Material)) as Material;

        
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Seed++;
            GenCreature();
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            Seed--;
            GenCreature();
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            GenCreature();
        }
    }

    private void GenCreature()
    {
        Destroy(debugCreature);
        creature = CreatureType switch
        {
            CreatureType.Biped => CreatureGenerator.ParametricBiped(
                (CreatureGeneratorSettings)CreatureGeneratorSettings,
                (ParametricCreatureSettings)ParametricCreatureSettings, Seed),
            CreatureType.Quadruped => CreatureGenerator.ParametricQuadruped(
                (CreatureGeneratorSettings)CreatureGeneratorSettings,
                (ParametricCreatureSettings)ParametricCreatureSettings, Seed),
            _ => creature
        };

        debugCreature = new GameObject
        {
            name = $"DebugCreature_{Seed}"
        };
        creature.transform.parent = debugCreature.transform;
    }
}
