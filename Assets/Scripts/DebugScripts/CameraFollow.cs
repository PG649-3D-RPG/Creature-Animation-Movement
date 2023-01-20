using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform targetObject;
    private Vector3? initalOffset = null;
    private Vector3 cameraPosition;

    void Start()
    {
        
        //initalOffset = transform.position - targetObject.position;
    }

    void FixedUpdate()
    {   
        if(targetObject == null) return;
        if(!this.initalOffset.HasValue){
            initalOffset = new Vector3(0, 5, 0);
        }
        cameraPosition = targetObject.position + initalOffset.GetValueOrDefault();
        transform.position = cameraPosition;
    }
}