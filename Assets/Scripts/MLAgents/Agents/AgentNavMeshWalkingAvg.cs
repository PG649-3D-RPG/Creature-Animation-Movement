using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using BodyPart = Unity.MLAgentsExamples.BodyPart;

public class AgentNavMeshWalkingAvg : GenericAgent
{

    //private GameObject targetBall;
    private float sumSpeedReward;

    protected override int CalculateNumberContinuousActions()
    {
        return _jdController.bodyPartsList.Sum(bodyPart => 1 + bodyPart.GetNumberUnlockedAngularMotions());
    }

    public override void Initialize()
    {
        base.Initialize();
        _path = new NavMeshPath();
        _nextPathPoint = _topTransform.position;
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
        sensor.AddObservation(_orientationCube.transform.InverseTransformPoint(_nextPathPoint));

        foreach (var bodyPart in _jdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
            //rotation deltas for the head
            if (bodyPart.rb.transform.GetComponent<Bone>().category == BoneCategory.Head) sensor.AddObservation(Quaternion.FromToRotation(bodyPart.rb.transform.forward, cubeForward));
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

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        sumSpeedReward = 0;
    }

    public void FixedUpdate()
    {
        _nextPathPoint = GetNextPathPoint();

        if (Application.isEditor)
        {
            var lv = GameObject.FindObjectOfType<PathVisualizer>();
            if (_path != null)
            {
                lv.DrawPath(_path);
                lv.DrawPoint(_nextPathPoint);
            }
        }

        //Update OrientationCube and DirectionIndicator
        var position = _topTransform.position;
        _orientationCube.UpdateOrientation(position, _nextPathPoint);
        var cubeForward = _orientationCube.transform.forward;
        var forwardDir = _creatureConfig.creatureType == CreatureType.Biped ? _topTransform.up : _topTransform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(GetAvgVelocityOfCreature(), cubeForward * MTargetWalkingSpeed), 0, MTargetWalkingSpeed);
        var matchSpeedReward = Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);

        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = (Vector3.Dot(cubeForward, forwardDir) + 1) * 0.5f;

        if (float.IsNaN(lookAtTargetReward) ||
            float.IsNaN(matchSpeedReward)) 
        {
            Debug.LogError($"Reward contain NaN: lookAtTargetReward {float.IsNaN(lookAtTargetReward)} or matchSpeedReward {float.IsNaN(matchSpeedReward)}");
        }
        else
        {
            sumSpeedReward = +matchSpeedReward;
            var avgSpeedReward = (1 / _agent.StepCount + 1) * sumSpeedReward;
            var reward = avgSpeedReward * lookAtTargetReward;
            if (Application.isEditor) Debug.Log($"Current reward in episode {_agent.StepCount}: {reward} matchSpeedReward {matchSpeedReward} und lookAtTargetReward {lookAtTargetReward}");
            AddReward(reward);
        }

        SwitchModel(DetermineModel);
    }
}
