using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GetComponent<Agent>().EndEpisode();
        }
    }
}