using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using LSystem;


public static class BoneAdd{
    public static readonly Dictionary<char, BoneCategory> literalCategoryMap = new Dictionary<char, BoneCategory>{
        {'A',BoneCategory.Arm},
        {'L',BoneCategory.Leg},
        {'T',BoneCategory.Torso},
        {'C',BoneCategory.Head},
        {'H',BoneCategory.Hand},
        {'V',BoneCategory.Foot},
        {'U',BoneCategory.Shoulder},
        {'G',BoneCategory.Hip},
        {'S',BoneCategory.Other}
    };

        public static readonly Dictionary<BoneCategory, char> categoryLiteralMap = new Dictionary<BoneCategory, char>{
        {BoneCategory.Arm,'A'},
        {BoneCategory.Leg,'L'},
        {BoneCategory.Torso,'T'},
        {BoneCategory.Head,'C'},
        {BoneCategory.Hand,'H'},
        {BoneCategory.Foot,'V'},
        {BoneCategory.Shoulder,'U'},
        {BoneCategory.Hip,'G'},
        {BoneCategory.Other,'S'}
    };

    public static readonly Dictionary<char, string> literalStringMap = new Dictionary<char, string>{
        {'A',"arm"},
        {'L',"leg"},
        {'T',"torso"},
        {'C',"head"},
        {'H',"hand"},
        {'V',"foot"},
        {'U',"shoulder"},
        {'G',"hip"},
        {'S',"other"}
    };
    public static readonly Dictionary<BoneCategory, SoftJointLimit> defaultLowXLimit = new Dictionary<BoneCategory, SoftJointLimit>{
        {BoneCategory.Arm, new SoftJointLimit() {limit = -45}},
        {BoneCategory.Leg, new SoftJointLimit() {limit = 0}},
        {BoneCategory.Torso, new SoftJointLimit() {limit = -15}},
        {BoneCategory.Head, new SoftJointLimit() {limit = -45}},
        {BoneCategory.Hand, new SoftJointLimit() {limit = -45}},
        {BoneCategory.Foot, new SoftJointLimit() {limit = -45}},
        {BoneCategory.Shoulder, new SoftJointLimit() {limit = -5}},
        {BoneCategory.Hip, new SoftJointLimit() {limit = -5}},
        {BoneCategory.Other, new SoftJointLimit() {limit = 0}}
    };

    public static readonly Dictionary<BoneCategory, SoftJointLimit> defaultHighXLimit = new Dictionary<BoneCategory, SoftJointLimit>{
        {BoneCategory.Arm, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Leg, new SoftJointLimit() {limit = 145}},
        {BoneCategory.Torso, new SoftJointLimit() {limit = 15}},
        {BoneCategory.Head, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Hand, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Foot, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Shoulder, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Hip, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Other, new SoftJointLimit() {limit = 0}}
    };

    public static readonly Dictionary<BoneCategory, SoftJointLimit> defaultYLimit = new Dictionary<BoneCategory, SoftJointLimit>{
        {BoneCategory.Arm, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Leg, new SoftJointLimit() {limit = 15}},
        {BoneCategory.Torso, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Head, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Hand, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Foot, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Shoulder, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Hip, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Other, new SoftJointLimit() {limit = 0}}
    };
    public static readonly Dictionary<BoneCategory, SoftJointLimit> defaultZLimit = new Dictionary<BoneCategory, SoftJointLimit>{
        {BoneCategory.Arm, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Leg, new SoftJointLimit() {limit = 15}},
        {BoneCategory.Torso, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Head, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Hand, new SoftJointLimit() {limit = 45}},
        {BoneCategory.Foot, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Shoulder, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Hip, new SoftJointLimit() {limit = 5}},
        {BoneCategory.Other, new SoftJointLimit() {limit = 0}}
    };

}

public enum BoneCategory{
    Leg,
    Arm,
    Torso,
    Head,
    Hand,
    Foot,
    Shoulder,
    Hip,
    Other
}

public class SkeletonGenerator
{
    static float BoneTreeRadius = 0.025f;
    // Human density if apparently about 1000kg/m^3
    static float BodyDensity = 1000.0f;

    class BoneTree{
        public Tuple<Vector3, Vector3> segment;

        public BoneTree root;

        public BoneTree parent;

        public List<BoneTree> children;
        public int limbIndex;

        public int boneIndex;

        private GameObject go;

        private Tuple<int,char> t;

        public BoneCategory boneCategory;

        public BoneTree(Tuple<Vector3, Vector3> segment, BoneTree root, BoneTree parent, Tuple<int,char> t) {
            this.segment = segment;
            this.root = root;
            this.parent = parent;
            this.t = t;

            this.boneCategory = BoneAdd.literalCategoryMap[t.Item2];
            children = new List<BoneTree>();
        }

        public String Name() {
            return BoneAdd.literalStringMap[BoneAdd.categoryLiteralMap[this.boneCategory]] + "_" + this.limbIndex + "_" + this.boneIndex;
        }

        public GameObject toGameObjectTree() {
            go = this.ToGameObject();
            foreach (BoneTree child in children) {
                child.toGameObjectTree();
            }
            return go;
        }

