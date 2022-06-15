using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Skeleton : MonoBehaviour
{
    public Dictionary<BoneCategory, List<GameObject>> bonesByCategory;

    public Skeleton() {
        bonesByCategory = new();

        foreach (BoneCategory cat in Enum.GetValues(typeof(BoneCategory))) {
            bonesByCategory[cat] = new List<GameObject>();
        }
    }
}
