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
        public int EpisodeCountToRandomizeTargetCubePosition = 0;
        [SerializeField] 
        public int TargetMaxSecondsInOneDirection = 10;
        [SerializeField]
        public float TargetMovementSpeed = 0f;
        

        [Header("Terrain settings")]
        [SerializeField]
        public bool GenerateHeights  = true;
        [SerializeField]
        public int RegenerateTerrainAfterXEpisodes = 0;
        [SerializeField]
        public int Depth = 10;
        [SerializeField]
        public float Scale = 2.5f;

        [SerializeField]
        public bool GenerateObstacles = false;
        [SerializeField, Range(0, 50)]
        public int numberObstacles = 0;

        
        public CollectObjects NavMeshSurfaceCollectObjects = CollectObjects.Children;
        public bool BakeNavMesh = false;
        [Tooltip("valid range (0, Anzahl der NavMeshAgents (in Navigation->Agents) -1)")]// Valid range (0, NavMesh.GetSettingsCount-1)
        public int NavMeshBuildSettingIndex = 0;

        protected override void ExecuteAtLoad()
        {
            if (ArenaCount <= 0) throw new ArgumentException("We need at least one arena!");
        }
    }
}