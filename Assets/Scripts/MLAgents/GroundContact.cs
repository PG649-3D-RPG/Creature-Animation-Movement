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
        public bool TouchingWall;
        private DynamicEnvironmentGenerator _deg;
        private Bone _boneScript;
        private CreatureConfig _creatureConfig;
        private Terrain _terrain;
        private const string GroundTag = "ground";

        public GroundContact(Agent agent)
        {
            Agent = agent;
        }

        public void Awake()
        {
            _deg = FindObjectOfType<DynamicEnvironmentGenerator>();
            _creatureConfig = FindObjectOfType<CreatureConfig>();
            _terrain = FindObjectOfType<Terrain>();
            _boneScript = GetComponentInParent<Bone>();
        }

        /// <summary>
        /// Check for collision with ground, and optionally penalize agent. Ground needs to be tagged with "ground".
        /// </summary>
        private void OnCollisionEnter(Collision col)
        {
            if (col.transform.CompareTag(GroundTag))
            {
                var isWall = false;
                var contacts = new ContactPoint[col.contactCount];
                col.GetContacts(contacts);
                
                foreach (var pos in contacts)
                {
                    if (_terrain.SampleHeight(pos.point) > 0.3){
                        if(Application.isEditor) Debug.LogError($"{_boneScript.transform.name} touched the wall");
                        isWall = true;
                    }
                }

                if (!isWall)
                {
                    TouchingGround = true;
                    switch (_boneScript.category)
                    {
                        case BoneCategory.Leg:
                        case BoneCategory.Arm:
                        case BoneCategory.Torso:
                        case BoneCategory.Head:
                        case BoneCategory.Hand:
                        case BoneCategory.Shoulder:
                        case BoneCategory.Hip:
                            Agent.SetReward(_creatureConfig.ContactPenalty);
                            break;
                    }
                    if (_creatureConfig.ResetOnGroundContactParts.Contains(_boneScript.category))
                    {

                        Agent.EndEpisode();
                        if(Application.isEditor) Debug.LogError($"{_boneScript.transform.name} touched the ground");

                    }
                }
            }

            TouchingWall = true;
        }

        /// <summary>
        /// Check for end of ground collision and reset flag appropriately.
        /// </summary>
        private void OnCollisionExit(Collision other)
        {
            if (other.transform.CompareTag(GroundTag))
            {
                TouchingGround = false;
                TouchingWall = false;
            }
        }
    }
}
