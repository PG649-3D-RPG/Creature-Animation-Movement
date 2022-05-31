using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkTargetScript : MonoBehaviour
{
    [SerializeField]
    private GameObject _arenaTerrain;
    private int _arenaWidth;
    private int _arenaLength;
    [SerializeField]
    private bool _isMovingTarget = true;
    [SerializeField]
    private double _movement_speed = 0.1;
    [SerializeField]
    private bool _jumping_target = false;

    private System.Random _random;

    private double _xDirection;

    private double _zDirection;

    private TerrainGenerator _terrainGenerator;
    // Start is called before the first frame update
    void Start()
    {
        _random = new System.Random();
        _terrainGenerator = _arenaTerrain.GetComponent<TerrainGenerator>();
        _arenaWidth = _terrainGenerator.GetArenaWidth();
        _arenaLength = _terrainGenerator.GetArenaLength();
        PlaceTargetCubeRandomly();

        StartCoroutine("ChangeDirection");
    }

    // Update is called once per frame
    void Update()
    {
        if (_isMovingTarget)
        {
            MoveTargetRandomlyPerTick();
        }
        if (Input.GetKeyDown(KeyCode.F1)){
            MoveTargetRandomlyPerTick();
        }
        if(transform.localPosition.y < -5f){//The TargetCube fell through the floor
            PlaceTargetCubeRandomly();
        }
    }

    public void PlaceTargetCubeRandomly(){
        int x = Random.Range(0 , _arenaWidth);
        int z = Random.Range(0, _arenaLength);
        float y = _terrainGenerator.GetTerrainHeight(x, z) + 1f;
        this.transform.localPosition = new Vector3(x, y, z);
    }

    public void MoveTargetRandomlyPerTick()
    {
        var rndXMovement = (float) ((_random.NextDouble() * _xDirection)*  _movement_speed);
        var rndYMovement = _jumping_target ? (float) (_random.NextDouble() *  _movement_speed) : 0;
        var rndZMovement = (float) ((_random.NextDouble() * _zDirection) * _movement_speed);

        transform.Translate(rndXMovement, 0, rndZMovement);
    }

    private IEnumerator ChangeDirection()
    {
        while (true)
        {
            _xDirection = _random.NextDouble() * 2 - 1;
            _zDirection = _random.NextDouble() * 2 - 1;
            yield return new WaitForSeconds(Random.Range(1,10));
        }
        
    }

}
