using System;
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

public class WalkerAgent : Agent
{
    // Generator
    private DynamicEnviormentGenerator deg;

    // Internal values
    private float otherBodyPartHeight = 1f;
    private Quaternion _otherStartingRotation;
    private Transform topTransform;
    private Rigidbody topTransformRb;

    private long episodeCounter = 0;
    private List<Transform> bodyParts = new();
    private Vector3 dirToWalk = Vector3.right;

    // Scripts
    private TerrainGenerator terrainGenerator;
    private WalkTargetScript walkTargetScript;
    private Transform target;
    private OrientationCubeController orientationCube;
    private JointDriveController jdController;
    private DecisionRequester decisionRequester;

    public float MTargetWalkingSpeed // property
    {
        get => deg.TargetWalkingSpeed;
        set => deg.TargetWalkingSpeed = Mathf.Clamp(value, .1f, deg.MaxWalkingSpeed);
    }

    public void Awake()
    {
        deg = GameObject.FindObjectOfType<DynamicEnviormentGenerator>();
        jdController = this.AddComponent<JointDriveController>();
        decisionRequester = this.AddComponent<DecisionRequester>();
        jdController.maxJointForceLimit = deg.MaxJointForceLimit;
        jdController.jointDampen = deg.JointDampen;
        jdController.maxJointSpring = deg.MaxJointSpring;
        
        // Set agent settings (maxSteps)
        var mAgent = gameObject.GetComponent<Agent>();
        mAgent.MaxStep = deg.MaxStep;
        
        // Set behavior parameters
        var skeleton = GetComponentInChildren<Skeleton>();
        var bpScript = GetComponent<BehaviorParameters>();
        bpScript.BrainParameters.ActionSpec = deg.UseContinuousActionSpaceOffsetAsContinuousActionSpace 
            ? new ActionSpec(deg.ContinuousActionSpaceOffset, new int[deg.DiscreteBranches]) 
            : new ActionSpec(3 * skeleton.nBones + deg.ContinuousActionSpaceOffset, new int[deg.DiscreteBranches]);
        bpScript.BrainParameters.VectorObservationSize = deg.UseObservationSpaceOffsetAsObservationSpace
            ? deg.ObservationSpaceOffset
            : 3 * skeleton.nBones + deg.ObservationSpaceOffset;
    }


    public override void Initialize()
    {
        var parent = transform.parent;
        terrainGenerator = parent.GetComponentInChildren<TerrainGenerator>();
        walkTargetScript = parent.GetComponentInChildren<WalkTargetScript>();
        // TODO: Update
        target = parent.Find("Creature Target").transform;
        orientationCube = transform.Find("orientation cube").AddComponent<OrientationCubeController>();

        //Get Body Parts
        //and setup each body part

        var transforms = GetComponentsInChildren<Transform>();
        foreach (var trans in transforms)
        {
            // Double check if categories change!
            var boneScript = trans.GetComponent<Bone>();
            if (boneScript != null && !boneScript.isRoot)
            {
                bodyParts.Add(trans);
                var groundContact = trans.AddComponent<GroundContact>();
                // TODO Config Ground Contact
                if (deg.NotAllowedToTouchGround.Contains(boneScript.category))
                {
                    groundContact.agentDoneOnGroundContact = true;
                }

                if (deg.PenalizeGroundContact && deg.PenaltiesForBodyParts.ContainsKey(boneScript.category))
                {
                    groundContact.penalizeGroundContact = true;
                    groundContact.groundContactPenalty = deg.PenaltiesForBodyParts.GetValueOrDefault(boneScript.category);
                }
                var bodyPartHeight = trans.position.y - transform.position.y;
                jdController.SetupBodyPart(trans, bodyPartHeight);
            }
            else if (boneScript != null && boneScript.isRoot)
            {
                topTransform = trans;
                topTransformRb = trans.GetComponent<Rigidbody>();

                _otherStartingRotation = trans.rotation;
            }

        }

        otherBodyPartHeight = topTransform.position.y - transform.position.y;

        SetWalkerOnGround();
    }


