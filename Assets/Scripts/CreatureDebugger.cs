using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureDebugger : MonoBehaviour
{
    [SerializeField] public bool ActivateCreatureDebugger = false;
    [SerializeField] public int Seed = 0;
    [SerializeField] public ScriptableObject CreatureGeneratorSettings;
    [SerializeField] public ScriptableObject ParametricCreatureSettings;


    private GameObject creature;
    private GameObject debugCreature;

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
        creature = CreatureGenerator.ParametricBiped((CreatureGeneratorSettings)CreatureGeneratorSettings, (ParametricCreatureSettings)ParametricCreatureSettings, Seed);
        debugCreature = new GameObject
        {
            name = $"DebugCreature_{Seed}"
        };
        creature.transform.parent = debugCreature.transform;
    }
}
