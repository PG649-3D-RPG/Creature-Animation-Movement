using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bone : MonoBehaviour
{
    /// What type of body part this bone belongs to.
    public BoneCategory category;

    /// The index number of the limb this bone belongs to, zero based. 
    /// A bone with category "Arm" and limbIndex 0 for example belong to the skeletons first arm.
    /// Bones with identical limb indices belong to the same limb.
    public int limbIndex;

    /// The index number of the bone within its limb.
    /// Taking an arm as an example, the bone connected to the torso has boneIndex 0, the bone below that has boneIndex 1, and so on.
    public int boneIndex;
}
