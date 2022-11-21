using System;
using System.Collections.Generic;
using System.Text;
using Config;
using UnityEngine;
using UnityEngine.Windows;
using Object = UnityEngine.Object;
using System.Reflection;

public class CreatureConfig : GenericConfig
{
    [Header("Creature Settings")] [Space(10)] 
    [SerializeField]
    public int seed = 0;
    public CreatureType creatureType ;

    [Header("Penalty Settings")]
    [Space(10)]
    [SerializeField]
    public float ContactPenalty = 0;
    [SerializeField]
    public List<BoneCategory> ResetOnGroundContactParts = new() { BoneCategory.Head };
    protected override void ExecuteAtLoad()
    {
        if(ContactPenalty > 0)
        {
            Debug.LogWarning($"Positive penalty {ContactPenalty} applied! Inverting it.");
            ContactPenalty = -ContactPenalty;
        }
    }
}


public enum CreatureType
{
    Biped,
    Quadruped
}