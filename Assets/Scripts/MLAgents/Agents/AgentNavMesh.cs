using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

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

        foreach (var bodyPart in _jdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
            //rotation deltas for the head
            if (bodyPart.rb.transform.GetComponent<Bone>().category == BoneCategory.Head) sensor.AddObservation(Quaternion.FromToRotation(bodyPart.rb.transform.forward, cubeForward));
             sensor.AddObservation(bodyPart.rb.worldCenterOfMass);
             sensor.AddObservation(bodyPart.rb.transform.forward);
             sensor.AddObservation(bodyPart.rb.transform.up);
             sensor.AddObservation(bodyPart.rb.transform.right);

        }
        sensor.AddObservation(_topTransform.position.y);
        
        sensor.AddObservation(CalculateCenterOfMass(_topTransform));
    }

    private float Normalize(float val, float min, float max)
    {
        return Math.Clamp((val - min) / (max - min), 0, 1); 
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

        var cubeForward = _orientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(GetAvgVelocityOfCreature(), cubeForward * MTargetWalkingSpeed), 0, MTargetWalkingSpeed);
        var matchSpeedReward = Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);

        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = (Vector3.Dot(cubeForward, _topTransform.forward) + 1) * 0.5f;

        // TODO works only with flat terrain
        var normHeadPos = Normalize(_headTransform.position.y, 0f, _headPosition.y);
        
        // TODO This is kinda hacky. It is not assured, that the value is between 0 and 1 and is simply clipped if the difference gets to big
        var normCenterOfMass = 1 -Math.Clamp(Vector3.Distance(CalculateCenterOfMass(_topTransform),  _initialCenterOfMass), 0, 1);
        
        //Debug.Log($"Norm variant {Vector3.Distance(Vector3.Normalize(CalculateCenterOfMass(_topTransform)), Vector3.Normalize(_initialCenterOfMass))} Dot {Vector3.Dot(_initialCenterOfMass, CalculateCenterOfMass(_topTransform))}");
        //Debug.Log($"CoM {CalculateCenterOfMass(_topTransform)} Init CoM {_initialCenterOfMass} Distance {Vector3.Distance(CalculateCenterOfMass(_topTransform),_initialCenterOfMass )}");
        //Debug.Log($"Norm {normCenterOfMass} ");
        
        // Getting direction vector
        var orientationCounter = 0;
        var avgOrientationUp = Vector3.zero;
        var avgOrientationForward = Vector3.zero;
        var avgOrientationRight = Vector3.zero;

        foreach (var rb in _topTransform.GetComponentsInChildren<Rigidbody>())
        {
            switch (rb.transform.GetComponent<Bone>().category)
            {
                case BoneCategory.Torso: // Empty on purpose
                case BoneCategory.Head:
                    orientationCounter++;
                    var transform1 = rb.transform;
                    avgOrientationForward += transform1.forward;
                    avgOrientationRight += transform1.right;
                    avgOrientationUp += transform1.up;
                    break;
            }
        }
        avgOrientationForward /= orientationCounter;
        avgOrientationRight /= orientationCounter;
        avgOrientationUp /= orientationCounter;
        //Debug.Log($"_avgForwardOrientation {_avgForwardOrientation}  _avgRightOrientation{_avgRightOrientation} _avgUpOrientation {_avgUpOrientation}");
        //Debug.Log($"Dot {Vector3.Dot(avgOrientationForward, _avgForwardOrientation)} Distance {Vector3.Distance(avgOrientationForward, _avgForwardOrientation)}");
        
        //Forward reward ist kinda hacky aswell
        var torsoReward = Normalize(Math.Clamp(Vector3.Dot(avgOrientationForward, _avgForwardOrientation), 0, 1), 0, 1);
        
        if (float.IsNaN(lookAtTargetReward) ||
            float.IsNaN(matchSpeedReward)||
            float.IsNaN(normHeadPos) ||
            float.IsNaN(normCenterOfMass)
            ) 
        {
            Debug.LogError(
                $"lookAtTargetReward {float.IsNaN(lookAtTargetReward)} or matchSpeedReward {float.IsNaN(matchSpeedReward)}");
        }
        else
        {
            
            var headToLow = _headPosition.y - _headTransform.position.y;
            // Idea: 
            // If the head is lower than x percent of the body height, set the reward to null.
            // Else calculate the reward from matching the torso forward vector, the normalized head pos,
            // distance from the original center of mass and speed + look at target reward 
            var reward = torsoReward * normHeadPos * normCenterOfMass * matchSpeedReward * lookAtTargetReward;
            Debug.Log($"Reward {reward} torso {torsoReward} normHeadPos {normHeadPos} normCenterOfMass {normCenterOfMass} matchSpeedReward {matchSpeedReward} lookAtTargetReward {lookAtTargetReward}");
            //Debug.Log($"Reward {reward}");
            AddReward(reward);
        }
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
        return _path.corners[_pathCornerIndex] + new Vector3(0, 2 * _topStartingPosition.y, 0);
    }
}
