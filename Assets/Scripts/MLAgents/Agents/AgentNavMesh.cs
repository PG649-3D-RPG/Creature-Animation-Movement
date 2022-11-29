using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;

public class AgentNavMesh : GenericAgent
{
    
    private NavMeshPath _path;
    private int _pathCornerIndex;
    private float _timeElapsed;
    private Vector3 _nextPathPoint;

    //private GameObject targetBall;

    protected override int CalculateNumberContinuousActions()
    {
        var numberActions = 0;
        foreach(BodyPart bodyPart in _jdController.bodyPartsList)
        {
            numberActions += 1 + bodyPart.GetNumberUnlockedAngularMotions();
        }
        return numberActions;
    }

    protected override int CalculateNumberDiscreteBranches()
    {
        return 0;
    }

    public override void Initialize()
    {
        base.Initialize();
        _path = new NavMeshPath();
        _timeElapsed = 1f;
        _nextPathPoint = _topTransform.position;

        //targetBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //Destroy(targetBall.GetComponent<Collider>());
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

        sensor.AddObservation(_topTransform.position.y);

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

    public void FixedUpdate()
    {
        _timeElapsed += Time.deltaTime;
        _nextPathPoint = GetNextPathPoint(_nextPathPoint);
        //targetBall.transform.position = _nextPathPoint;

        //Update OrientationCube and DirectionIndicator
        _dirToWalk = _nextPathPoint - _topTransform.position;
        
        _orientationCube.UpdateOrientation(_topTransform.position, _nextPathPoint);
 
        //Debug.Log($"top transform up {_topTransform.forward}, standup-Reward: {CalculateStandUpReward()}; COM: {_topTransformRb.worldCenterOfMass.y}");
        var reward = CalculateReward();
        //Debug.Log($"Reward: {reward}");
        AddReward(reward);   
    }
    
    private float CalculateReward()
    {
        if(_topTransform.position.y < (_topStartingPosition.y/2))
        {
            return CalculateStandUpReward();
        }
        else // Walking Reward
        {
            var cubeForward = _orientationCube.transform.forward;

            // Set reward for this step according to mixture of the following elements.
            // a. Match target speed
            //This reward will approach 1 if it matches perfectly and approach zero as it deviates
            var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(GetAvgVelocityOfCreature(), cubeForward * MTargetWalkingSpeed), 0, MTargetWalkingSpeed);
            var matchSpeedReward = Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);

            // b. Rotation alignment with target direction.
            //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
            var lookAtTargetReward = (Vector3.Dot(cubeForward, _topTransform.forward) + 1) * 0.5f;

            var heightReward = Mathf.Clamp(_topTransform.position.y, 0, _topStartingPosition.y) / _topStartingPosition.y;
            //Debug.Log($"HeightReward: {heightReward}; y-pos: {_topTransform.position.y}, starting y-Pos {_topStartingPosition.y}");

            if (float.IsNaN(lookAtTargetReward) ||
                float.IsNaN(matchSpeedReward) ||
                float.IsNaN(heightReward)) //throw new ArgumentException($"A reward is NaN. float.");
            //Debug.Log($"matchSpeedReward {Math.Max(matchSpeedReward, 0.1f)} lookAtTargetReward {Math.Max(lookAtTargetReward, 0.1f)}");
            {
                Debug.LogError($"lookAtTargetReward {float.IsNaN(lookAtTargetReward)} or matchSpeedReward {float.IsNaN(matchSpeedReward)}");
                return 0;
            }

            return heightReward * matchSpeedReward * lookAtTargetReward;
        }
    }

    private float CalculateStandUpReward()
    {
        var headHeightReward = RewardFunction(_headTransform.position.y, 0.8f* _headStartingPosition.y, float.PositiveInfinity, 0.37f, 0.1f); 
        var straightReward = _topTransformRb.worldCenterOfMass.y > 0.5f ? RewardFunction(_topTransform.forward.y, 0.9f, float.PositiveInfinity, 1.9f, 0f) : 1;

        var avgVel = GetAvgVelocityOfCreature();
        var noWalkingReward = 0.5f * (RewardFunction(avgVel.x, -0.3f, 0.3f, 1.2f, 0.1f) + RewardFunction(avgVel.z, -0.3f, 0.3f, 1.2f, 0.1f));

        //Debug.Log($"head-Reward:{headHeightReward}; straightReward: {straightReward}; noWalkingReward: {noWalkingReward}");
        return headHeightReward * straightReward * noWalkingReward;
    }


    private Vector3 GetNextPathPoint(Vector3 nextPoint)
    {
        if(_timeElapsed >= 1.0f || _path.status == NavMeshPathStatus.PathInvalid)
        {   
            _timeElapsed = 0;
            var oldPath = _path;
            bool pathValid = NavMesh.CalculatePath(_topTransform.position, _target.position, NavMesh.AllAreas, _path);
            if (!pathValid)
            {
                _path = oldPath;
                //Debug.Log($"Path invalid for {gameObject.name}");
                return nextPoint;
            }
            else
            {
                _pathCornerIndex = 1;
            }
        }
        if(_pathCornerIndex < _path.corners.Length - 1 && Vector3.Distance(_topTransform.position, _path.corners[_pathCornerIndex]) < 4f)
        {
            //Debug.Log("Increased path corner index");
            _pathCornerIndex++;
        }
        return _path.corners[_pathCornerIndex] + new Vector3(0,  _topStartingPosition.y, 0);
    }

    private float RewardFunction(float value, float lowerBound, float higherBound, float margin, float reachingValue)
    {
        if(lowerBound <= value && value <= higherBound)
        {
            return 1f;
        }
        else if ((lowerBound - margin) < value && value < lowerBound)
        {
            //Parameter der linearen Funktion bestimmen
            var m = (1 - reachingValue)/margin;
            var b = 1 - (m * lowerBound);

            return m * value + b;
        }
        else if(higherBound < value && value < (higherBound + margin))
        {
            //Parameter der linearen Funktion bestimmen
            var m = (reachingValue-1)/margin;
            var b = 1 - (m * higherBound);

            return m * value + b;
        }
        else
        {
            return reachingValue;
        }
    }
}
