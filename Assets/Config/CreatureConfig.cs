using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Windows;


public class CreatureConfig : MonoBehaviour
{
    [Header("Creature Settings")] [Space(10)] [SerializeField]
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

    public void Awake()
    {
        if(HeadGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Head, HeadGroundContactPenalty);
        if(TorsoGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Torso, TorsoGroundContactPenalty);
        if(HipsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Hip, HipsGroundContactPenalty);
        if(LegsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Leg, LegsGroundContactPenalty);
        if(ArmsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Arm, ArmsGroundContactPenalty);
        if(HandsGroundContactPenalty > 0) PenaltiesForBodyParts.Add(BoneCategory.Hand, HandsGroundContactPenalty);

        
        if (!Application.isEditor)
        {
        }
        else
        {
            Debug.Log("Savin");
            SaveSettingToDisk();
        }
    }
    
    

    private void SaveSettingToDisk()
    {   
        try
        {
            Debug.Log(Application.persistentDataPath + "/creatureConfig.json");
            // It seems like Unitys own File implementation is windows exclusive :clown:
            // Change only if you know it compiles for Linux
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/creatureConfig.json",
                Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)));
        }
        catch (Exception)
        {
            Debug.LogError("Could not write setting file");
        }
    }
}
