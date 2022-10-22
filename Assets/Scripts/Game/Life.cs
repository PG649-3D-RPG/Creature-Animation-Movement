using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Life : MonoBehaviour
{
    [SerializeField]
    private int BodyPartHealth = 6;
    private CreatureController _creatureController;
    void Start()
    {
        _creatureController = transform.GetComponentInParent<CreatureController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TakeDamage();
    }

    private void TakeDamage()
    {
        if(BodyPartHealth > 0) BodyPartHealth--;
    }
}
