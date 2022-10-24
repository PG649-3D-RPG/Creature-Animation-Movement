using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreatureController : MonoBehaviour
{
    [SerializeField]
    private bool Alive;

    [SerializeField] private int TotalHealth = 1000;
    private bool DestroyCreatureOnDeath = false;
    // Start is called before the first frame update
    void Start()
    {
        Alive = true;
    }

    public void TakeDamage(int dmg)
    {
        TotalHealth -= dmg;
        if(TotalHealth <= 0 && DestroyCreatureOnDeath) Die();
    }
    
    private void Die()
    {
        Alive = false;
        foreach (var joint in transform.GetComponentsInChildren<ConfigurableJoint>())
        {
            if (!joint.IsDestroyed())
            {
                joint.breakForce = 10;
                joint.breakTorque = 10;
            }
        }
        CleanUpCreature();
    }

    private void CleanUpCreature()
    {
        Debug.LogError("Not Implemented");
    }
}
