using System.Collections;
using System.Collections.Generic;
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
        private string _kGround; // Tag of ground object.
        private DynamicEnviormentGenerator _deg;
        private Bone _boneScript;
        private CreatureConfig _creatureConfig;

        public GroundContact(Agent agent)
        {
            Agent = agent;
        }

        public void Awake()
        {
            _deg = FindObjectOfType<DynamicEnviormentGenerator>();
            _creatureConfig = FindObjectOfType<CreatureConfig>();

            _kGround = _deg.GroundTag;
            _boneScript = GetComponentInParent<Bone>();
        }

        /// <summary>
        /// Check for collision with ground, and optionally penalize agent. Ground needs to be tagged with "ground".
        /// </summary>
        private void OnCollisionEnter(Collision col)
        {
            if (col.transform.CompareTag(_kGround))
            {
                TouchingGround = true;
                if (_creatureConfig.PenaltiesForBodyParts.TryGetValue(_boneScript.category, out var groundContactPenalty)) Agent.SetReward(groundContactPenalty);
                if (_creatureConfig.ResetOnGroundContactParts.Contains(_boneScript.category)) Agent.EndEpisode();
            }
        }

        /// <summary>
        /// Check for end of ground collision and reset flag appropriately.
        /// </summary>
        private void OnCollisionExit(Collision other)
        {
            if (other.transform.CompareTag(_kGround)) TouchingGround = false;
        }
    }
}
