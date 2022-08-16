using System;
using Unity.AI.Navigation;
using UnityEngine;

namespace Config
{
    public class ArenaConfig: GenericConfig
    {
        [Header("Arena Settings")]
        [SerializeField]
        public int ArenaCount = 10;

        [Header("Target Cube Settings")]
        [SerializeField]
        public float MaxWalkingSpeed = 10;
        [SerializeField]
        public int EpisodeCountToRandomizeTargetCubePosition = 0;
        [SerializeField]
        public bool RandomizeWalkSpeedEachEpisode = false;
        [SerializeField] 
        public int TargetMaxSecondsInOneDirection = 10;
        [SerializeField]
        public float TargetMovementSpeed = 0f;
        [SerializeField]
        public float TargetWalkingSpeed = 10;

        [Header("Terrain settings")]
        [SerializeField]
        public bool GenerateHeights  = true;
        [SerializeField]
        public int RegenerateTerrainAfterXEpisodes = 0;
        [SerializeField]
        public int Depth = 10;
        [SerializeField]
        public float Scale = 2.5f;

        
        [HideInInspector] // TODO Activate when implemented
        public CollectObjects NavMeshSurfaceCollectObjects = CollectObjects.Children;
        [HideInInspector] // TODO Activate when implemented
        public bool BakeNavMesh = false;
        [HideInInspector, Tooltip("valid range (0, Anzahl der NavMeshAgents (in Navigation->Agents) -1)")] // TODO Activate when implemented. Valid range (0, NavMesh.GetSettingsCount-1)
        public int NavMeshBuildSettingIndex = 0;

        protected override void ExecuteAtLoad()
        {
            if (ArenaCount <= 0) throw new ArgumentException("We need at least one arena!");
            Debug.Log($"ArenaCount: {ArenaCount}");
        }
    }
}