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
using Random = UnityEngine.Random;

public abstract class GenericAgent : Agent
{
    
    // Internal values
    protected float _otherBodyPartHeight = 1f;
    protected Vector3 _topStartingPosition;
    protected Quaternion _topStartingRotation;
    protected Transform _topTransform;
    protected Rigidbody _topTransformRb;

    protected long _episodeCounter = 0;
    protected Vector3 _dirToWalk = Vector3.right;


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
    
    public float MTargetWalkingSpeed // property
    {
        get => _mlAgentsConfig.TargetWalkingSpeed;
        set => _mlAgentsConfig.TargetWalkingSpeed = Mathf.Clamp(value, .1f, _mlAgentsConfig.MaxWalkingSpeed);
    }

    
    public void Awake()
    {
        _deg = FindObjectOfType<DynamicEnvironmentGenerator>();
        _jdController = this.AddComponent<JointDriveController>();
        _decisionRequester = this.AddComponent<DecisionRequester>();
        _mlAgentsConfig = FindObjectOfType<MlAgentConfig>();
        _arenaSettings = FindObjectOfType<ArenaConfig>();
        
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

        // Set behavior parameters
        var skeleton = GetComponentInChildren<Skeleton>();
        var bpScript = GetComponent<BehaviorParameters>();
        bpScript.BrainParameters.ActionSpec = new ActionSpec(_mlAgentsConfig.ContinuousActionSpaceOffset, new int[_mlAgentsConfig.DiscreteBranches]);
        bpScript.BrainParameters.VectorObservationSize = _mlAgentsConfig.ObservationSpaceOffset;
        bpScript.BehaviorName = DynamicEnvironmentGenerator.BehaviorName;
        bpScript.Model = _deg.NnModel;
    }

    public override void Initialize()
    {
        var parent = transform.parent;
        _terrainGenerator = parent.GetComponentInChildren<TerrainGenerator>();
        _walkTargetScript = parent.GetComponentInChildren<WalkTargetScript>();
        _agent = gameObject.GetComponent<Agent>();
        // TODO: Update
        _target = parent.Find("Creature Target").transform;
        var oCube = transform.Find("Orientation Cube");
        _orientationCube = oCube.GetComponent<OrientationCubeController>();

        if(_orientationCube == null)
        {
            _orientationCube = oCube.AddComponent<OrientationCubeController>();
        }
         

        //Get Body Parts
        //and setup each body part

        var transforms = GetComponentsInChildren<Transform>();
        var minYBodyPartCoor = 0f;
        foreach (var trans in transforms)
        {
            // Double check if categories change!
            var boneScript = trans.GetComponent<Bone>();
            if (boneScript != null)
            {
                if(!boneScript.isRoot)
                {
                    trans.AddComponent<GroundContact>();
                    _jdController.SetupBodyPart(trans);
                }
                else
                {
                    _topTransform = trans;
                    _topTransformRb = trans.GetComponent<Rigidbody>();

                    _topStartingRotation = trans.rotation;
                    _topStartingPosition = trans.position;
                }
                minYBodyPartCoor = Math.Min(minYBodyPartCoor, trans.position.y);
            }
        }

        foreach(var (trans, bodyPart) in _jdController.bodyPartsDict)
        {
            bodyPart.BodyPartHeight = trans.position.y - minYBodyPartCoor;
        }

        _otherBodyPartHeight = _topTransform.position.y - minYBodyPartCoor;

        SetWalkerOnGround();
    }
    
    /// <summary>
    /// Set the walker on the terrain.
    /// </summary>
    protected void SetWalkerOnGround()
    {
        var position = _topTransform.position;
        var terrainHeight = _terrainGenerator.GetTerrainHeight(position);

        position = new Vector3(_topStartingPosition.x, terrainHeight + _otherBodyPartHeight + DynamicEnvironmentGenerator.YHeightOffset, _topStartingPosition.z);
        _topTransform.position = position;
        _topTransform.rotation = _topStartingRotation;


        _topTransformRb.velocity = Vector3.zero;
        _topTransformRb.angularVelocity = Vector3.zero;

        //Reset all of the body parts
        foreach (var bodyPart in _jdController.bodyPartsDict.Values.AsParallel())
        {
            bodyPart.Reset(bodyPart, terrainHeight, DynamicEnvironmentGenerator.YHeightOffset);
        }

        _topTransform.rotation = Quaternion.Euler(-90, Random.Range(0.0f, 360.0f),Random.Range(-5,5));
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
            _arenaSettings.RandomizeWalkSpeedEachEpisode ? Random.Range(0.1f, _mlAgentsConfig.MaxWalkingSpeed) : MTargetWalkingSpeed;
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
}
