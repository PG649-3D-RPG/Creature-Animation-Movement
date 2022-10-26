using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private int Health = 1000;

    private int HealthInLastUpdate;

    private int DmgTakenSinceLastUpdate;

    private bool Alive;

    private void Start()
    {
        HealthInLastUpdate = Health;
        DmgTakenSinceLastUpdate = 0;
        Alive = true;
    }

    private void FixedUpdate()
    {
        DmgTakenSinceLastUpdate = HealthInLastUpdate - Health;
        Health = HealthInLastUpdate;
    }

    public void TakeDmg(int dmg)
    {
        Health -= dmg;
        if (Health <= 0) Alive = false;
    }
}
