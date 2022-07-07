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
    private Transform otherTransform;
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
        get => deg.targetWalkingSpeed;
        set => deg.targetWalkingSpeed = Mathf.Clamp(value, .1f, deg.maxWalkingSpeed);
    }

    public void Awake()
    {
        deg = GameObject.FindObjectOfType<DynamicEnviormentGenerator>();
        jdController = this.AddComponent<JointDriveController>();
        decisionRequester = this.AddComponent<DecisionRequester>(); 
    }

    public override void Initialize()
    {
        terrainGenerator = transform.parent.GetComponentInChildren<TerrainGenerator>();
        walkTargetScript = transform.parent.GetComponentInChildren<WalkTargetScript>();
        // TODO: Update
        target = transform.parent.Find("Creature Target").transform;
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
                if (deg.notAllowedToTouchGround.Contains(boneScript.category))
                {
                    groundContact.agentDoneOnGroundContact = true;
                }

                if (deg.penalizeGroundContact && deg.penaltiesForBodyParts.ContainsKey(boneScript.category))
                {
                    groundContact.penalizeGroundContact = true;
                    groundContact.groundContactPenalty = deg.penaltiesForBodyParts.GetValueOrDefault(boneScript.category);
                }
                var bodyPartHeight = trans.position.y - transform.position.y;
                jdController.SetupBodyPart(trans, bodyPartHeight);
            }
            else if (boneScript != null && boneScript.isRoot)
            {
                otherTransform = trans;
                _otherStartingRotation = trans.rotation;
            }

        }

        otherBodyPartHeight = otherTransform.position.y - transform.position.y;

        //SetWalkerOnGround();
    }


    /// <summary>
    /// Is called on episode beginn.
    /// Loop over body parts and reset them to initial conditions.
    /// Regenerate terrain and place target cube randomly 
    /// </summary>
    public override void OnEpisodeBegin()
    {
        episodeCounter++;

        if (deg.regenerateTerrain && episodeCounter % deg.regenerateTerrainAfterXSteps == 0)
        {
            terrainGenerator.RegenerateTerrain();
        }

        if (deg.placeTargetCubeRandomly && episodeCounter % deg.placeTargetCubeRandomlyAfterXSteps == 0)
        {
            walkTargetScript.PlaceTargetCubeRandomly();
        }

        SetWalkerOnGround();

        //Random start rotation to help generalize
        //otherTransform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            deg.randomizeWalkSpeedEachEpisode ? Random.Range(0.1f, deg.maxWalkingSpeed) : MTargetWalkingSpeed;
    }


    /// <summary>
    /// Set the walker on the terrain.
    /// </summary>
    public void SetWalkerOnGround()
    {
        var terrainGenerator = transform.parent.GetComponentInChildren<TerrainGenerator>();
        var terrainHeight = terrainGenerator.GetTerrainHeight(otherTransform.position);

        Rigidbody otherRigidbody = otherTransform.GetComponent<Rigidbody>();
        otherTransform.position = new Vector3(otherTransform.position.x, terrainHeight + otherBodyPartHeight + deg.yheightOffset, otherTransform.position.z);
        otherTransform.rotation = _otherStartingRotation;

        otherRigidbody.velocity = Vector3.zero;
        otherRigidbody.angularVelocity = Vector3.zero;

        //Reset all of the body parts
        foreach (var bodyPart in jdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart, terrainHeight, deg.yheightOffset);
        }
    }


    /// <summary>
    /// Safeguard if the walker leaves the playable area
    /// </summary>
    private void CheckWalkerOutOfBound()
    {
        if (otherTransform.position.y is < -10 or > 40)
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
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.position - otherTransform.position));

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
        sensor.AddObservation(Quaternion.FromToRotation(otherTransform.forward, cubeForward));

        //Position of target position relative to cube
        sensor.AddObservation(orientationCube.transform.InverseTransformPoint(target.transform.position));

        foreach (var bodyPart in jdController.bodyPartsList)
        {
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
            float x_target = parts.joint.angularXMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            float y_target = parts.joint.angularYMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            float z_target = parts.joint.angularZMotion == ConfigurableJointMotion.Locked ? 0 : continuousActions[++i];
            parts.SetJointTargetRotation(x_target, y_target, z_target);
            parts.SetJointStrength(continuousActions[++i]);
        }
    }

    //Update OrientationCube and DirectionIndicator
    void UpdateOrientationObjects()
    {
        dirToWalk = target.position - otherTransform.position;
        orientationCube.UpdateOrientation(otherTransform, target);
    }

    void FixedUpdate()
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
    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in jdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    //normalized value of the difference in avg speed vs goal walking speed.
    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
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
