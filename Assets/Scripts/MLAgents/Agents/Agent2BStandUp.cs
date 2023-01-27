using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using BodyPart = Unity.MLAgentsExamples.BodyPart;

public class Agent2BStandUp : GenericAgent
{
    
    private NavMeshPath _path;
    private int _pathCornerIndex;
    private float _timeElapsed;
    private Vector3 _nextPathPoint;
    private GameObject head;
    private float standingHeadHeight;
    private float initialHeadHeight;
    private float episodeMaxHeadHeight;
    private int[] zRotations = new int[] { 0,180 };
    //private GameObject targetBall;

    protected override int CalculateNumberContinuousActions()
    {
        return _jdController.bodyPartsList.Sum(bodyPart => 1 + bodyPart.GetNumberUnlockedAngularMotions());
    }

    protected override int CalculateNumberDiscreteBranches()
    {
        return 0;
    }

    public override void Initialize()
    {
        head = this.gameObject.GetComponentsInChildren<Bone>().Where( it => it.category == BoneCategory.Head).First().gameObject;
        standingHeadHeight = head.transform.position.y;
        base.Initialize();

        _path = new NavMeshPath();
        _timeElapsed = 1f;
        _nextPathPoint = base._topTransform.position;
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
        sensor.AddObservation(_orientationCube.transform.InverseTransformDirection(bp.rb.position - base._topTransform.position));

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
        sensor.AddObservation(Quaternion.FromToRotation(base._topTransform.forward, cubeForward));

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
        initialHeadHeight = this.head.transform.position.y;
        episodeMaxHeadHeight = initialHeadHeight;
        lastUpdateHeadHeight = initialHeadHeight;

        float topHeightDiff = Mathf.Abs(_topStartingPosition.y - _topTransform.position.y);
        Vector3 temp = (_topStartingRotation.eulerAngles - _topTransform.rotation.eulerAngles);
        float topRotationDiff = (new Vector3(temp.x % 360, 0, temp.z % 360)).magnitude;

        minTopHeightDiff = topHeightDiff;
        minTopRotationDiff = topRotationDiff;
    }

    protected override void SetWalkerOnGround(){
        base.SetWalkerOnGround();
        PutCreatureOnSide();
    }

    public void PutCreatureOnSide(){
        if(_topTransform == null) return;
        _topTransform.position = new Vector3(_topTransform.position.x, 0.3f, _topTransform.position.z);
        _topTransform.rotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(-180, 180), zRotations[UnityEngine.Random.Range(0, 2)]));
    }



    private float lastUpdateHeadHeight;
    private float minTopRotationDiff;
    private float minTopHeightDiff;
    public void FixedUpdate()
    {
        _timeElapsed += Time.deltaTime;
        _nextPathPoint = GetNextPathPoint(_nextPathPoint);

        if (Application.isEditor)
        {
            #if UNITY_EDITOR
            //Debug.Log($"Episode Step {_agent.StepCount}");
            #endif
            var lv = GameObject.FindObjectOfType<PathVisualizer>();
            if (_path != null)
            {
                lv.DrawPath(_path);
                lv.DrawPoint(_nextPathPoint);
            }
        }

        //Update OrientationCube and DirectionIndicator
        var position = base._topTransform.position;
        _orientationCube.UpdateOrientation(position, _nextPathPoint);
        var cubeForward = _orientationCube.transform.forward;
        var forwardDir = _creatureConfig.creatureType == CreatureType.Biped ? base._topTransform.up : base._topTransform.forward;
        
        //head position relative to creature height //TODO: Idee: kein konstanter Reward, sondern nur bei Verbesserung seit Episodenbeginn
        float headHeight = head.transform.position.y;
        float headDiff = Mathf.Pow(Mathf.Clamp(headHeight / standingHeadHeight, 0, 1), 2);

        if(headHeight > episodeMaxHeadHeight || Mathf.Abs(standingHeadHeight - headHeight) < 0.25f){
            AddReward(headDiff);
            episodeMaxHeadHeight = headHeight;
            //Debug.Log($"Head Height is {headHeight}, initial height was {initialHeadHeight}, standing head height ist {standingHeadHeight}, calculated reward is {headDiff}");
        }
        else if(headHeight < lastUpdateHeadHeight && Mathf.Abs(standingHeadHeight - headHeight) > 0.25f){
            AddReward(-0.1f); //penalty if creature lowers head again
        }
        lastUpdateHeadHeight = headHeight;

        

        //Idee: Stabilität messen -> wie stark verändert sich die Position der Creature noch? (wenig + gute Head Height = sehr gut (stabiles stehen)). Basierend auf Center of Mass? -> scheint keine sinnvollen Ergebnisse zu liefern
        // float comDiff = (_initialCenterOfMass - CalculateCenterOfMass(_topTransform, out _)).sqrMagnitude;
        // Debug.Log($"difference of com is {comDiff}");

        //Idee: Ursprüngliche rigidbody positions und rotations messen und auf Basis der Differenz dazu einen Reward verteilen (Versucht den Ausgangszustand wiederherzustellen), Problem: Creature steht nicht perfekt auf dem Boden -> positions sind in y Richtung leicht falsch, mit etwas Glück ist das aber close enough und die Verbesserung des Rewards reicht trotzdem für einen sinnvollen Trainingsfortschritt

        float topHeightDiff = Mathf.Abs(_topStartingPosition.y - _topTransform.position.y);
        Vector3 temp = (_topStartingRotation.eulerAngles - _topTransform.rotation.eulerAngles);
        float topRotationDiff = (new Vector3(temp.x % 360, 0, temp.z % 360)).magnitude;

        if(topHeightDiff < minTopHeightDiff || topHeightDiff < 0.125f){
            minTopHeightDiff = topHeightDiff;
            float r = Mathf.Clamp( Mathf.Pow(0.2f, topHeightDiff), 0, 1 );
            //Debug.Log($"Adding top height reward of {r}");
            AddReward( r );
        }
        if(topRotationDiff < minTopRotationDiff || topRotationDiff < 0.4f){
            minTopRotationDiff = topRotationDiff;
            float r = Mathf.Clamp( Mathf.Pow(0.2f, topRotationDiff), 0, 1 );
            //Debug.Log($"Adding top rotation reward of {r}");
            AddReward( r );
        }


        SwitchModel(DetermineModel);
    }
    
    private Vector3 GetNextPathPoint(Vector3 nextPoint)
    {
        if(_timeElapsed >= 1.0f || _path.status == NavMeshPathStatus.PathInvalid)
        {   
            _timeElapsed = 0;
            var oldPath = _path;
            var pathValid = NavMesh.CalculatePath(base._topTransform.position, _target.position, NavMesh.AllAreas, _path);
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
        if(_pathCornerIndex < _path.corners.Length - 1 && Vector3.Distance(base._topTransform.position, _path.corners[_pathCornerIndex]) < 4f)
        {
            //Debug.Log("Increased path corner index");
            _pathCornerIndex++;
        }
        return _path.corners[_pathCornerIndex] + new Vector3(0, 2 * _topStartingPosition.y, 0);
    }

    private int DetermineModel()
    {
        return 0;
    }
}
