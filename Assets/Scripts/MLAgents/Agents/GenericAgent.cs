using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
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
    protected Vector3 _initialCenterOfMass;
    protected float _creatureHeight;
    protected NavMeshPath _path;
    protected Vector3 _nextPathPoint;


    // Scripts
    protected GenericEnvironmentGenerator _deg;
    protected WalkTargetScript _walkTargetScript;
    protected Transform _target;
    protected OrientationCubeController _orientationCube;
    protected JointDriveController _jdController;
    protected DecisionRequester _decisionRequester;
    protected Agent _agent;
    protected MlAgentConfig _mlAgentsConfig;
    protected ArenaConfig _arenaSettings;
    protected CreatureConfig _creatureConfig;
    protected BehaviorParameters bpScript;

    public float MTargetWalkingSpeed;
    public const float YHeightOffset = 0.1f;

    public void Awake()
    {
        _deg = FindObjectOfType<GenericEnvironmentGenerator>();
        if(GetComponent<JointDriveController>() != null) Destroy(GetComponent<DecisionRequester>());
        if (GetComponent<DecisionRequester>() != null) Destroy(GetComponent<JointDriveController>());

        _decisionRequester = this.AddComponent<DecisionRequester>();
        _jdController = this.AddComponent<JointDriveController>();
        _mlAgentsConfig = FindObjectOfType<MlAgentConfig>();
        _arenaSettings = FindObjectOfType<ArenaConfig>();
        _creatureConfig = FindObjectOfType<CreatureConfig>();
        bpScript = GetComponent<BehaviorParameters>();


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

    public override void Initialize()
    {
        _walkTargetScript = FindObjectOfType<WalkTargetScript>(); 
        _agent = gameObject.GetComponent<Agent>();
        _target = GameObject.Find("Creature Target").transform;
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
        var terrainHeight = _deg.TerrainObject.GetComponent<Terrain>().SampleHeight(position);

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
            _topStartingRotation.eulerAngles.z + Random.Range(-5, 5));
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
        SetWalkerOnGround();
        if(Application.isEditor) Debug.Log($"A new Episode has just begun");
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
        CalculateInitialValues();
    }

    private void CalculateInitialValues()
    {
        _initialCenterOfMass = CalculateCenterOfMass(_topTransform, out var _);
        
        Rigidbody minx, maxx, miny, maxy, minz, maxz; 
        minx = maxx = miny = maxy = minz = maxz = _topTransform.GetComponentInChildren<Rigidbody>();
        
        foreach (var rb in _topTransform.GetComponentsInChildren<Rigidbody>())
        {
            if (rb.transform.position.x <= minx.position.x)
            {
                minx = rb;
            }
            if (rb.transform.position.x >= maxx.position.x)
            {
                maxx = rb;
            }
            
            if (rb.transform.position.y <= miny.position.y)
            {
                miny = rb;
            }
            if (rb.transform.position.y >= maxy.position.y)
            {
                maxy = rb;
            }
            
            if (rb.transform.position.z <= minz.position.z)
            {
                minz = rb;
            }
            if (rb.transform.position.z >= maxz.position.z)
            {
                maxz = rb;
            }
        }

        // Will only work for the biped
        _creatureHeight = maxy.transform.GetComponent<Collider>().bounds.max.y -
                     miny.transform.GetComponent<Collider>().bounds.min.y;
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

    private void InitializeBehaviorParameters()
    {
        // Set behavior parameters
        bpScript.BrainParameters.VectorObservationSize = _mlAgentsConfig.ObservationSpace;
        bpScript.BehaviorName = BehaviorName;

        if(_deg.NnModels.Count > 0)
        {
            bpScript.Model = _deg.NnModels[0];
        }

        if(_mlAgentsConfig.CalculateActionSpace)
        {
            // Will assume no discrete branches
            bpScript.BrainParameters.ActionSpec = new ActionSpec(CalculateNumberContinuousActions(), Array.Empty<int>());
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
        }

        foreach(var (trans, bodyPart) in _jdController.bodyPartsDict)
        {
            bodyPart.BodyPartHeight = trans.position.y - minYBodyPartCoordinate;
        }

        _otherBodyPartHeight = _topTransform.position.y - minYBodyPartCoordinate;
    }

    protected void SwitchModel(Func<int> f)
    {
        var newNetworkIndex = f();

        if(newNetworkIndex < _deg.NnModels.Count)
        {
            var newNetwork = _deg.NnModels[newNetworkIndex];

            if (bpScript.Model != newNetwork)
            {
                bpScript.Model = newNetwork;
            }
        }

    }

    protected Vector3 GetNextPathPoint()
    {
        var pathValid = NavMesh.CalculatePath(_topTransform.position, _target.position, NavMesh.AllAreas, _path);

        if (_path.corners.Length == 0 || !pathValid)
        {
            if (NavMesh.SamplePosition(_topTransform.position, out var hitIndicator, 20, NavMesh.AllAreas))
            {
                return hitIndicator.position;
            }

            Debug.LogError("Could not find close NavMesh edge.");
        }

        return _path.corners[1] + new Vector3(0, 2 * _topStartingPosition.y, 0);
    }

    protected int DetermineModel()
    {
        return 0;
    }
}
