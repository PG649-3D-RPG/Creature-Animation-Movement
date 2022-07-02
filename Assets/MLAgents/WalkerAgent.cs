using System;
using System.Collections.Generic;
using System.Linq;
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
    private float _otherBodyPartHeight = 1f;

    private TerrainGenerator _terrainGenerator;

    private WalkTargetScript _walkTargetScript;

    [Header("Walk Speed")]
    [Range(0.1f, 10)]
    [SerializeField]
    //The walking speed to try and achieve
    private float m_TargetWalkingSpeed = 10;

    public float MTargetWalkingSpeed // property
    {
        get { return m_TargetWalkingSpeed; }
        set { m_TargetWalkingSpeed = Mathf.Clamp(value, .1f, m_maxWalkingSpeed); }
    }

    const float m_maxWalkingSpeed = 10; //The max walking speed

    //Should the agent sample a new goal velocity each episode?
    //If true, walkSpeed will be randomly set between zero and m_maxWalkingSpeed in OnEpisodeBegin()
    //If false, the goal velocity will be walkingSpeed
    public bool randomizeWalkSpeedEachEpisode;

    //The direction an agent will walk during training.
    private Vector3 m_WorldDirToWalk = Vector3.right;

    [Header("Target To Walk Towards")] public Transform target; //Target the agent will walk towards during training.

    [Header("Body Parts")] public List<Transform> bodyParts = new();

    private Quaternion _otherStartingRotation;
    public Transform otherTransform;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    OrientationCubeController m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    public JointDriveController m_JdController;

    
    public float yheightOffset = 0.05f;

    [Header("Enviorment Settings")]
    [SerializeField]
    public bool regenerateTerrain = true;

    [SerializeField]
    public int regenerateTerrainAfterXSteps = 1;

    [SerializeField]
    public bool placeTargetCubeRandomly = true;

    [SerializeField]
    public int placeTargetCubeRandomlyAfterXSteps = 1;

    [SerializeField]
    public bool fastResetForTheFirstEpisodes = true;

    [SerializeField]
    public int fastResetLength = 10000000;

    [SerializeField]
    public List<BoneCategory> notAllowedToTouchGround = new() { BoneCategory.Head };

    [SerializeField]
    public List<BoneCategory> notAllowedToTouchGroundInFastPhase = new() { BoneCategory.Arm, BoneCategory.Hand, BoneCategory.Torso };

    private long episodeCounter = 0;


    //public void Awake()
    //{
    //    var bpScript = GetComponent<BehaviorParameters>();
    //    bpScript.BrainParameters.VectorObservationSize = 123;
    //}

    public override void Initialize()
    {
        _terrainGenerator = transform.parent.GetComponentInChildren<TerrainGenerator>();
        _walkTargetScript = transform.parent.GetComponentInChildren<WalkTargetScript>();

        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
        m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();

        if (fastResetForTheFirstEpisodes) notAllowedToTouchGround.AddRange(notAllowedToTouchGroundInFastPhase);


        //Get Body Parts
        //and setup each body part

        var transforms = GetComponentsInChildren<Transform>();
        foreach (var trans in transforms)
        {
            // Double check if categories change!
            var boneScript = trans.GetComponent<Bone>();
            if (boneScript != null && boneScript.category is not BoneCategory.Other)
            {
                bodyParts.Add(trans);
                var groundContact = trans.AddComponent<GroundContact>();
                // TODO Config Ground Contact
                if (notAllowedToTouchGround.Contains(boneScript.category))
                {
                    groundContact.agentDoneOnGroundContact = true;
                }
                float bodyPartHeight = trans.position.y - transform.position.y;
                m_JdController.SetupBodyPart(trans, bodyPartHeight);
            }
            else if (boneScript != null && boneScript.category is BoneCategory.Other)
            {
                otherTransform = trans;
                _otherStartingRotation = trans.rotation;
            }

        }

        _otherBodyPartHeight = otherTransform.position.y - transform.position.y;


        m_JdController = GetComponent<JointDriveController>();

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

        if (regenerateTerrain && episodeCounter % regenerateTerrainAfterXSteps == 0)
        {
            _terrainGenerator.RegenerateTerrain();
        }

        if (placeTargetCubeRandomly && episodeCounter % placeTargetCubeRandomlyAfterXSteps == 0)
        {
            _walkTargetScript.PlaceTargetCubeRandomly();
        }

        if (fastResetForTheFirstEpisodes && episodeCounter == fastResetLength)
        {
            notAllowedToTouchGround.RemoveAll(x => notAllowedToTouchGroundInFastPhase.Contains(x));

            foreach (var bp in bodyParts)
            {
                bp.GetComponent<GroundContact>().agentDoneOnGroundContact = false;
            }
        }

        SetWalkerOnGround();

        //Random start rotation to help generalize
        //otherTransform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            randomizeWalkSpeedEachEpisode ? Random.Range(0.1f, m_maxWalkingSpeed) : MTargetWalkingSpeed;
    }


    /// <summary>
    /// Set the walker on the terrain.
    /// </summary>
    public void SetWalkerOnGround()
    {
        var terrainGenerator = transform.parent.GetComponentInChildren<TerrainGenerator>();
        var terrainHeight = terrainGenerator.GetTerrainHeight(otherTransform.position);

        Rigidbody otherRigidbody = otherTransform.GetComponent<Rigidbody>();
        otherTransform.position = new Vector3(otherTransform.position.x, terrainHeight + _otherBodyPartHeight + yheightOffset, otherTransform.position.z);
        otherTransform.rotation = _otherStartingRotation;

        otherRigidbody.velocity = Vector3.zero;
        otherRigidbody.angularVelocity = Vector3.zero;

        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart, terrainHeight, yheightOffset);
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
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        // TODO Why do we do this?
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.position - otherTransform.position));

        if (bp.rb.transform.GetComponent<Bone>().category != BoneCategory.Hand)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = m_OrientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * MTargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(avgVel));
        //vel goal relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(velGoal));

        //rotation deltas
        sensor.AddObservation(Quaternion.FromToRotation(otherTransform.forward, cubeForward));

        //Position of target position relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformPoint(target.transform.position));

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
            //rotation deltas for the head
            if (bodyPart.rb.transform.GetComponent<Bone>().category == BoneCategory.Head) sensor.AddObservation(Quaternion.FromToRotation(bodyPart.rb.transform.forward, cubeForward));
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(1f);
        var bpList = m_JdController.bodyPartsList;
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
        m_WorldDirToWalk = target.position - otherTransform.position;
        m_OrientationCube.UpdateOrientation(otherTransform, target);
        if (m_DirectionIndicator)
        {
            m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
        }
    }

    void FixedUpdate()
    {
        CheckWalkerOutOfBound();
        UpdateOrientationObjects();

        var cubeForward = m_OrientationCube.transform.forward;

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
        foreach (var item in m_JdController.bodyPartsList)
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