        public BoneTree findParent(Tuple<Vector3, Vector3> segment, bool inverse = false) {
            if (!inverse && this.segment.Item2 == segment.Item1) {
                return this;
            }
            else if(inverse && this.segment.Item1 == segment.Item1){
                return this;
            }
            foreach (BoneTree child in children) {
                var res = child.findParent(segment, inverse);
                if (res != null) {
                    return res;
                }
            }
            return null;
        }

        private GameObject ToGameObject() {
            Vector3 start = segment.Item1;
            Vector3 end = segment.Item2;
            float length = Vector3.Distance(start, end);
            bool isRoot = root == null;

            GameObject result = new GameObject(Name());
            result.transform.rotation = Quaternion.LookRotation(end-start) * Quaternion.FromToRotation(Vector3.forward, Vector3.down);//Quaternion.FromToRotation(start,end);
            result.transform.position = start + ((end-start).normalized*(length/2));  
            //result.transform.localPosition = result.transform.localRotation * Vector3.forward * length * 1.5f; 

            Rigidbody rb = result.AddComponent<Rigidbody>();

            Bone bone = result.AddComponent<Bone>();
            bone.category = boneCategory;
            bone.limbIndex = limbIndex;
            bone.boneIndex = boneIndex;
            
            if (isRoot) {
                result.AddComponent<Skeleton>();
            } else {
                GameObject parentGo = parent.go;
                result.transform.parent = parentGo.transform;

                ConfigurableJoint joint = result.AddComponent<ConfigurableJoint>();
                //joint.transform.rotation = result.transform.rotation;
                //joint.targetRotation = Quaternion.LookRotation(end-start);
                joint.anchor = new Vector3(0,-length/2,0);

                joint.connectedBody = parentGo.GetComponent<Rigidbody>();
                joint.connectedAnchor = parentGo.transform.position;

                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;

                joint.lowAngularXLimit = BoneAdd.defaultLowXLimit[boneCategory];
                joint.highAngularXLimit = BoneAdd.defaultHighXLimit[boneCategory];
                joint.angularYLimit = BoneAdd.defaultYLimit[boneCategory];
                joint.angularZLimit = BoneAdd.defaultZLimit[boneCategory];

                GameObject rootGo = root.go;
                Skeleton skeleton = rootGo.GetComponent<Skeleton>();
                skeleton.bonesByCategory[boneCategory].Add(result);
            }

            if(start != end){
                GameObject meshObject;
                if(boneCategory == BoneCategory.Hand){
                    meshObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    meshObject.transform.localScale = new Vector3(0.1f, 0.1f ,0.1f);

                    SphereCollider collider = result.AddComponent<SphereCollider>();
                    float r = 0.1f;
                    // NOTE(markus): Needs to be scaled by anther factor of 0.1, not quite sure why
                    collider.radius = 0.1f * r;

                    rb.mass = BodyDensity * (3.0f * (float)Math.PI * r * r * r) / 4.0f;
                }
                else if (boneCategory == BoneCategory.Foot) {
                    meshObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Vector3 size = new Vector3(0.1f, length * 0.9f, 0.05f);
                    meshObject.transform.localScale = size;

                    BoxCollider collider = result.AddComponent<BoxCollider>();
                    collider.size = size;

                    rb.mass = BodyDensity * (size.x * size.y * size.z);
                } else {
                    meshObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    meshObject.transform.localScale = new Vector3(0.1f,length*0.45f,0.1f);

                    CapsuleCollider collider = result.AddComponent<CapsuleCollider>();
                    collider.height = length;
                    collider.radius = BoneTreeRadius;

                    // Ellipsoid Volume is 3/4 PI abc, with radii a, b, c
                    rb.mass = BodyDensity * (3.0f * (float)Math.PI * 0.1f * length * 0.45f * 0.1f) / 4;
                }
                meshObject.transform.parent = result.transform;
                meshObject.transform.position = result.transform.position;
                meshObject.transform.rotation = result.transform.rotation;            
            }
            return result;
        }
    }
    
    private static BoneTree GenerateBoneTree(LSystem.LSystem l) {
        var segments = l.segments;
        if (segments.Count == 0) return null;

        Dictionary<BoneCategory, int> limbIndices = new();
        BoneTree root = new BoneTree(l.segments[0], null, null, l.fromRule[0]);

        foreach (var (segment, rule) in segments.Zip(l.fromRule, (a, b) => (a, b)).Skip(1)) {
            BoneTree parent = root.findParent(segment);
            BoneTree child = new BoneTree(segment, root, parent, rule);
            parent.children.Add(child);

            if (child.boneCategory == parent.boneCategory) {
                // Same Limb, keep limb index from parent, increment bone index
                child.limbIndex = parent.limbIndex;
                child.boneIndex = parent.boneIndex + 1;
            } else {
                // Start of new Limb, grab next limbIndex, increment dictionary for next limb
                int limbIndex;
                limbIndices.TryGetValue(child.boneCategory, out limbIndex);
                limbIndices[child.boneCategory] = limbIndex;
                child.limbIndex = limbIndex;
                child.boneIndex = 0;

                limbIndices[child.boneCategory]++;
            }
        }
        return root;
    }

    public static GameObject Generate(LSystem.LSystem l) {
        BoneTree root = GenerateBoneTree(l);
        GameObject rootGo = root.toGameObjectTree();
        return rootGo;
    }

}