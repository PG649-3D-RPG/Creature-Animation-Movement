using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshGeneratorScript : MonoBehaviour
{
    public NavMeshSurface _surface; 
    // Start is called before the first frame update
    void Start()
    {
        _surface = GetComponent<NavMeshSurface>();
        _surface.BuildNavMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
