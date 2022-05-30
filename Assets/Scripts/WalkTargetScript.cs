using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkTargetScript : MonoBehaviour
{
    [SerializeField]
    private GameObject _arenaTerrain;
    private int _arenaWidth;
    private int _arenaLength;

    private TerrainGenerator _terrainGenerator;
    // Start is called before the first frame update
    void Start()
    {
        _terrainGenerator = _arenaTerrain.GetComponent<TerrainGenerator>();
        _arenaWidth = _terrainGenerator.GetArenaWidth();
        _arenaLength = _terrainGenerator.GetArenaLength();
        PlaceTargetCubeRandomly();
    }

    // Update is called once per frame
    void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Space)){
          //  PlaceTargetCubeRandomly();
        //}
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
}