    /// <summary>
    /// Is called on episode beginn.
    /// Loop over body parts and reset them to initial conditions.
    /// Regenerate terrain and place target cube randomly 
    /// </summary>
    public override void OnEpisodeBegin()
    {
        episodeCounter++;

        if (deg.RegenerateTerrain && episodeCounter % deg.RegenerateTerrainAfterXSteps == 0)
        {
            terrainGenerator.RegenerateTerrain();
        }

        if (deg.PlaceTargetCubeRandomly && episodeCounter % deg.PlaceTargetCubeRandomlyAfterXSteps == 0)
        {
            walkTargetScript.PlaceTargetCubeRandomly();
        }

        SetWalkerOnGround();

        //Random start rotation to help generalize
        //otherTransform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            deg.RandomizeWalkSpeedEachEpisode ? Random.Range(0.1f, deg.MaxWalkingSpeed) : MTargetWalkingSpeed;
    }


    /// <summary>
    /// Set the walker on the terrain.
    /// </summary>
    private void SetWalkerOnGround()
    {
        var position = topTransform.position;
        var terrainHeight = terrainGenerator.GetTerrainHeight(position);

        position = new Vector3(position.x, terrainHeight + otherBodyPartHeight + deg.YHeightOffset, position.z);
        topTransform.position = position;
        topTransform.rotation = _otherStartingRotation;

        topTransformRb.velocity = Vector3.zero;
        topTransformRb.angularVelocity = Vector3.zero;

        //Reset all of the body parts
        foreach (var bodyPart in jdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart, terrainHeight, deg.YHeightOffset);
        }
    }


    /// <summary>
    /// Safeguard if the walker leaves the playable area
    /// </summary>
    private void CheckWalkerOutOfBound()
    {
        if (topTransform.position.y is < -10 or > 40)
        {
            SetWalkerOnGround();
        }
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.touchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        // TODO Why do we do this?
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.position - topTransform.position));

        if (bp.rb.transform.GetComponent<Bone>().category != BoneCategory.Hand)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / jdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = orientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * MTargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(avgVel));
        //vel goal relative to cube
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(velGoal));

        //rotation deltas
        sensor.AddObservation(Quaternion.FromToRotation(topTransform.forward, cubeForward));

        //Position of target position relative to cube
        sensor.AddObservation(orientationCube.transform.InverseTransformPoint(target.transform.position));

        foreach (var bodyPart in jdController.bodyPartsList)
        {
            // TODO Hier fliegt der ArgumentException Fehler. Irgendwas ist wahrscheinlich mit unser Initialisierung falsch?
            CollectObservationBodyPart(bodyPart, sensor);
            //rotation deltas for the head
            if (bodyPart.rb.transform.GetComponent<Bone>().category == BoneCategory.Head) sensor.AddObservation(Quaternion.FromToRotation(bodyPart.rb.transform.forward, cubeForward));
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(1f);
        var bpList = jdController.bodyPartsList;
        var i = -1;

        var continuousActions = actionBuffers.ContinuousActions;
        foreach (var parts in bpList)
        {
            var xTarget = parts.joint.angularXMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            var yTarget = parts.joint.angularYMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            var zTarget = parts.joint.angularZMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            parts.SetJointTargetRotation(xTarget, yTarget, zTarget);
            parts.SetJointStrength(continuousActions[++i]);
        }
    }

    //Update OrientationCube and DirectionIndicator
    public void UpdateOrientationObjects()
    {
        dirToWalk = target.position - topTransform.position;
        orientationCube.UpdateOrientation(topTransform, target);
    }

    public void FixedUpdate()
    {
        CheckWalkerOutOfBound();
        UpdateOrientationObjects();

        var cubeForward = orientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * MTargetWalkingSpeed, GetAvgVelocity());

        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = bodyParts.Where(x => x.GetComponent<Bone>().category == BoneCategory.Head).Sum(part => (Vector3.Dot(cubeForward, part.forward) + 1) * .5F);

        if (float.IsNaN(lookAtTargetReward) || float.IsNaN(matchSpeedReward)) throw new ArgumentException($"A reward is NaN. float.");

        AddReward(matchSpeedReward * lookAtTargetReward);
    }

    //Returns the average velocity of all of the body parts
    //Using the velocity of the hips only has shown to result in more erratic movement from the limbs, so...
    //...using the average helps prevent this erratic movement
    private Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        var numOfRb = 0;
        foreach (var item in jdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    //normalized value of the difference in avg speed vs goal walking speed.
    private float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, MTargetWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);
    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(1f);
    }
}
