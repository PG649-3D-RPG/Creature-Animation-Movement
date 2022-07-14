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
        [HideInInspector] public Agent agent;

        public bool touchingGround;
        private string k_Ground; // Tag of ground object.
        private DynamicEnviormentGenerator deg;
        private Bone boneScript;
        public void Awake()
        {
            deg = GameObject.FindObjectOfType<DynamicEnviormentGenerator>();
            k_Ground = deg.GroundTag;
            boneScript = this.GetComponentInParent<Bone>();
        }

        /// <summary>
        /// Check for collision with ground, and optionally penalize agent. Ground needs to be tagged with "ground".
        /// </summary>
        void OnCollisionEnter(Collision col)
        {

            // TODO: Ground should be tagged
            if (col.transform.CompareTag(k_Ground))
            {
                touchingGround = true;
                
                if (deg.PenaltiesForBodyParts.TryGetValue(boneScript.category, out var groundContactPenalty))
                {                    
                    agent.SetReward(groundContactPenalty);
                }

                if (deg.ResetOnGroundContactParts.Contains(boneScript.category))
                {
                    agent.EndEpisode();
                }
            }
        }

        /// <summary>
        /// Check for end of ground collision and reset flag appropriately.
        /// </summary>
        void OnCollisionExit(Collision other)
        {
            if (other.transform.CompareTag(k_Ground))
            {
                touchingGround = false;
            }
        }
    }
}
