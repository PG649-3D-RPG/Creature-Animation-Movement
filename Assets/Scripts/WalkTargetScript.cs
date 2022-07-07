using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class WalkTargetScript : MonoBehaviour
{
    private GameObject ArenaTerrain { get; set; }

    private Random Rng { get; set; }

    private Vector3 TargetDirection { get; set; }

    private Rigidbody ThisRigidbody { get; set; }

    private TerrainGenerator TerrainGenerator { get; set; }

    private DynamicEnviormentGenerator Deg { get; set; }


    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    /// <returns></returns>
    void Start()
    {
        Deg = GameObject.FindObjectOfType<DynamicEnviormentGenerator>();

        Rng = new Random();
        TerrainGenerator = transform.parent.GetComponent<TerrainGenerator>();
        ThisRigidbody = transform.GetComponentInChildren<Rigidbody>();
        PlaceTargetCubeRandomly();
        _ = StartCoroutine(nameof(ChangeDirection));
    }

    /// <summary>
    /// Run update at a fixed rate
    /// </summary>
    /// <returns></returns>
    public void FixedUpdate()
    {
        // Move the target randomly
        if (Deg.IsMovingTarget)
        {
            MoveTargetRandomlyPerTick();
        }
        // Safeguard if target is outside of arena
        if (transform.localPosition.y is < -5f or > 40 || transform.localPosition.x is < -1 or > 129 || transform.localPosition.z is < -1 or > 129) 
        {
            PlaceTargetCubeRandomly();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public void PlaceTargetCubeRandomly(){
        var x = UnityEngine.Random.Range(4 , Deg .TerrainSize - 4);
        var z = UnityEngine.Random.Range(4, Deg.TerrainSize  - 4);
        var y = TerrainGenerator.GetTerrainHeight(x, z) + 1f;
        transform.localPosition = new Vector3(x, y, z);
    }

    /// <summary>
    /// Move the rigidbody of the target to a random position
    /// </summary>
    /// <returns></returns>
    public void MoveTargetRandomlyPerTick()
    {
        ThisRigidbody.MovePosition(transform.position + (Deg.MovementSpeed * Time.deltaTime * TargetDirection));
    }

    /// <summary> 
    /// Change direction randomly every x seconds
    /// </summary>
    /// <returns></returns>
    private IEnumerator ChangeDirection()
    {
        while (true)
        {
            TargetDirection = Vector3.Normalize(new Vector3((float)(Rng.NextDouble() * 2) - 1, 0, (float)(Rng.NextDouble() * 2) - 1));
            yield return new WaitForSeconds(UnityEngine.Random.Range(1, Deg.TargetMaxSecondsInOneDirection));
        }
    }


    /// <summary>
    /// Move to random direction if target collided with walls or
    /// </summary>
    /// <returns></returns>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name != "Terrain")
        {
            TargetDirection = Quaternion.AngleAxis(UnityEngine.Random.Range(60,170), Vector3.up) * TargetDirection;
        }
    }
}
