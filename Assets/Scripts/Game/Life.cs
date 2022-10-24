using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Life : MonoBehaviour
{
    [SerializeField]
    private int BpDmgModifier = 5;
    private CreatureController _creatureController;
    void Start()
    {
        _creatureController = transform.GetComponentInParent<CreatureController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        _creatureController.TakeDamage(BpDmgModifier);
    }
}
