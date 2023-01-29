using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using BodyPart = Unity.MLAgentsExamples.BodyPart;

public class Agent4BStandup : GenericAgent
{

    private float _timeElapsed;
    /// Attributes for reward function based on head and top transform
    private GameObject head;
    private float standingHeadHeight;
    private float initialHeadHeight;
    private float episodeMaxHeadHeight;
    private float lastUpdateHeadHeight;
    private float minTopRotationDiff;
    private float minTopHeightDiff;
    private float spawnHeight;

    protected override int CalculateNumberContinuousActions()
    {
        return _jdController.bodyPartsList.Sum(bodyPart => 1 + bodyPart.GetNumberUnlockedAngularMotions());
    }

    public override void Initialize()
    {
        head = this.gameObject.GetComponentsInChildren<Bone>().Where( it => it.category == BoneCategory.Head).First().gameObject;
        standingHeadHeight = head.transform.position.y;

        // Calculate maximum body width for putting creature on its side
        try{
            var back_parts = this.gameObject.GetComponentsInChildren<Bone>().Where( it => it.category == BoneCategory.Hip || it.category == BoneCategory.Torso );
            float maxWidth = 0;
            foreach(var part in back_parts){
                maxWidth = Mathf.Max(maxWidth, part.gameObject.transform.GetChild(0).transform.localScale.x);
            }
            spawnHeight = maxWidth/2 + 0.05f;
            Debug.Log($"Calculated spawn height as {spawnHeight} from maxWidth {maxWidth}");
        }
        catch(Exception){
            spawnHeight = 0.3f;
        }

        base.Initialize();
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

    /// Override to read initial positions of head and top transform for reward function
    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        initialHeadHeight = episodeMaxHeadHeight = lastUpdateHeadHeight = this.head.transform.position.y;

        minTopHeightDiff = Mathf.Abs(_topStartingPosition.y - _topTransform.position.y); // measure initial difference in height of top transform (this is called after overriden SetWalkerOnGround, so the creature is alaready on the floor)
        Vector3 temp = (_topStartingRotation.eulerAngles - _topTransform.rotation.eulerAngles);
        minTopRotationDiff = (new Vector3(temp.x % 360, 0, temp.z % 360)).magnitude;  // measure initial difference in x and z axis rotation of the top transform (y axis is ignored as it essentially only represents the direction in which the creature is looking, which is irrelevant for standing up)
    }

    /// Override to put the creature on its side (laying down on the floor)
    protected override void SetWalkerOnGround(){
        base.SetWalkerOnGround();
        PutCreatureOnSide();
    }

    /// Puts a quadruped creature on its side
    /// x rotation is set randomly between -195 and -165 degrees (shoulders slightly higher than hips or reversed)
    /// y rotation is set randomly
    /// z rotation is set to 90 or -90 degrees (lay on either side)
    public void PutCreatureOnSide(){
        if(_topTransform == null) return;
        _topTransform.position = new Vector3(_topTransform.position.x, spawnHeight, _topTransform.position.z);
        _topTransform.rotation = Quaternion.Euler(new Vector3(UnityEngine.Random.Range(-195, -165), UnityEngine.Random.Range(-180, 180), (Mathf.RoundToInt(UnityEngine.Random.Range(0, 1)) * (-2) + 1) * 90));
    }

    public void FixedUpdate()
    {
        _timeElapsed += Time.deltaTime;

        //Update OrientationCube and DirectionIndicator
        var position = base._topTransform.position;
        _orientationCube.UpdateOrientation(position, _nextPathPoint);
        var cubeForward = _orientationCube.transform.forward;
        var forwardDir = _creatureConfig.creatureType == CreatureType.Biped ? base._topTransform.up : base._topTransform.forward;

        /// Standup rewards
        
        //head position relative to creature height 
        float headHeight = head.transform.position.y;
        float headDiff = Mathf.Pow(Mathf.Clamp(headHeight / standingHeadHeight, 0, 1), 2);

        // Add a reward if the head height is higher than ever before in this episode OR if head height is almost at standing height
        if(headHeight > episodeMaxHeadHeight || Mathf.Abs(standingHeadHeight - headHeight) < 0.25f){
            AddReward(headDiff);
            episodeMaxHeadHeight = headHeight;
        }
        // Add a penalty if head height is less than it was in previous update while head is already almost at standing height (creature should never lower its head again once it is already standing)
        else if(headHeight < lastUpdateHeadHeight && Mathf.Abs(standingHeadHeight - headHeight) > 0.25f){
            AddReward(-0.1f); //penalty if creature lowers head again
        }
        lastUpdateHeadHeight = headHeight;


        float topHeightDiff = Mathf.Abs(_topStartingPosition.y - _topTransform.position.y); // measure difference in height of top transform
        Vector3 temp = (_topStartingRotation.eulerAngles - _topTransform.rotation.eulerAngles);
        float topRotationDiff = (new Vector3(temp.x % 360, 0, temp.z % 360)).magnitude;  // measure difference in x and z axis rotation of the top transform (y axis is ignored as it essentially only represents the direction in which the creature is looking, which is irrelevant for standing up)

        // Add a reward if the height of the top transform improves or is close to its original (standing) position
        if(topHeightDiff < minTopHeightDiff || topHeightDiff < 0.125f){
            minTopHeightDiff = topHeightDiff;
            float r = Mathf.Clamp( Mathf.Pow(0.2f, topHeightDiff), 0, 1 );
            AddReward( r );
        }
        // Add a reward if the rotation of the top transform improves or is close to its original (standing) rotation
        if(topRotationDiff < minTopRotationDiff || topRotationDiff < 0.4f){
            minTopRotationDiff = topRotationDiff;
            float r = Mathf.Clamp( Mathf.Pow(0.2f, topRotationDiff), 0, 1 );
            AddReward( r );
        }

        SwitchModel(DetermineModel);
    }

}