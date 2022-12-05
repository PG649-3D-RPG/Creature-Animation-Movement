using System;
using UnityEngine;
using Unity.MLAgents;


//
// Already Checked
//

namespace Unity.MLAgentsExamples
{
    /// <summary>
    /// This class contains logic for locomotion agents with joints which might make contact with the ground.
    /// By attaching this as a component to those joints, their contact with the ground can be used as either
    /// an observation for that agent, and/or a means of punishing the agent for making undesirable contact.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundContact : MonoBehaviour
    {
        public Agent Agent;
        public bool TouchingGround;
        private DynamicEnvironmentGenerator _deg;
        private Bone _boneScript;
        private CreatureConfig _creatureConfig;
        private const string GroundTag = "ground";

        public GroundContact(Agent agent)
        {
            Agent = agent;
        }

        public void Awake()
        {
            _deg = FindObjectOfType<DynamicEnvironmentGenerator>();
            _creatureConfig = FindObjectOfType<CreatureConfig>();

            _boneScript = GetComponentInParent<Bone>();
        }

        /// <summary>
        /// Check for collision with ground, and optionally penalize agent. Ground needs to be tagged with "ground".
        /// </summary>
        private void OnCollisionEnter(Collision col)
        {
            if (col.transform.CompareTag(GroundTag))
            {
                TouchingGround = true;
                if (_boneScript.category is BoneCategory.Leg or BoneCategory.Arm or BoneCategory.Torso
                    or BoneCategory.Head or BoneCategory.Hand or BoneCategory.Shoulder or BoneCategory.Hip)
                    Agent.SetReward(_creatureConfig.ContactPenalty);

                if (_boneScript.category != BoneCategory.Foot && Agent.CompletedEpisodes < 100000000) Agent.EndEpisode();
            }
        }

        /// <summary>
        /// Check for end of ground collision and reset flag appropriately.
        /// </summary>
        private void OnCollisionExit(Collision other)
        {
            if (other.transform.CompareTag(GroundTag)) TouchingGround = false;
        }
    }
}
