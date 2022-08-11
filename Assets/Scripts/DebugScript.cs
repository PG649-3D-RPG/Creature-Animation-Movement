using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    [SerializeField]
    public float TimeScale = 1f;

    public void Awake()
    {
        var Deg = FindObjectOfType<DynamicEnviormentGenerator>();
        if(Deg.DebugMode) Time.timeScale = TimeScale;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GetComponent<Agent>().EndEpisode();
        }
    }
}