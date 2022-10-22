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
    [SerializeField] public bool EnableStabilityHack;
    [SerializeField] public CreatureType CreatureType;
    

    private GameObject creature;
    private GameObject debugCreature;

    private float max = 0f;



    private void Start()
    {
        if (ActivateCreatureDebugger)
        {
            if (CreatureGeneratorSettings is null || ParametricCreatureSettings is null) Debug.LogError("No config object for creature.");
        }
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
        if (ActivateCreatureDebugger)
        {
            Physics.autoSimulation = !DisablePhysics;
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
    }

    void FixedUpdate()
    {
        if (ActivateCreatureDebugger)
        {
            if (creature != null)
            {
                foreach (var v in creature.GetComponentsInChildren<Rigidbody>())
                {
                    var force = Vector3.Magnitude(v.velocity) * v.mass;
                    if (force > max)
                    {
                        Debug.Log($"Part {v.transform.name} with force {force}");
                        max = force;
                    }

                }
            }
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

        if(EnableStabilityHack) StabilityHack();
        
        debugCreature = new GameObject
        {
            name = $"DebugCreature_{Seed}"
        };
        creature.transform.parent = debugCreature.transform;
    }

    private void StabilityHack()
    {
        foreach (var v in creature.GetComponentsInChildren<ConfigurableJoint>())
        {
            v.enablePreprocessing = false;
            v.slerpDrive = new JointDrive {maximumForce = 0, positionDamper = 0, positionSpring = 0};
            v.breakTorque = 10000000;
            v.breakForce = 10000000;
        }
    }
}
