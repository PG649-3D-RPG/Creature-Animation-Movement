using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    [SerializeField]
    public float TimeScale = 1f;

    private Agent _agent;

    private void Start()
    {
        _agent = GetComponent<Agent>();
    }

    public void Awake()
    {
        if(Application.isEditor)
        {
            if(Math.Abs(TimeScale - 1.0) > 0.0001) Debug.LogWarning("TimeScale modified!");
            Time.timeScale = TimeScale;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            _agent.EndEpisode();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}