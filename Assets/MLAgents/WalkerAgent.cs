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
    public Transform otherTransform;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    OrientationCubeController m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    public JointDriveController m_JdController;
    EnvironmentParameters m_ResetParams;

    public List<BoneCategory> notAllowedToTouchGround = new();

    public override void Initialize()
    {
        notAllowedToTouchGround.AddRange(new [] {BoneCategory.Head, BoneCategory.Hand, BoneCategory.Torso, BoneCategory.Arm});
        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
        m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();

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
                if(notAllowedToTouchGround.Contains(boneScript.category))
                {
                    groundContact.agentDoneOnGroundContact = true;
                }
                m_JdController.SetupBodyPart(trans);
            }
            else if(boneScript != null && boneScript.category is BoneCategory.Other)
            {
                otherTransform = trans;
            }
        }


        m_JdController = GetComponent<JointDriveController>();

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        //otherTransform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            randomizeWalkSpeedEachEpisode ? Random.Range(0.1f, m_maxWalkingSpeed) : MTargetWalkingSpeed;

        // TODO Currently empty
        SetResetParameters();
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

        Debug.Log(i);
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
        UpdateOrientationObjects();

        var cubeForward = m_OrientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * MTargetWalkingSpeed, GetAvgVelocity());

        //Check for NaNs
        if (float.IsNaN(matchSpeedReward))
        {
            throw new ArgumentException(
                "NaN in moveTowardsTargetReward.\n" +
                $" cubeForward: {cubeForward}\n" +
                $" hips.velocity: {otherTransform.GetComponent<Rigidbody>().velocity}\n" +
                $" maximumWalkingSpeed: {m_maxWalkingSpeed}"
            );
        }

        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = bodyParts.Where(x => x.GetComponent<Bone>().category == BoneCategory.Head).Sum(part => (Vector3.Dot(cubeForward, part.forward) + 1) * .5F);

        //Check for NaNs
        //if (float.IsNaN(lookAtTargetReward))
        //{
        //    throw new ArgumentException(
        //        "NaN in lookAtTargetReward.\n" +
        //        $" cubeForward: {cubeForward}\n" +
        //        $" head.forward: {head.forward}"
        //    );
        //}

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

    public void SetTorsoMass()
    {
        // TODO Do we need this?
        //m_JdController.bodyPartsDict[chest].rb.mass = m_ResetParams.GetWithDefault("chest_mass", 8);
        //m_JdController.bodyPartsDict[spine].rb.mass = m_ResetParams.GetWithDefault("spine_mass", 8);
        //m_JdController.bodyPartsDict[hips].rb.mass = m_ResetParams.GetWithDefault("hip_mass", 8);
    }

    public void SetResetParameters()
    {
        SetTorsoMass();
    }
}
