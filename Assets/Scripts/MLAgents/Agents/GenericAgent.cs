using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public abstract class GenericAgent : Agent
{
    public abstract void TouchedTarget();
}
