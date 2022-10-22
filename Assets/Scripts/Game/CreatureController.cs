using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureController : MonoBehaviour
{
    [SerializeField]
    private bool Alive;
    
    // Start is called before the first frame update
    void Start()
    {
        Alive = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Die()
    {
        Alive = false;
        foreach (var rb in transform.GetComponentsInChildren<ConfigurableJoint>())
        {
            rb.breakForce = 0;
            rb.breakTorque = 0;
        }
    }
}
