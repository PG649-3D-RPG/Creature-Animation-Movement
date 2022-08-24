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
    [SerializeField]
    public float ArmsGroundContactPenalty = 0;
    [SerializeField]
    public float HandsGroundContactPenalty = 0;
    [SerializeField]
    public float HeadGroundContactPenalty = 0;
    [SerializeField]
    public float HipsGroundContactPenalty = 0;
    [SerializeField]
    public float LegsGroundContactPenalty = 0;
    [SerializeField]
    public float TorsoGroundContactPenalty = 0;
    [SerializeField]
    public List<BoneCategory> ResetOnGroundContactParts = new() { BoneCategory.Head };

    public readonly Dictionary<BoneCategory, float> PenaltiesForBodyParts = new() {};

    protected override void ExecuteAtLoad()
    {
        if(HeadGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Head, HeadGroundContactPenalty);
        if(TorsoGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Torso, TorsoGroundContactPenalty);
        if(HipsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Hip, HipsGroundContactPenalty);
        if(LegsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Leg, LegsGroundContactPenalty);
        if(ArmsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Arm, ArmsGroundContactPenalty);
        if(HandsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Hand, HandsGroundContactPenalty);
    }
}
