using System;
using UnityEngine;

namespace Config
{
    public class MlAgentConfig  : GenericConfig
    {
        [Header("ML-Agent Settings settings")]
        [Space(10)]
        [SerializeField]
        public float MaxWalkingSpeed = 10;
        [SerializeField]
        public float TargetWalkingSpeed = 10;
        [SerializeField]
        public int ContinuousActionSpaceOffset = 100;
        [SerializeField]
        public int DiscreteBranches = 0;
        [SerializeField]
        public float JointDampen = 5000;
        [SerializeField]
        public float MaxJointForceLimit = 20000;
        [SerializeField]
        public float MaxJointSpring = 40000;
        [SerializeField]
        public int MaxStep = 5000;
        [SerializeField]
        public int ObservationSpaceOffset = 100;
        [SerializeField]
        public bool TakeActionsBetweenDecisions = false;
        [SerializeField]
        public int DecisionPeriod = 0;

        protected override void ExecuteAtLoad()
        {
            // Empty on purpose
        }
    }
}