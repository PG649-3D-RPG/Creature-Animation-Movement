using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    [SerializeField]
    public float TimeScale = 1f;

    private Agent[] _agent;

    public void Awake()
    {

        _agent = FindObjectsOfType<Agent>();
        if (_agent == null) Debug.LogWarning("Agent object is null"); 
        if (Application.isEditor)
        {
            if(Math.Abs(TimeScale - 1.0) > 0.0001) Debug.LogWarning("TimeScale modified!");
            Time.timeScale = TimeScale;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Plus))
        {
            Time.timeScale = (float) (Time.timeScale + 0.1);
            Debug.Log($"Timescale raised to {Time.timeScale}");
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            Time.timeScale = (float) (Time.timeScale - 0.1);
            Debug.Log($"Timescale lowered to {Time.timeScale}");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var agent in _agent)
            {
                agent.EndEpisode();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}