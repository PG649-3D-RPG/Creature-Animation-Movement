using UnityEngine;
using UnityEngine.AI;

public class PathVisualizer : MonoBehaviour
{
    public LineRenderer line; //to hold the line Renderer
    private GameObject point;

    public void Awake()
    {
        point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(point.GetComponent<Collider>());
        point.transform.name = "NextNavMeshPoint";

    }

    public void DrawPath(NavMeshPath path)
    {
        line.alignment = LineAlignment.TransformZ;
        line.useWorldSpace = true;
        line.positionCount = path.corners.Length; //set the array of positions to the amount of corners
        line.SetPositions(path.corners);

    }

    public void DrawPoint(Vector3 nextPoint)
    {
        point.transform.position = nextPoint;
    }
}