using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

public class AgentNew : GenericAgent
{
    // Generator
    private DynamicEnviormentGenerator _deg;

    // Internal values
    private float _otherBodyPartHeight = 1f;

    private Vector3 _topStartingPosition;
    private Quaternion _topStartingRotation;
    private Transform _topTransform;
    private Rigidbody _topTransformRb;

    private long _episodeCounter = 0;
    private Vector3 _dirToWalk = Vector3.right;

    // Scripts
    private TerrainGenerator _terrainGenerator;
    private WalkTargetScript _walkTargetScript;
    private Transform _target;
    private OrientationCubeController _orientationCube;
    private JointDriveController _jdController;
    private DecisionRequester _decisionRequester;
    private Agent _agent;

    public float MTargetWalkingSpeed // property
    {
        get => _deg.TargetWalkingSpeed;
        set => _deg.TargetWalkingSpeed = Mathf.Clamp(value, .1f, _deg.MaxWalkingSpeed);
    }

    public void Awake()
    {
        _deg = GameObject.FindObjectOfType<DynamicEnviormentGenerator>();
        _jdController = this.AddComponent<JointDriveController>();
        _decisionRequester = this.AddComponent<DecisionRequester>();
        _decisionRequester.TakeActionsBetweenDecisions = _deg.TakeActionsBetweenDecisions;
        _jdController.maxJointForceLimit = _deg.MaxJointForceLimit;
        _jdController.jointDampen = _deg.JointDampen;
        _jdController.maxJointSpring = _deg.MaxJointSpring;

        // Set agent settings (maxSteps)
        var mAgent = gameObject.GetComponent<Agent>();
        mAgent.MaxStep = _deg.MaxStep;

        // Set behavior parameters
        var skeleton = GetComponentInChildren<Skeleton>();
        var bpScript = GetComponent<BehaviorParameters>();
        bpScript.BrainParameters.ActionSpec = _deg.UseContinuousActionSpaceOffsetAsContinuousActionSpace
            ? new ActionSpec(_deg.ContinuousActionSpaceOffset, new int[_deg.DiscreteBranches])
            : new ActionSpec(3 * skeleton.nBones + _deg.ContinuousActionSpaceOffset, new int[_deg.DiscreteBranches]);
        bpScript.BrainParameters.VectorObservationSize = _deg.UseObservationSpaceOffsetAsObservationSpace
            ? _deg.ObservationSpaceOffset
            : 3 * skeleton.nBones + _deg.ObservationSpaceOffset;
        bpScript.BehaviorName = _deg.BehaviorName;
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
        _orientationCube = transform.Find("Orientation Cube").AddComponent<OrientationCubeController>();

        //Get Body Parts
        //and setup each body part

        var transforms = GetComponentsInChildren<Transform>();
        float minYBodyPartCoor = 0f;
        foreach (var trans in transforms)
        {
            // Double check if categories change!
            var boneScript = trans.GetComponent<Bone>();
            if (boneScript != null)
            {
                if(!boneScript.isRoot)
                {
                    var groundContact = trans.AddComponent<GroundContact>();
                    //var bodyPartHeight = trans.position.y - transform.position.y;
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
            Debug.Log($"BodyPartHeight von {trans.gameObject.name} = {bodyPart.BodyPartHeight}");
        }

        _otherBodyPartHeight = _topTransform.position.y - minYBodyPartCoor;

        SetWalkerOnGround();
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

    /// <summary>
    /// Is called on episode begin.
    /// Loop over body parts and reset them to initial conditions.
    /// Regenerate terrain and place target cube randomly 
    /// </summary>
    public override void OnEpisodeBegin()
    {
        _episodeCounter++;

        // Order is important. First regenerate terrain -> than place cube!
        if (_deg.RegenerateTerrainAfterXEpisodes > 0 && _episodeCounter % _deg.RegenerateTerrainAfterXEpisodes == 0)
        {
            _terrainGenerator.RegenerateTerrain();
        }

        if (_deg.EpisodeCountToRandomizeTargetCubePosition > 0 && _episodeCounter % _deg.EpisodeCountToRandomizeTargetCubePosition == 0)
        {
            _walkTargetScript.PlaceTargetCubeRandomly();
        }

        SetWalkerOnGround();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            _deg.RandomizeWalkSpeedEachEpisode ? Random.Range(0.1f, _deg.MaxWalkingSpeed) : MTargetWalkingSpeed;

        //Physics.gravity = new Vector3(0, Random.Range(-200, 200), 0);
        //Debug.Log($"Physics {Physics.gravity} Reward {_agent.GetCumulativeReward()}");
    }


    /// <summary>
    /// Set the walker on the terrain.
    /// </summary>
    private void SetWalkerOnGround()
    {
        var position = _topTransform.position;
        var terrainHeight = _terrainGenerator.GetTerrainHeight(position);

        position = new Vector3(_topStartingPosition.x, terrainHeight + _otherBodyPartHeight + _deg.YHeightOffset, _topStartingPosition.z);
        _topTransform.position = position;
        _topTransform.rotation = _topStartingRotation;


        _topTransformRb.velocity = Vector3.zero;
        _topTransformRb.angularVelocity = Vector3.zero;

        //Reset all of the body parts
        foreach (var bodyPart in _jdController.bodyPartsDict.Values.AsParallel())
        {
            bodyPart.Reset(bodyPart, terrainHeight, _deg.YHeightOffset);
        }

        _topTransform.rotation = Quaternion.Euler(-90, Random.Range(0.0f, 360.0f),Random.Range(-5,5));
    }


    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    private void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.TouchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(_orientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(_orientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        // TODO Why do we do this?
        sensor.AddObservation(_orientationCube.transform.InverseTransformDirection(bp.rb.position - _topTransform.position));

        if (bp.rb.transform.GetComponent<Bone>().category != BoneCategory.Hand)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / _jdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = _orientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * MTargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocityOfCreature();

        //current ragdoll velocity. normalized
        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        sensor.AddObservation(_orientationCube.transform.InverseTransformDirection(avgVel));
        //vel goal relative to cube
        sensor.AddObservation(_orientationCube.transform.InverseTransformDirection(velGoal));

        //rotation deltas
        sensor.AddObservation(Quaternion.FromToRotation(_topTransform.forward, cubeForward));

        //Position of target position relative to cube
        sensor.AddObservation(_orientationCube.transform.InverseTransformPoint(_target.transform.position));

        foreach (var bodyPart in _jdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
            //rotation deltas for the head
            if (bodyPart.rb.transform.GetComponent<Bone>().category == BoneCategory.Head) sensor.AddObservation(Quaternion.FromToRotation(bodyPart.rb.transform.forward, cubeForward));
        }

        sensor.AddObservation(_topTransformRb.position.y);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var bpList = _jdController.bodyPartsList;
        var i = -1;

        var continuousActions = actionBuffers.ContinuousActions;
        // TODO Needs to be reworked for generalization
        foreach (var parts in bpList)
        {
            var xTarget = parts.joint.angularXMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            var yTarget = parts.joint.angularYMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            var zTarget = parts.joint.angularZMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            parts.SetJointTargetRotation(xTarget, yTarget, zTarget);
            parts.SetJointStrength(continuousActions[++i]);
        }
    }

    public void FixedUpdate()
    {
        //Update OrientationCube and DirectionIndicator
        _dirToWalk = _target.position - _topTransform.position;
        _orientationCube.UpdateOrientation(_topTransform, _target);

        var cubeForward = _orientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * MTargetWalkingSpeed, GetAvgVelocityOfCreature());

        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = (Vector3.Dot(cubeForward, _topTransform.forward) + 1) * 0.5f;

        if (float.IsNaN(lookAtTargetReward) || float.IsNaN(matchSpeedReward)) throw new ArgumentException($"A reward is NaN. float.");
        //Debug.Log($"matchSpeedReward {Math.Max(matchSpeedReward, 0.1f)} lookAtTargetReward {Math.Max(lookAtTargetReward, 0.1f)}");
        AddReward(Math.Max(matchSpeedReward, 0.1f) * Math.Max(lookAtTargetReward, 0.1f));
    }

    private Vector3 GetAvgVelocityOfCreature()
    {
        return _jdController.bodyPartsList.Select(x => x.rb.velocity)
            .Aggregate(Vector3.zero, (x, y) => x + y) / _jdController.bodyPartsList.Count;
    }

    /// <summary>
    /// normalized value of the difference in avg speed vs goal walking speed.
    /// </summary>
    /// <param name="velocityGoal"></param>
    /// <param name="actualVelocity"></param>
    /// <returns>Return the value on a declining sigmoid shaped curve that decays from 1 to 0. This reward will approach 1 if it matches perfectly and approach zero as it deviates. </returns>
    private float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, MTargetWalkingSpeed);
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);
    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public override void TouchedTarget()
    {
        AddReward(1f);
        _walkTargetScript.PlaceTargetCubeRandomly();
    }
}
