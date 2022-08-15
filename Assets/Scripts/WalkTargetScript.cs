using System.Collections;
using Config;
using Unity.MLAgents;
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
    
    private GenericAgent _agent;

    private const string TagToDetect = "Agent";

    private ArenaConfig _arenaSettings;
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    /// <returns></returns>
    public void Start()
    {
        _arenaSettings = FindObjectOfType<ArenaConfig>();
        
        Rng = new Random();
        var parent = transform.parent;
        TerrainGenerator = parent.GetComponentInChildren<TerrainGenerator>();
        ThisRigidbody = transform.GetComponentInChildren<Rigidbody>();
        _agent = parent.GetComponentInChildren<GenericAgent>();
        const int x = 64;
        const int z = 80;
        var y = TerrainGenerator.GetTerrainHeight(new Vector3(x, 0, z));
        transform.localPosition = new Vector3(x, y, z);
        _ = StartCoroutine(nameof(ChangeDirection));
    }

    /// <summary>
    /// Run update at a fixed rate
    /// </summary>
    /// <returns></returns>
    public void FixedUpdate()
    {
        // Move the target randomly
        if (_arenaSettings.TargetMovementSpeed > 0)
        {
            ThisRigidbody.MovePosition(transform.position + (_arenaSettings.TargetMovementSpeed * Time.deltaTime * TargetDirection));
        }
        
        var localPosition = transform.localPosition;
        var x = localPosition.x;
        var z = localPosition.z;
        var terrainHeight = TerrainGenerator.GetTerrainHeight(new Vector3(x, 0, z));
        // Safeguard if target is outside of arena
        if (transform.localPosition.y < terrainHeight -1 || transform.localPosition.y> 40 || transform.localPosition.x is < -1 or > 129 || transform.localPosition.z is < -1 or > 129)
        {
            localPosition = new Vector3(x,terrainHeight + 1f , z);
            transform.localPosition = localPosition;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public void PlaceTargetCubeRandomly(){
        var x = UnityEngine.Random.Range(4 , DynamicEnviormentGenerator.TerrainSize - 4);
        var z = UnityEngine.Random.Range(4, DynamicEnviormentGenerator.TerrainSize  - 4);
        var y = TerrainGenerator.GetTerrainHeight(new Vector3(x, 0, z));
        transform.localPosition = new Vector3(x, y, z);
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
            yield return new WaitForSeconds(UnityEngine.Random.Range(1, _arenaSettings.TargetMaxSecondsInOneDirection));
        }
    }


    /// <summary>
    /// Move to random direction if target collided with walls
    /// Signal if the agent touched the target
    /// </summary>
    /// <returns></returns>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag(TagToDetect))
        {
            _agent.TouchedTarget();
        }
        if (collision.gameObject.name != "Terrain")
        {
            TargetDirection = Quaternion.AngleAxis(UnityEngine.Random.Range(60,170), Vector3.up) * TargetDirection;
        }
    }
}
