using System.Collections;
using System.Collections.Generic;
using Config;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;

public abstract class GenericAgent : Agent
{
    
    // Internal values
    protected float _otherBodyPartHeight = 1f;
    protected Vector3 _topStartingPosition;
    protected Quaternion _topStartingRotation;
    protected Transform _topTransform;
    protected Rigidbody _topTransformRb;

    protected long _episodeCounter = 0;
    protected Vector3 _dirToWalk = Vector3.right;


    // Scripts
    protected DynamicEnviormentGenerator _deg;
    protected TerrainGenerator _terrainGenerator;
    protected WalkTargetScript _walkTargetScript;
    protected Transform _target;
    protected OrientationCubeController _orientationCube;
    protected JointDriveController _jdController;
    protected DecisionRequester _decisionRequester;
    protected Agent _agent;
    protected MlAgentConfig _mlAgentsConfig;
    protected ArenaConfig _arenaSettings;
    
    public abstract void TouchedTarget();
    
    public void Awake()
    {
        
        _deg = FindObjectOfType<DynamicEnviormentGenerator>();
        _jdController = this.AddComponent<JointDriveController>();
        _decisionRequester = this.AddComponent<DecisionRequester>();
        _mlAgentsConfig = FindObjectOfType<MlAgentConfig>();
        _arenaSettings = FindObjectOfType<ArenaConfig>();
        
        // Set agent settings (maxSteps)
        var mAgent = gameObject.GetComponent<Agent>();
        mAgent.MaxStep = _mlAgentsConfig.MaxStep;

        // Set behavior parameters
        var skeleton = GetComponentInChildren<Skeleton>();
        var bpScript = GetComponent<BehaviorParameters>();
        bpScript.BrainParameters.ActionSpec = new ActionSpec(_mlAgentsConfig.ContinuousActionSpaceOffset, new int[_mlAgentsConfig.DiscreteBranches]);
        bpScript.BrainParameters.VectorObservationSize = _mlAgentsConfig.ObservationSpaceOffset;
        bpScript.BehaviorName = DynamicEnviormentGenerator.BehaviorName;
        bpScript.Model = _deg.NnModel;
    }

    public float MTargetWalkingSpeed // property
    {
        get => _arenaSettings.TargetWalkingSpeed;
        set => _arenaSettings.TargetWalkingSpeed = Mathf.Clamp(value, .1f, _arenaSettings.MaxWalkingSpeed);
    }

    
    void Start()
    {
        _ = StartCoroutine(nameof(CheckWalkerOutOfArea));
    }


    private IEnumerator CheckWalkerOutOfArea()
    {
        while (true)
        {
            if (_topTransform.position.y is < -10 or > 40)
            {
                _agent.EndEpisode();
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
