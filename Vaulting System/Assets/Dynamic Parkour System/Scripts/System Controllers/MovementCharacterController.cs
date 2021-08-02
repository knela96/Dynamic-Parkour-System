using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public enum MovementState { Walking, Running, Jumping }

    [RequireComponent(typeof(ThirdPersonController))]
    public class MovementCharacterController : MonoBehaviour
    {
        #region Variables
        private ThirdPersonController controller;
        private Animator anim;
        public Rigidbody rb;
        private Vector3 velocity;
        private Vector3 animVelocity;
        public float speed = 6f;
        private float smoothSpeed;

        public float walkSpeed;
        public float JogSpeed;
        public float RunSpeed;
        public float jumpForce;
        public float fallForce;
        float timeDrop = 0;

        private Vector3 leftFootPosition, leftFootIKPosition, rightFootPosition, rightFootIKPosition;
        Quaternion leftFootIKRotation, rightFootIKRotation;
        private float lastPelvisPositionY, lastLeftFootPosition, lastRightFootPosition;
        [Header("Feet IK")]
        public bool enableFeetIK = true;
        [SerializeField] private float heightFromGroundRaycast = -0.03f;
        [Range(0, 2f)] [SerializeField] private float raycastDownDistance = 1.5f;
        [SerializeField] private LayerMask environmentLayer;
        [SerializeField] private float pelvisOffset = 0f;
        [Range(0, 1f)] [SerializeField] private float pelvisUpandDownSpeed = 0.25f;
        [Range(0, 1f)] [SerializeField] private float feetToIKPositionSpeed = 0.25f;

        public string leftFootAnimVariableName = "LeftFootCurve";
        public string rightFootAnimVariableName = "RightFootCurve";

        public bool useProIKFeature = false;
        public bool showDebug = true;
        public bool limitMovement = false;
        public bool stopMotion = false;

        MovementState currentState;

        public delegate void OnLandedDelegate();
        public delegate void OnFallDelegate();
        public event OnLandedDelegate OnLanded;
        public event OnFallDelegate OnFall;


        #endregion
        // Start is called before the first frame update
        void Start()
        {
            controller = GetComponent<ThirdPersonController>();
            rb = GetComponent<Rigidbody>();
            anim = controller.characterAnimation.animator;
            SetCurrentState(MovementState.Walking);
        }

        // Update is called once per frame
        void Update()
        {
            if (!controller.dummy)
            {
                if (controller.isGrounded)
                {
                    EnableFeetIK();
                    lastPelvisPositionY = 0.0f;

                    limitMovement = CheckBoundaries();

                    //Drop if Space or moving drop direction furing 0.2s
                    if (limitMovement && controller.isGrounded && ((controller.characterInput.drop && !controller.isJumping) || timeDrop > 0.2f))
                    {
                        anim.CrossFade("Jump Down", 0.1f);
                        controller.isJumping = true;
                        timeDrop = -1;
                    }
                }

                if (!stopMotion)
                    ApplyInputMovement();

            }            

            if (controller.isGrounded)
            {
                OnLanded();
                controller.isJumping = false;
            }
            else
            {
                timeDrop = 0;
                OnFall();
            }
        }



        private void FixedUpdate()
        {
            if (!enableFeetIK || controller.dummy)
                return;
            if (anim == null)
                return;

            AdjustFeetTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
            AdjustFeetTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

            //Raycast to Ground
            FeetPositionSolver(rightFootPosition, ref rightFootIKPosition, ref rightFootIKRotation);
            FeetPositionSolver(leftFootPosition, ref leftFootIKPosition, ref leftFootIKRotation);
        }

        #region Movement
        public void ApplyInputMovement()
        {
            //Apply fall multiplier
            if (rb.velocity.y <= 0)
            {
                rb.velocity += Vector3.up * Physics.gravity.y * (fallForce - 1) * Time.deltaTime;
            }

            if (GetState() == MovementState.Running)
            {
                velocity.Normalize();
            }

            if (velocity.magnitude > 0.3f)
            {
                smoothSpeed = Mathf.Lerp(smoothSpeed, speed, Time.deltaTime * 2);
                rb.velocity = new Vector3(velocity.x * smoothSpeed, velocity.y * smoothSpeed + rb.velocity.y, velocity.z * smoothSpeed);

                //Detect Player on Irregular Surface and adjust movement to avoid slowing down
                RaycastHit hit;
                controller.characterDetection.ThrowRayOnDirection(transform.position, Vector3.down, 1.0f, out hit);
                if (hit.normal != Vector3.up)
                {
                    rb.velocity += -new Vector3(hit.normal.x, 0, hit.normal.z) * 1.0f;
                    rb.velocity = rb.velocity + Vector3.up * Physics.gravity.y * 1.6f * Time.deltaTime;
                }

                controller.characterAnimation.SetAnimVelocity(rb.velocity);
            }
            else
            {
                //Lerp down with current velocity of the rigidbody
                smoothSpeed = Mathf.SmoothStep(smoothSpeed, 0, Time.deltaTime * 20);
                rb.velocity = new Vector3(rb.velocity.normalized.x * smoothSpeed, rb.velocity.y, rb.velocity.normalized.z * smoothSpeed);
                controller.characterAnimation.SetAnimVelocity(controller.characterAnimation.GetAnimVelocity().normalized * smoothSpeed);
            }
        }

        public bool CheckBoundaries()
        {
            if (timeDrop == -1)
                return false;


            bool ret = false;
            Vector3 origin = transform.position + transform.forward * 0.5f + new Vector3(0, 0.5f, 0);

            if (!Physics.Raycast(origin, Vector3.down, 1))
                ret = CheckSurfaceBoundary(origin);
            if (!Physics.Raycast(origin + transform.right * 0.25f, Vector3.down, 1) && ret == false)
                ret = CheckSurfaceBoundary(origin + transform.right * 0.25f);
            if (!Physics.Raycast(origin + transform.right * -0.25f, Vector3.down, 1) && ret == false)
                ret = CheckSurfaceBoundary(origin + transform.right * -0.25f);

            if (ret == false)
                timeDrop = 0;

            if (showDebug)
            {
                Debug.DrawLine(origin, origin + Vector3.down * 1);
                Debug.DrawLine(origin + transform.right * 0.25f, origin + transform.right * 0.25f + Vector3.down * 1);
                Debug.DrawLine(origin + transform.right * -0.25f, origin + transform.right * -0.25f + Vector3.down * 1);
                Debug.DrawLine(origin, origin + Vector3.down * 3);
            }

            return ret;
        }

        private bool CheckSurfaceBoundary(Vector3 origin)
        {
            RaycastHit hit;
            bool allowJump = Physics.Raycast(origin, Vector3.down, out hit, 3, environmentLayer.value); //Allows automatic drop if next surface is reachable
            
            if (hit.point.y - transform.position.y < 0.5f)
                return true;

            Vector3 origin2 = transform.position + transform.forward * 0.8f + new Vector3(0, -0.03f, 0);
            if (showDebug)
                Debug.DrawLine(origin2, transform.position + new Vector3(0, -0.03f, 0));

            RaycastHit hit1;
            if (Physics.Raycast(origin2, -transform.forward, out hit1, 1))
            {
                if (showDebug)
                    Debug.DrawLine(hit1.point, hit1.point + hit1.normal, Color.cyan);

                RaycastHit hit2;
                RaycastHit hit3;
                Physics.Raycast(origin2 + transform.right * 0.05f, -transform.forward, out hit2, 1);
                Physics.Raycast(origin2 + transform.right * -0.05f, -transform.forward, out hit3, 1);

                if (hit2.normal == Vector3.zero)
                    hit2.normal = hit1.normal;

                if (hit3.normal == Vector3.zero)
                    hit3.normal = hit1.normal;

                Vector3 right = Vector3.Cross(Vector3.up, hit1.normal); //Get tangent of current surface (Right Vector)
                float vel = Vector3.Dot(velocity.normalized, right); //We get the projection of velocity vector to the tangent

                if (vel < 0.4 && vel > -0.4 && velocity.magnitude > 0.1f && hit1.normal.y <= 0.2)
                {
                    timeDrop += Time.deltaTime;
                    vel = 0;
                }
                else
                {
                    timeDrop = 0;
                }

                if (hit1.normal != hit2.normal || hit1.normal != hit3.normal) //These normal checks are used to detect corners and avoid problems
                    vel = 0;

                velocity = right * vel;

                return true;
            }

            return false;
        }

        public Vector3 GetVelocity() { return rb.velocity; }
        public void SetVelocity(Vector3 value)
        {
            velocity = value;
        }

        public void ResetSpeed()
        {
            smoothSpeed = 0;
        }

        public void SetCurrentState(MovementState state)
        {
            currentState = state;

            switch (currentState)
            {
                case MovementState.Walking:
                    speed = walkSpeed;
                    break;
                case MovementState.Running:
                    speed = RunSpeed;
                    smoothSpeed = speed;
                    break;
            }
        }

        public MovementState GetState()
        {
            return currentState;
        }

        public void Jump()
        {
            if (controller.isGrounded)
            {
                rb.velocity = Vector3.up * jumpForce;
                controller.isJumping = true;
            }
        }

        #endregion

        #region Foot IK


        private void OnAnimatorIK(int layerIndex)
        {
            if (!enableFeetIK || controller.dummy)
                return;
            if (anim == null)
                return;

            MovePelvisHeight();

            //Right Foot IK Position and Rotation
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);

            if (useProIKFeature)
            {
                anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, anim.GetFloat(rightFootAnimVariableName));
            }

            MoveFeetToIKPoint(AvatarIKGoal.RightFoot, rightFootIKPosition, rightFootIKRotation, ref lastRightFootPosition);

            //Left Foot IK Position and Rotation
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);

            if (useProIKFeature)
            {
                anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, anim.GetFloat(leftFootAnimVariableName));
            }

            MoveFeetToIKPoint(AvatarIKGoal.LeftFoot, leftFootIKPosition, leftFootIKRotation, ref lastLeftFootPosition);
        }

        void MoveFeetToIKPoint(AvatarIKGoal foot, Vector3 positionIKHolder, Quaternion rotationHolder, ref float lastFootPositionY)
        {
            Vector3 targetIKPosition = anim.GetIKPosition(foot);

            if (positionIKHolder != Vector3.zero)
            {
                targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
                positionIKHolder = transform.InverseTransformPoint(positionIKHolder);
                float yVariable = Mathf.Lerp(lastFootPositionY, positionIKHolder.y, feetToIKPositionSpeed);
                targetIKPosition.y += yVariable;

                lastFootPositionY = yVariable;

                targetIKPosition = transform.TransformPoint(targetIKPosition);

                anim.SetIKRotation(foot, rotationHolder);
            }
            anim.SetIKPosition(foot, targetIKPosition);
        }

        private void MovePelvisHeight()
        {
            if (rightFootIKPosition == Vector3.zero || leftFootIKPosition == Vector3.zero || lastPelvisPositionY == 0)
            {
                lastPelvisPositionY = anim.bodyPosition.y;
                return;
            }

            float leftOffsetPosition = leftFootIKPosition.y - transform.position.y;
            float rightOffsetPosition = rightFootIKPosition.y - transform.position.y;

            float totalOffset = leftOffsetPosition < rightOffsetPosition ? leftOffsetPosition : rightOffsetPosition;

            Vector3 newPelvisPosition = anim.bodyPosition + Vector3.up * totalOffset;

            newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpandDownSpeed);

            anim.bodyPosition = newPelvisPosition;

            lastPelvisPositionY = anim.bodyPosition.y;
        }

        //Raycast handler
        private void FeetPositionSolver(Vector3 fromRaycastPosition, ref Vector3 feetIKPositions, ref Quaternion feetIKRotations)
        {
            RaycastHit feetHit;

            if (showDebug)
                Debug.DrawLine(fromRaycastPosition, fromRaycastPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast), Color.green);

            if (Physics.Raycast(fromRaycastPosition, Vector3.down, out feetHit, raycastDownDistance + heightFromGroundRaycast, environmentLayer))
            {
                feetIKPositions = fromRaycastPosition;
                feetIKPositions.y = feetHit.point.y + pelvisOffset;
                feetIKRotations = Quaternion.FromToRotation(Vector3.up, feetHit.normal) * transform.rotation;
                return;
            }

            feetIKPositions = Vector3.zero;
        }

        private void AdjustFeetTarget(ref Vector3 feetPositions, HumanBodyBones foot)
        {
            feetPositions = anim.GetBoneTransform(foot).position;
            feetPositions.y = transform.position.y + heightFromGroundRaycast;
        }

        #endregion


        #region Climb

        public void SetKinematic(bool active)
        {
            rb.isKinematic = active;
        }


        #endregion

        public void EnableFeetIK()
        {
            enableFeetIK = true;
        }

        public void ApplyGravity()
        {
            rb.velocity += Vector3.up * -0.300f;
        }
    }

}