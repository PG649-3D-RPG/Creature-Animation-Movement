using System;
using UnityEngine;

namespace Config
{
    public class MlAgentConfig  : GenericConfig
    {
        [Header("Creature Speed Settings")]
        [Space(10)]
        [SerializeField]
        public bool RandomizeWalkSpeedEachEpisode = false;
        [SerializeField]
        public float MaxWalkingSpeed = 10;
        [SerializeField]
        public float TargetWalkingSpeed = 10;
        [Header("Learning Settings")]
        [Space(10)]
        [SerializeField]
        public bool CalculateActionSpace = false;
        [SerializeField]
        public int ContinuousActionSpace = 100;
        [SerializeField]
        public int DiscreteBranches = 0;
        [SerializeField]
        public int MaxStep = 5000;
        [SerializeField]
        public int ObservationSpace = 100;
        [SerializeField]
        public bool TakeActionsBetweenDecisions = false;
        [SerializeField]
        public int DecisionPeriod = 0;
        [Header("Joint Settings")]
        [Space(10)]
        [SerializeField]
        public float JointDampen = 5000;
        [SerializeField]
        public float MaxJointForceLimit = 20000;
        [SerializeField]
        public float MaxJointSpring = 40000;

        protected override void ExecuteAtLoad()
        {
            // Empty on purpose
        }
    }
}