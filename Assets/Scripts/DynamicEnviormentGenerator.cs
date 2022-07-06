using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;

public class DynamicEnviormentGenerator : MonoBehaviour
{
    [SerializeField] public GameObject _wallPrefab;

    [SerializeField] public GameObject _creaturePrefab;
    
    [SerializeField] public GameObject _targetCubePrefab;

    
    // Start is called before the first frame update
    void Start()
    {
        var arena = GenerateArena();
        GenerateCreature(arena);
        AddTargetToArena(arena);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private GameObject GenerateArena()
    {
        var arena = new GameObject
        {
            name = "Creature Arena"
        };

        var terrainObj =
            GameObject.Instantiate(new GameObject(), Vector3.zero, Quaternion.identity, arena.transform);
        terrainObj.name = "Terrain";
        var terrain = terrainObj.AddComponent<Terrain>();
        var terrainGenerator = terrain.AddComponent<TerrainGenerator>();
        var colliderObj =  terrain.AddComponent<TerrainCollider>(); 
        terrain.terrainData = new TerrainData();
        colliderObj.terrainData = terrain.terrainData;
        terrainGenerator.width = 128;
        terrainGenerator.length = 128;
        
        var wall1 = GameObject.Instantiate(_wallPrefab, new Vector3(64,12,126), Quaternion.identity, terrain.transform);
        wall1.name = "Wall North";
        var wall2 = GameObject.Instantiate(_wallPrefab, new Vector3(126,12,64), Quaternion.Euler(0,90,0), terrain.transform);
        wall2.name = "Wall East";
        var wall3 = GameObject.Instantiate(_wallPrefab, new Vector3(64,12,2), Quaternion.identity, terrain.transform);
        wall3.name = "Wall South";
        var wall4 = GameObject.Instantiate(_wallPrefab, new Vector3(2,12,64), Quaternion.Euler(0,90,0), terrain.transform);
        wall4.name = "Wall West";

        return arena;
    }

    public void GenerateCreature(GameObject arena)
    {
        
        var creature = GameObject.Instantiate(_creaturePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        creature.name = "Creature";
        //creature.AddComponent<WalkerAgent>();
        //creature.AddComponent<DecisionRequester>();
        //creature.AddComponent<JointDriveController>();
        //creature.AddComponent<BehaviorParameters>();
    }

    private void AddTargetToArena(GameObject arena)
    {
        var target = GameObject.Instantiate(_targetCubePrefab, new Vector3(64,12,126), Quaternion.identity, arena.transform);
        target.name = "Creature Target";
    }
}
