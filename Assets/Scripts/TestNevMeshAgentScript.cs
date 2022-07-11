using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TestNevMeshAgentScript : MonoBehaviour
{
    public Transform target;
    private WalkTargetScript targetScript;
    private Vector3 _oldTargetPosition;
    private NavMeshAgent agent;
    private NavMeshPath _path;
    private float _timeElapsed;
    private int _pathCornerIndex;

    private float _speed = 5f;

    private Rigidbody _rigidbody;
    // Start is called before the first frame update
    void Start()
    {
        _oldTargetPosition = target.position;
        _path = new NavMeshPath();
        _timeElapsed = 0.0f;
        _pathCornerIndex = 0;
        _rigidbody = GetComponent<Rigidbody>();
        targetScript = target.GetComponent<WalkTargetScript>();
        //agent = GetComponent<NavMeshAgent>();
        //agent.SetDestination(target.position);
    }

    private void FixedUpdate() {
            MoveToTarget();
    }

    private void MoveToTarget()
    {
        _timeElapsed += Time.fixedDeltaTime;
        if(_timeElapsed > 1.0f)
        {
            //_oldTargetPosition = target.position;
            //agent.destination = _oldTargetPosition;
            _timeElapsed = 0f;
            if(_oldTargetPosition != target.position)
            {
                bool pathValid = NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, _path);
                if(!pathValid)
                {
                    Debug.LogError("path is invalid");
                }
                _pathCornerIndex = 1;
            }
        }
        if(Vector3.Distance(transform.position, target.position) <= 1.5f)
        {
            _rigidbody.velocity = Vector3.zero;
            targetScript.PlaceTargetCubeRandomly();
             bool pathValid = NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, _path);
            if(!pathValid)
            {
                Debug.LogError("path is invalid");
            }
            _pathCornerIndex = 1;
            Debug.Log("Reached Target");
            return;
        }

        if(_pathCornerIndex < _path.corners.Length)
        {
            if(_pathCornerIndex < _path.corners.Length - 1 && Vector3.Distance(transform.position, _path.corners[_pathCornerIndex]) < 0.25f)
            {
                _pathCornerIndex++;
            }
            _rigidbody.velocity = _speed * (Vector3.Normalize(_path.corners[_pathCornerIndex] - transform.position));
        }
    }
}
