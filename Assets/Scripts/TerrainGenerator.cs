using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int width = 128; //x-axis of the terrain
    public int length = 128; //z-axis

    public int depth = 10; //y-axis

    public float scale = 2.5f;

    public float offsetX = 100f;
    public float offsetY = 100f;

    [SerializeField]
    private GameObject _obstaclesContainer;
    [SerializeField]
    private GameObject _obstaclesPrefab;
    [SerializeField]
    private float _threshold = 0.9f;
    [SerializeField]
    private float _scaleObstacle = 10f;
    [SerializeField]
    private WalkTargetScript _target;

    private void Start()
    {
        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);
    }

    private void Update() {
        //Terrain terrain = GetComponent<Terrain>();
        //terrain.terrainData = GenerateTerrain(terrain.terrainData);

        if(Input.GetKeyDown(KeyCode.Space)){
            RegenerateTerrain();
            if(_target != null){
                //RegenerateTerrain muss fertig sein, bevor das Target neu gesetzt wird.
                //Ansonsten kann es passieren, das der Cube durch den Boden fällt.
                _target.PlaceTargetCubeRandomly();
            }
        }
    }

    public int GetArenaWidth(){
        return width;
    }

    public int GetArenaLength(){
        return length;
    }

    public float GetTerrainHeight(int x, int y){
        Terrain terrain = GetComponent<Terrain>();
        return terrain.terrainData.GetHeight(x, y);
    }

    private void KillObstacleChildren(){
        foreach (Transform child in _obstaclesContainer.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public void RegenerateTerrain()
    {
        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);
        KillObstacleChildren();

        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);

    }

    TerrainData GenerateTerrain (TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, length);

        terrainData.SetHeights(0, 0, GenerateHeights());

        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                if(PlaceObstacleOnPos(x,y)){
                    GameObject newObstacle = GameObject.Instantiate(_obstaclesPrefab, Vector3.zero, Quaternion.identity, _obstaclesContainer.transform);
                    newObstacle.transform.localPosition = new Vector3(x, terrainData.GetHeight(x,y) + 2f , y);
                }
            }
        }
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, length];
        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                heights[x, y] = CalculateHeight(x, y);
                //if(PlaceObstacleOnPos(x,y)){
                //    GameObject.Instantiate(_obstaclesPrefab, new Vector3(x, heights[x,y] , y), Quaternion.identity, _obstaclesContainer.transform);
                //}
            }
        }

        return heights;
    }

    bool PlaceObstacleOnPos(int x, int y){
        float xCoord = ((float)x + offsetX) / width * _scaleObstacle;
        float yCoord = ((float)y + offsetY) / length * _scaleObstacle + offsetY;

       
        return Mathf.PerlinNoise(xCoord, yCoord) > _threshold;
    }

    float CalculateHeight (int x, int y)
    {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / length * scale + offsetY;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
