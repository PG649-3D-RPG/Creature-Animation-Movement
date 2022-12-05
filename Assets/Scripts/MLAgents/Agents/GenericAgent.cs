using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Config;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class GenericAgent : Agent
{

    private const string BehaviorName = "Walker";

    // Internal values
    protected float _otherBodyPartHeight = 1f;
    protected Vector3 _topStartingPosition;
    protected Quaternion _topStartingRotation;
    protected Transform _topTransform;
    protected Rigidbody _topTransformRb;
    protected Transform _headTransform;
    protected Vector3 _headPosition;
    protected long _episodeCounter = 0;
    protected Vector3 _dirToWalk = Vector3.right;
    protected Vector3 _initialCenterOfMass;
    protected float _xLength;
    protected float _yLength;
    protected float _zLength;
    protected Vector3 _avgUpOrientation;
    protected Vector3 _avgForwardOrientation;
    protected Vector3 _avgRightOrientation;
    protected List<Transform> _footTransforms = new();
    protected List<GroundContact> _footGCScript = new();


    // Scripts
    protected DynamicEnvironmentGenerator _deg;
    protected TerrainGenerator _terrainGenerator;
    protected WalkTargetScript _walkTargetScript;
    protected Transform _target;
    protected OrientationCubeController _orientationCube;
    protected JointDriveController _jdController;
    protected DecisionRequester _decisionRequester;
    protected Agent _agent;
    protected MlAgentConfig _mlAgentsConfig;
    protected ArenaConfig _arenaSettings;
    protected CreatureConfig _creatureConfig;

    public float MTargetWalkingSpeed;
    public const float YHeightOffset = 0.1f;

    public void Awake()
    {
        _deg = FindObjectOfType<DynamicEnvironmentGenerator>();
        if(GetComponent<JointDriveController>() != null) Destroy(GetComponent<DecisionRequester>());
        if (GetComponent<DecisionRequester>() != null) Destroy(GetComponent<JointDriveController>());

        _decisionRequester = this.AddComponent<DecisionRequester>();
        _jdController = this.AddComponent<JointDriveController>();
        _mlAgentsConfig = FindObjectOfType<MlAgentConfig>();
        _arenaSettings = FindObjectOfType<ArenaConfig>();
        _creatureConfig = FindObjectOfType<CreatureConfig>();

        // Config decision requester
        _decisionRequester.DecisionPeriod = _mlAgentsConfig.DecisionPeriod;
        _decisionRequester.TakeActionsBetweenDecisions = _mlAgentsConfig.TakeActionsBetweenDecisions;
        
        // Config jdController
        _jdController.maxJointForceLimit = _mlAgentsConfig.MaxJointForceLimit;
        _jdController.jointDampen = _mlAgentsConfig.JointDampen;
        _jdController.maxJointSpring = _mlAgentsConfig.MaxJointSpring;
        
        // Set agent settings (maxSteps)
        var mAgent = gameObject.GetComponent<Agent>();
        mAgent.MaxStep = _mlAgentsConfig.MaxStep;

        SetUpBodyParts();
        
        InitializeBehaviorParameters();
        
        
    }

    protected abstract int CalculateNumberContinuousActions();
    protected abstract int CalculateNumberDiscreteBranches();

    public override void Initialize()
    {
        var parent = transform.parent;
        _terrainGenerator = parent.GetComponentInChildren<TerrainGenerator>();
        _walkTargetScript = parent.GetComponentInChildren<WalkTargetScript>();
        _agent = gameObject.GetComponent<Agent>();
        _target = parent.Find("Creature Target").transform;
        MTargetWalkingSpeed = _mlAgentsConfig.TargetWalkingSpeed;
        var oCube = transform.Find("Orientation Cube");
        _orientationCube = oCube.GetComponent<OrientationCubeController>();
        if(_orientationCube == null) _orientationCube = oCube.AddComponent<OrientationCubeController>();
        
        SetWalkerOnGround();
    }
    /// <summary>
    /// Set the walker on the terrain.
    /// </summary>
    protected void SetWalkerOnGround()
    {
        var position = _topTransform.position;
        var terrainHeight = _terrainGenerator.GetTerrainHeight(position);

        position = new Vector3(_topStartingPosition.x, terrainHeight + _otherBodyPartHeight + YHeightOffset, _topStartingPosition.z);
        _topTransform.position = position;
        _topTransform.localRotation = _topStartingRotation;


        _topTransformRb.velocity = Vector3.zero;
        _topTransformRb.angularVelocity = Vector3.zero;

        //Reset all of the body parts
        foreach (var bodyPart in _jdController.bodyPartsDict.Values.AsParallel())
        {
            bodyPart.Reset(bodyPart, terrainHeight, YHeightOffset);
        }

        var rotation = new Vector3(_topStartingRotation.eulerAngles.x, Random.Range(0.0f, 360.0f),
            _topStartingRotation.eulerAngles.z + Random.Range(0, 5));
        while (rotation == Vector3.zero)
        {
            rotation = new Vector3(_topStartingRotation.eulerAngles.x, Random.Range(0.0f, 360.0f),
                _topStartingRotation.eulerAngles.z + Random.Range(-5, 5));
            Debug.LogError("Fixing zero vector rotation!");
        }
        _topTransform.localRotation = Quaternion.Euler(rotation);
    }

    /// <summary>
    /// Is called on episode begin.
    /// Loop over body parts and reset them to initial conditions.
    /// Regenerate terrain and place target cube randomly 
    /// </summary>
    public override void OnEpisodeBegin()
    {
        _episodeCounter++;

        // Order is important. First regenerate terrain -> than place cube!
        if (_arenaSettings.RegenerateTerrainAfterXEpisodes > 0 && _episodeCounter % _arenaSettings.RegenerateTerrainAfterXEpisodes == 0)
        {
            _terrainGenerator.RegenerateTerrain();
        }

        if (_arenaSettings.EpisodeCountToRandomizeTargetCubePosition > 0 && _episodeCounter % _arenaSettings.EpisodeCountToRandomizeTargetCubePosition == 0)
        {
            _walkTargetScript.PlaceTargetCubeRandomly();
        }

        SetWalkerOnGround();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            _mlAgentsConfig.RandomizeWalkSpeedEachEpisode ? Random.Range(0.1f, _mlAgentsConfig.MaxWalkingSpeed) : MTargetWalkingSpeed;
    }
    
    protected Vector3 GetAvgVelocityOfCreature()
    {
        return _jdController.bodyPartsList.Select(x => x.rb.velocity)
            .Aggregate(Vector3.zero, (x, y) => x + y) / _jdController.bodyPartsList.Count;
    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(1f);
        _walkTargetScript.PlaceTargetCubeRandomly();
    }

    void Start()
    {
        _ = StartCoroutine(nameof(CheckWalkerOutOfArea));
        _initialCenterOfMass = CalculateCenterOfMass(_topTransform);

        
        // Creates object aligned bounding box and calcs xyz length
        var minx = new Vector3(float.MaxValue,float.MaxValue,float.MaxValue );
        var maxx = new Vector3(float.MinValue,float.MinValue,float.MinValue );
        var miny =new Vector3(float.MaxValue,float.MaxValue,float.MaxValue );
        var maxy =  new Vector3(float.MinValue,float.MinValue,float.MinValue );
        var minz = new Vector3(float.MaxValue,float.MaxValue,float.MaxValue );
        var maxz = new Vector3(float.MinValue,float.MinValue,float.MinValue );

        // Saves initial vector orientation
        var orientationCounter = 0;
        var avgOrientationUp = Vector3.zero;
        var avgOrientationForward = Vector3.zero;
        var avgOrientationRight = Vector3.zero;
        
        foreach (var rb in _topTransform.GetComponentsInChildren<Rigidbody>())
        {
            if (rb.transform.position.x <= minx.x)
            {
                minx = rb.transform.position;
            }
            if (rb.transform.position.x >= maxx.x)
            {
                maxx = rb.transform.position;
            }
            
            if (rb.transform.position.y <= miny.y)
            {
                miny = rb.transform.position;
            }
            if (rb.transform.position.y >= maxy.y)
            {
                maxy = rb.transform.position;
            }
            
            if (rb.transform.position.z <= minz.z)
            {
                minz = rb.transform.position;
            }
            if (rb.transform.position.z >= maxz.z)
            {
                maxz = rb.transform.position;
            }

            switch (rb.transform.GetComponent<Bone>().category)
            {
                case BoneCategory.Torso: // Empty on purpose
                case BoneCategory.Head:
                    orientationCounter++;
                    var transform1 = rb.transform;
                    avgOrientationForward += transform1.forward;
                    avgOrientationRight += transform1.right;
                    avgOrientationUp += transform1.up;
                    break;
            }

        }

        _xLength = maxx.x - minx.x;
        _yLength = maxy.y - miny.y;
        _zLength = maxz.z - minz.z;

        Debug.Log($"Creature size x {_xLength} y {_yLength} z {_zLength}");
        
        _avgForwardOrientation = avgOrientationForward / orientationCounter;
        _avgRightOrientation = avgOrientationRight / orientationCounter;
        _avgUpOrientation = avgOrientationUp / orientationCounter;
        
        Debug.Log($"Avg up {_avgForwardOrientation} Avg right {_avgRightOrientation} Avg forward {_avgUpOrientation}");
    }


    private IEnumerator CheckWalkerOutOfArea()
    {
        while (true)
        {
            if (_topTransform.position.y is < -10 or > 40)
            {
                _agent.EndEpisode();
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private void InitializeBehaviorParameters()
    {
        // Set behavior parameters
        var bpScript = GetComponent<BehaviorParameters>();
        bpScript.BrainParameters.VectorObservationSize = _mlAgentsConfig.ObservationSpace;
        bpScript.BehaviorName = BehaviorName;
        bpScript.Model = _deg.NnModel;

        if(_mlAgentsConfig.CalculateActionSpace)
        {
            bpScript.BrainParameters.ActionSpec = new ActionSpec(CalculateNumberContinuousActions(), new int[CalculateNumberDiscreteBranches()]);
        }
        else
        {
            bpScript.BrainParameters.ActionSpec = new ActionSpec(_mlAgentsConfig.ContinuousActionSpace, new int[_mlAgentsConfig.DiscreteBranches]);
        }
    }

    private void SetUpBodyParts()
    {
        //Get Body Parts
        //and setup each body part
        var minYBodyPartCoordinate = 0f;
        foreach (var bone in GetComponentsInChildren<Bone>())
        {
            if (!bone.isRoot)
            {
                if (bone.transform.GetComponent<GroundContact>() == null) bone.transform.AddComponent<GroundContact>();
                _jdController.SetupBodyPart(bone.transform);
            }
            else
            {
                _topTransform = bone.transform;
                _topTransformRb = bone.transform.GetComponent<Rigidbody>();

                _topStartingRotation = bone.transform.localRotation;
                _topStartingPosition = bone.transform.position;
            }
            minYBodyPartCoordinate = Math.Min(minYBodyPartCoordinate, bone.transform.position.y);
   

            if (bone.category == BoneCategory.Head)
            {
                _headTransform = bone.transform;
                _headPosition = _headTransform.position;
            }

            if (bone.category == BoneCategory.Foot)
            {
                _footTransforms.Add(bone.transform);
                _footGCScript.Add(bone.transform.GetComponent<GroundContact>());
            }
        }

        foreach(var (trans, bodyPart) in _jdController.bodyPartsDict)
        {
            bodyPart.BodyPartHeight = trans.position.y - minYBodyPartCoordinate;
        }

        _otherBodyPartHeight = _topTransform.position.y - minYBodyPartCoordinate;
    }

    protected Vector3 CalculateCenterOfMass(Transform topTransform)
    {   
        var absCoM = Vector3.zero;
        var relativeCoM = Vector3.zero;
        var c = 0f;
        
        if (topTransform is not null)
        {
            foreach (var element in topTransform.GetComponentsInChildren<Rigidbody>())
            {
                float mass;
                absCoM += element.worldCenterOfMass * (mass = element.mass);
                c += mass;
            }

            absCoM /= c;
            // This might be a little bit off. Someone might improve it.
            relativeCoM = absCoM - topTransform.transform.position;
        }

        return relativeCoM;
    }
    
    protected Vector3 CalculateCenterOfMass(Transform topTransform, out Vector3 abs)
    {   
        var absCoM = Vector3.zero;
        var relativeCoM = Vector3.zero;
        var c = 0f;
        
        if (topTransform is not null)
        {
            foreach (var element in topTransform.GetComponentsInChildren<Rigidbody>())
            {
                float mass;
                absCoM += element.worldCenterOfMass * (mass = element.mass);
                c += mass;
            }

            absCoM /= c;
            // This might be a little bit off. Someone might improve it.
            relativeCoM = absCoM - topTransform.transform.position;
        }
        abs = absCoM;

        return relativeCoM;
    }
}
