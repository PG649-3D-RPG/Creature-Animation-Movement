using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class WalkTargetScript : MonoBehaviour
{
    [SerializeField]
    private GameObject _arenaTerrain;
    private int _arenaWidth;
    private int _arenaLength;
    [SerializeField]
    private bool _isMovingTarget = true;
    [SerializeField]
    private float _movementSpeed = 0.1f;
    [SerializeField]
    private bool _jumpingTarget = false;
    [SerializeField]
    private float _jumpingHeight = 0.1f;
    [SerializeField] private int _targetMaxSecondsInOneDirection = 10;

    private Random _random;

    private Vector3 _targetDirection;

    private Rigidbody _thisRigidbody;

    private TerrainGenerator _terrainGenerator;
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    /// <returns></returns>
    void Start()
    {
        _random = new Random();
        _terrainGenerator = _arenaTerrain.GetComponent<TerrainGenerator>();
        _arenaWidth = _terrainGenerator.width;
        _arenaLength = _terrainGenerator.length;
        _thisRigidbody = transform.GetComponentInChildren<Rigidbody>();
        PlaceTargetCubeRandomly();
        _ = StartCoroutine(nameof(ChangeDirection));
        if (_jumpingTarget) _ = StartCoroutine(nameof(Jump));
    }

    /// <summary>
    /// Run update at a fixed rate
    /// </summary>
    /// <returns></returns>
    public void FixedUpdate()
    {
        // Move the target randomly
        if (_isMovingTarget)
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
        var x = UnityEngine.Random.Range(4 , _arenaWidth-4);
        var z = UnityEngine.Random.Range(4, _arenaLength-4);
        var y = _terrainGenerator.GetTerrainHeight(x, z) + 1f;
        transform.localPosition = new Vector3(x, y, z);
    }

    /// <summary>
    /// Move the rigidbody of the target to a random position
    /// </summary>
    /// <returns></returns>
    public void MoveTargetRandomlyPerTick()
    {
        _thisRigidbody.MovePosition(transform.position + (_movementSpeed * Time.deltaTime * _targetDirection));
    }

    /// <summary> 
    /// Change direction randomly every x seconds
    /// </summary>
    /// <returns></returns>
    private IEnumerator ChangeDirection()
    {
        while (true)
        {
            _targetDirection = Vector3.Normalize(new Vector3((float)(_random.NextDouble() * 2) - 1, 0, (float)(_random.NextDouble() * 2) - 1));
            yield return new WaitForSeconds(UnityEngine.Random.Range(1, _targetMaxSecondsInOneDirection));
        }
    }

    /// <summary>
    /// Jump randomly every x seconds
    /// </summary>
    /// <returns></returns>
    private IEnumerator Jump()
    {
        while (true)
        {
            _thisRigidbody.MovePosition(transform.position + (_jumpingHeight * Time.deltaTime * (_targetDirection + Vector3.up)));
            yield return new WaitForSeconds(UnityEngine.Random.Range(1, 15));
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
            _targetDirection = Quaternion.AngleAxis(UnityEngine.Random.Range(60,170), Vector3.up) * _targetDirection;
        }
    }
}
