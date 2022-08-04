using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureDebugger : MonoBehaviour
{
    public GameObject Arena;

    [SerializeField] public int Seed = 0;
    [SerializeField] public ScriptableObject CreatureGeneratorSettings;
    [SerializeField] public ScriptableObject ParametricCreatureSettings;

    // Start is called before the first frame update
    void Start()
    {
        Arena = GameObject.Find("Arena 0");
        if (Arena == null) throw new ArgumentException("No arena found in scene!");

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Seed++;
            GenCreature();
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            GenCreature();
        }
    }

    public void GenCreature()
    {
        var creature = CreatureGenerator.ParametricBiped((CreatureGeneratorSettings)CreatureGeneratorSettings, (ParametricCreatureSettings)ParametricCreatureSettings, Seed);
        var debugCreature = new GameObject
        {
            name = $"DebugCreature_{Seed}"
        };
        creature.transform.parent = debugCreature.transform;
        debugCreature.transform.parent = Arena.transform;
    }
}
