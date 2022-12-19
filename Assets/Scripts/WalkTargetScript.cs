using System.Collections;
using System.Collections.Generic;
using Config;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

public class WalkTargetScript : MonoBehaviour
{
    private GameObject ArenaTerrain { get; set; }

    private Random Rng { get; set; }

    private Vector3 TargetDirection { get; set; }

    private Rigidbody ThisRigidbody { get; set; }

    public NavMeshAgent navAgent;
    public float timer = -1;
    public float wanderTimer = 30;
    public float wanderRadius = 1500;

    private const string TagToDetect = "Agent";

    //private ArenaConfig _arenaSettings;
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    /// <returns></returns>
    public void Start()
    {
        //_arenaSettings = FindObjectOfType<ArenaConfig>();
        
        Rng = new Random();
        var parent = transform.parent;
        var terrainGenerator = GameObject.Find("Generator")?.GetComponent<AdvancedEnvironmentGenerator>();
        ThisRigidbody = transform.GetComponentInChildren<Rigidbody>();

        if(terrainGenerator != null)
        {
            navAgent = GetComponent<NavMeshAgent>();
            navAgent.speed = terrainGenerator.TargetWanderTimer;
            wanderTimer = terrainGenerator.TargetWanderTimer;
            wanderRadius = terrainGenerator.TargetWanderRadius;
            navAgent.angularSpeed = 0;
            timer = wanderTimer;
        }


        // const int x = 64;
        // const int z = 80;
        //var y = TerrainGenerator.GetTerrainHeight(new Vector3(x, 0, z)) + transform.localScale.y/2;
        //transform.localPosition = new Vector3(x, y, z);

        //_ = StartCoroutine(nameof(ChangeDirection));
    }

    /// <summary>
    /// Run update at a fixed rate
    /// </summary>
    /// <returns></returns>
    public void FixedUpdate()
    {
        // Move the target randomly
        //if (_arenaSettings.TargetMovementSpeed > 0)
        //{
        //    ThisRigidbody.MovePosition(transform.position + (_arenaSettings.TargetMovementSpeed * Time.deltaTime * TargetDirection));
        //}

        NavMeshHit hit;
        var isOnNavMesh = NavMesh.SamplePosition(transform.position, out hit, 0.6f, NavMesh.AllAreas);
        if(!isOnNavMesh)
        {
            //Debug.Log("Not on NavMesh");
            PlaceTargetCubeRandomly();
        }

        if(timer != -1)
        {
            timer += Time.deltaTime;
        }

        if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            navAgent.SetDestination(newPos);
            timer = 0;
        }
        
        var localPosition = transform.localPosition;
        var x = localPosition.x;
        var z = localPosition.z;


        // var terrainHeight = TerrainGenerator.GetTerrainHeight(new Vector3(x, 0, z));
        // Safeguard if target is outside of arena
        //if (transform.localPosition.y < terrainHeight -1 || transform.localPosition.y> 40 || transform.localPosition.x is < -1 or > 129 || transform.localPosition.z is < -1 or > 129)
        //{
        //    localPosition = new Vector3(x,terrainHeight + 1f , z);
        //    transform.localPosition = localPosition;
        //}
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public void PlaceTargetCubeRandomly(){
        var x = UnityEngine.Random.Range(4 , DynamicEnvironmentGenerator.TerrainSize - 4);
        var z = UnityEngine.Random.Range(4, DynamicEnvironmentGenerator.TerrainSize  - 4);
        //var y = TerrainGenerator.GetTerrainHeight(new Vector3(x, 0, z)) + + transform.localScale.y/2;
        transform.localPosition = new Vector3(x, 0, z);
    }
    
    /// <summary> 
    /// Change direction randomly every x seconds
    /// </summary>
    /// <returns></returns>
    private IEnumerator ChangeDirection()
    {
        while (true)
        {
            //TargetDirection = Vector3.Normalize(new Vector3((float)(Rng.NextDouble() * 2) - 1, 0, (float)(Rng.NextDouble() * 2) - 1));
            //yield return new WaitForSeconds(UnityEngine.Random.Range(1, _arenaSettings.TargetMaxSecondsInOneDirection));
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
            Transform current_transform = collision.transform;
            while (current_transform.parent.name is not "Creature")
            {
                current_transform = current_transform.parent;
            }
            current_transform.parent.GetComponent<GenericAgent>().TouchedTarget();

        }
        if (collision.gameObject.name != "Terrain")
        {
            TargetDirection = Quaternion.AngleAxis(UnityEngine.Random.Range(60,170), Vector3.up) * TargetDirection;
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = UnityEngine.Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMeshHit navHit;

        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
}
