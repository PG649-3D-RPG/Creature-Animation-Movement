using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.MLAgents;

//
// Already Checked
//


namespace Unity.MLAgentsExamples
{
    /// <summary>
    /// Used to store relevant information for acting and learning for each body part in agent.
    /// </summary>
    [System.Serializable]
    public class BodyPart
    {
        public float BodyPartHeight {get; set;}
        [Header("Body Part Info")][Space(10)] public ConfigurableJoint joint;
        public Rigidbody rb;
        [HideInInspector] public Vector3 startingPos;
        [HideInInspector] public Quaternion startingRot;

        [Header("Ground & Target Contact")]
        [Space(10)]
        public GroundContact groundContact;

        public TargetContact targetContact;

        [FormerlySerializedAs("thisJDController")]
        [HideInInspector] public JointDriveController thisJdController;

        [Header("Current Joint Settings")]
        [Space(10)]
        public Vector3 currentEularJointRotation;

        [HideInInspector] public float currentStrength;
        // TODO: Required?
        public float currentXNormalizedRot;
        public float currentYNormalizedRot;
        public float currentZNormalizedRot;

        [Header("Other Debug Info")]
        [Space(10)]
        public Vector3 currentJointForce;

        public float currentJointForceSqrMag;
        public Vector3 currentJointTorque;
        public float currentJointTorqueSqrMag;
        public AnimationCurve jointForceCurve = new();
        public AnimationCurve jointTorqueCurve = new();

        public int GetNumberUnlockedAngularMotions()
        {
            var unlockedX = joint.angularXMotion != ConfigurableJointMotion.Locked ? 1 : 0;
            var unlockedY = joint.angularYMotion != ConfigurableJointMotion.Locked ? 1 : 0;
            var unlockedZ = joint.angularZMotion != ConfigurableJointMotion.Locked ? 1 : 0;

            return unlockedX + unlockedY + unlockedZ;
        }

        /// <summary>
        /// Reset body part to initial configuration.
        /// </summary>
        public void Reset(BodyPart bp, float terrainHeight, float yheightOffset = 0.05f)
        {
            //This resets the walker at the starting position
            bp.rb.transform.position = new Vector3(startingPos.x, terrainHeight + BodyPartHeight + yheightOffset, startingPos.z);
            //This will reset the walker at the current position
            //bp.rb.transform.position = // new Vector3(bp.rb.transform.position.x, terrainHeight + _bodyPartHeight + yheightOffset, bp.rb.transform.position.z);
            bp.rb.transform.rotation = bp.startingRot;
            bp.rb.velocity = Vector3.zero;
            bp.rb.angularVelocity = Vector3.zero;
            // Reset contact infos
            if (bp.groundContact)
            {
                bp.groundContact.TouchingGround = false;
            }

            if (bp.targetContact)
            {
                bp.targetContact.touchingTarget = false;
            }
        }

