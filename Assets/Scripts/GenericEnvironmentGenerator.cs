using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

public class GenericEnvironmentGenerator : MonoBehaviour
{
    [SerializeField] public NNModel NnModel;
    [HideInInspector] public GameObject TerrainObject;

}
