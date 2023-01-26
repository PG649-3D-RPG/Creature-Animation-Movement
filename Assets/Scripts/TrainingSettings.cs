using UnityEngine;
using System.Collections.Generic;
using Config;

[CreateAssetMenu(fileName ="Training", menuName = "PG649-CreatureAnimator/TrainingSettings")]
public class TrainingSettings : ScriptableObject{

    [Header("Generator Settings")]
    public string AgentName; 

    public GameObject CreaturePrefab;

    public int AmountCreatures;

    [Header("Creature Settings")]
    public int Seed;
    public CreatureType CreatureType;
    public List<BoneCategory> ResetOnGroundContactParts;

    [Header("MlAgent Settings")]
    public bool RandomizeWalkSpeedEachEpisode;
    public int MaxStep;
}