        /// <summary>
        /// Apply torque according to defined goal `x, y, z` angle and force `strength`.
        /// </summary>
        public void SetJointTargetRotation(float x, float y, float z)
        {
            x = (x + 1f) * 0.5f;
            y = (y + 1f) * 0.5f;
            z = (z + 1f) * 0.5f;

            //Debug.Log($"bodypart: {rb.gameObject.name}; x-limit-low: {joint.lowAngularXLimit.limit}; x-limit-high: {joint.highAngularXLimit.limit}, y-limti: {joint.angularYLimit.limit}, z-limit: {joint.angularZLimit.limit}");

            var xRot = Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x);
            var yRot = Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, y);
            var zRot = Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, z);

            currentXNormalizedRot =
                Mathf.InverseLerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, xRot);
            currentYNormalizedRot = Mathf.InverseLerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, yRot);
            currentZNormalizedRot = Mathf.InverseLerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, zRot);

            joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
            currentEularJointRotation = new Vector3(xRot, yRot, zRot);
        }

        public void SetJointStrength(float strength)
        {
            var rawVal = (strength + 1f) * 0.5f * thisJdController.maxJointForceLimit;
            var jd = new JointDrive
            {
                positionSpring = thisJdController.maxJointSpring,
                positionDamper = thisJdController.jointDampen,
                maximumForce = rawVal
            };
            joint.slerpDrive = jd;
            currentStrength = jd.maximumForce;
        }
    }

    public class JointDriveController : MonoBehaviour
    {
        [Header("Joint Drive Settings")]
        [Space(10)]

        public float maxJointSpring;
        public float jointDampen;
        public float maxJointForceLimit;

        // TODO: Required?
        float m_FacingDot;

        [HideInInspector] public Dictionary<Transform, BodyPart> bodyPartsDict = new();

        [HideInInspector] public List<BodyPart> bodyPartsList = new();

        // TODO: Changable?
        const float k_MaxAngularVelocity = 50.0f;

        /// <summary>
        /// Create BodyPart object and add it to dictionary.
        /// </summary>
        public void SetupBodyPart(Transform t, float bodyPartHeight = 0f)
        {
            var bp = new BodyPart
            {
                BodyPartHeight = bodyPartHeight,
                rb = t.GetComponent<Rigidbody>(),
                joint = t.GetComponent<ConfigurableJoint>(),
                startingPos = t.position,
                startingRot = t.rotation
            };
            bp.rb.maxAngularVelocity = k_MaxAngularVelocity;

            SetAllJointLimitsToFree(bp.joint);
            //SetLimitedJointLimitsToFree(bp.joint);

            // Add & setup the ground contact script
            bp.groundContact = t.GetComponent<GroundContact>();
            if (!bp.groundContact) // == Null
            {
                Debug.Assert(!bp.groundContact, "GroundContact script should be initialized before. Otherwise proper config of penalty or abort episode is missing. ");
                bp.groundContact = t.gameObject.AddComponent<GroundContact>();
            }
            bp.groundContact.Agent = gameObject.GetComponent<Agent>();

            if (bp.joint)
            {
                var jd = new JointDrive
                {
                    positionSpring = maxJointSpring,
                    positionDamper = jointDampen,
                    maximumForce = maxJointForceLimit
                };
                bp.joint.slerpDrive = jd;
            }

            var connectedCollider = bp.joint.connectedBody.transform.GetComponent<Collider>();

            if (connectedCollider != null) {
                Physics.IgnoreCollision(t.GetComponent<Collider>(), connectedCollider);
            }

            bp.thisJdController = this;
            bodyPartsDict.Add(t, bp);
            bodyPartsList.Add(bp);
        }

        public void GetCurrentJointForces()
        {
            foreach (var bodyPart in bodyPartsDict.Values.Where(bodyPart => bodyPart.joint))
            {
                bodyPart.currentJointForce = bodyPart.joint.currentForce;
                bodyPart.currentJointForceSqrMag = bodyPart.joint.currentForce.magnitude;
                bodyPart.currentJointTorque = bodyPart.joint.currentTorque;
                bodyPart.currentJointTorqueSqrMag = bodyPart.joint.currentTorque.magnitude;
                if (Application.isEditor)
                {
                    if (bodyPart.jointForceCurve.length > 1000)
                    {
                        bodyPart.jointForceCurve = new AnimationCurve();
                    }

                    if (bodyPart.jointTorqueCurve.length > 1000)
                    {
                        bodyPart.jointTorqueCurve = new AnimationCurve();
                    }

                    bodyPart.jointForceCurve.AddKey(Time.time, bodyPart.currentJointForceSqrMag);
                    bodyPart.jointTorqueCurve.AddKey(Time.time, bodyPart.currentJointTorqueSqrMag);
                }
            }
        }

        private void SetAllJointLimitsToFree(ConfigurableJoint cJoint)
        {
            cJoint.angularXMotion = ConfigurableJointMotion.Free;
            cJoint.angularYMotion = ConfigurableJointMotion.Free;
            cJoint.angularZMotion = ConfigurableJointMotion.Free;
        }

        private void SetLimitedJointLimitsToFree(ConfigurableJoint cJoint)
        {
            if(cJoint.angularXMotion != ConfigurableJointMotion.Locked)
            {
                cJoint.angularXMotion = ConfigurableJointMotion.Free;
            }
            if(cJoint.angularYMotion != ConfigurableJointMotion.Locked)
            {
                cJoint.angularYMotion = ConfigurableJointMotion.Free;
            }
            if(cJoint.angularZMotion != ConfigurableJointMotion.Locked)
            {
                cJoint.angularZMotion = ConfigurableJointMotion.Free;
            }
	    }
    }
}
