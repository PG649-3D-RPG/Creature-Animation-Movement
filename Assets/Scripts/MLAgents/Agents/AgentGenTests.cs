using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
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

public class AgentGenTests : GenericAgent
{
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

        var skeletonScript = _topTransform.GetComponent<Skeleton>();
        var selfObservations = skeletonScript.SettingsInstance.Observations();

        //Debug.Log(string.Join(", ", selfObservations));

        foreach (var selfOb in selfObservations)
        {
            sensor.AddObservation(selfOb);
        }
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

        var cubeForward2d = new Vector2(cubeForward.x, cubeForward.z);
        var topTranformForward2d = new Vector2(_topTransform.forward.x, _topTransform.forward.z);
        var lookAtTargetReward = (Vector2.Dot(cubeForward2d, -topTranformForward2d) + 1) * 0.5f;

        //Debug.Log(lookAtTargetReward);
        //Debug.Log("Cube: " + cubeForward2d + " | Creature: " + topTranformForward2d);

        if (float.IsNaN(lookAtTargetReward) || float.IsNaN(matchSpeedReward)) throw new ArgumentException($"A reward is NaN. float.");
        //Debug.Log($"matchSpeedReward {Math.Max(matchSpeedReward, 0.1f)} lookAtTargetReward {Math.Max(lookAtTargetReward, 0.1f)}");
        AddReward(Math.Max(matchSpeedReward, 0.1f) * lookAtTargetReward);

        //Debug.Log(GetCumulativeReward());
    }

    private float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, MTargetWalkingSpeed);
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);
    }

}
