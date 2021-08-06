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
        public float timeDrop = 0;
        private float velLimit = 0;

        private Vector3 leftFootPosition, leftFootIKPosition, rightFootPosition, rightFootIKPosition;
        Quaternion leftFootIKRotation, rightFootIKRotation;
        private float lastPelvisPositionY, lastLeftFootPosition, lastRightFootPosition;
        [Header("Feet IK")]
        public bool enableFeetIK = true;
        [SerializeField] private float heightFromGroundRaycast = -0.03f;
        [Range(0, 2f)] [SerializeField] public float raycastDownDistance = 1.5f;
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
            if (controller.isVaulting)
                return;

            if (controller.isGrounded && !controller.isJumping)
            {
                if (limitMovement && velLimit == 0 && timeDrop != -1)
                {
                    timeDrop += Time.deltaTime;

                    //Checks if below surface is too low and denies drop
                    if(timeDrop > 0.15f)
                    {
                        Vector3 origin = transform.position + transform.forward * 0.5f;
                        RaycastHit hit;
                        if (!Physics.Raycast(origin, Vector3.down, out hit, 1.5f, controller.characterDetection.wallLayer.value))
                        {
                            timeDrop = 0;
                        }
                        else if(hit.point.y - transform.position.y <= controller.stepHeight)
                        {
                            timeDrop = 0;
                        }
                    }
                }
                else
                {
                    timeDrop = 0;
                }

                //Drop if drop input or moving drop direction during 0.2s
                if (limitMovement && (controller.characterInput.drop || timeDrop > 0.15f) && controller.characterInput.movement != Vector2.zero)
                {
                    anim.CrossFade("Jump Down Slow", 0.1f);
                    timeDrop = -1;
                    controller.isJumping = true;
                }
            }

            if (controller.isJumping)
            {
                controller.allowMovement = true;

                if (anim.GetCurrentAnimatorStateInfo(0).IsName("Jump Down Slow") || anim.GetCurrentAnimatorStateInfo(0).IsName("Jump From Wall") || anim.GetCurrentAnimatorStateInfo(0).IsName("Freehang Drop"))
                {
                    Fall();
                }
                else if (controller.isGrounded && controller.onAir && anim.GetCurrentAnimatorStateInfo(0).IsName("Fall Idle"))
                {
                    OnLanded();
                    controller.isJumping = false;
                    controller.onAir = false;
                    timeDrop = 0;
                }
            }
        }

        public void Fall()
        {
            controller.onAir = true;
            OnFall();
        }

        private void FixedUpdate()
        {
            if (controller.isGrounded)
            {
                limitMovement = CheckBoundaries();
            }

            if (!controller.dummy)
            {
                if (!stopMotion)
                {
                    ApplyInputMovement();
                }
            }

            if (!controller.dummy && controller.isJumping && controller.characterInput.movement != Vector2.zero)
            {
                //Grants movement while falling
                rb.position += (transform.forward * walkSpeed) * Time.fixedDeltaTime;
            }

            //IKs
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
            if (GetState() == MovementState.Running)
            {
                velocity.Normalize();
            }

            if (velocity.magnitude > 0.3f)
            {
                smoothSpeed = Mathf.Lerp(smoothSpeed, speed, Time.fixedDeltaTime * 2);
                rb.velocity = new Vector3(velocity.x * smoothSpeed, velocity.y * smoothSpeed + rb.velocity.y, velocity.z * smoothSpeed);

                //Detect Player on Irregular Surface and adjust movement to avoid slowing down
                RaycastHit hit;
                controller.characterDetection.ThrowRayOnDirection(transform.position, Vector3.down, 1.0f, out hit);
                if (hit.normal != Vector3.up)
                {
                    controller.inSlope = true;
                    rb.velocity += -new Vector3(hit.normal.x, 0, hit.normal.z) * 1.0f;
                    rb.velocity = rb.velocity + Vector3.up * Physics.gravity.y * 1.6f * Time.fixedDeltaTime;
                }
                else
                {
                    controller.inSlope = false;
                }

                AutoStep();

                controller.characterAnimation.SetAnimVelocity(rb.velocity);
            }
            else
            {
                //Lerp down with current velocity of the rigidbody
                smoothSpeed = Mathf.SmoothStep(smoothSpeed, 0, Time.fixedDeltaTime * 20);
                rb.velocity = new Vector3(rb.velocity.normalized.x * smoothSpeed, rb.velocity.y, rb.velocity.normalized.z * smoothSpeed);
                controller.characterAnimation.SetAnimVelocity(controller.characterAnimation.GetAnimVelocity().normalized * smoothSpeed);
            }

            //Apply fall multiplier
            if (rb.velocity.y <= 0)
            {
                rb.velocity += Vector3.up * Physics.gravity.y * (fallForce - 1) * Time.fixedDeltaTime;
            }
        }

        public bool CheckBoundaries()
        {
            //if (timeDrop == -1)
            //    return false;

            bool ret = false;

            bool ground = false;
            Vector3 origin = transform.position + transform.forward * 0.5f + new Vector3(0, 0.5f, 0);
            //ground = Physics.Raycast(origin, Vector3.down, raycastDownDistance);

            float right = 0.25f;

            if (!Physics.Raycast(origin, Vector3.down, 1))
                ret = CheckSurfaceBoundary(origin, ground);
            if (!Physics.Raycast(origin + transform.right * right, Vector3.down, 1) && ret == false)
                ret = CheckSurfaceBoundary(origin + transform.right * right, ground);
            if (!Physics.Raycast(origin + transform.right * -right, Vector3.down, 1) && ret == false)
                ret = CheckSurfaceBoundary(origin + transform.right * -right, ground);

            //if (ret == false)
            //    timeDrop = 0;

            if (showDebug)
            {
                Debug.DrawLine(origin, origin + Vector3.down * raycastDownDistance);
                Debug.DrawLine(origin, origin + Vector3.down * 1);
                Debug.DrawLine(origin + transform.right * right, origin + transform.right * right + Vector3.down * 1);
                Debug.DrawLine(origin + transform.right * -right, origin + transform.right * -right + Vector3.down * 1);
            }

            return ret;
        }

        private bool CheckSurfaceBoundary(Vector3 origin, bool ground)
        {
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
                velLimit = Vector3.Dot(velocity.normalized, right); //We get the projection of velocity vector to the tangent

                if (velLimit < 0.4 && velLimit > -0.4)
                {
                    velLimit = 0;
                }

                if (hit1.normal != hit2.normal || hit1.normal != hit3.normal) //These normal checks are used to detect corners and avoid problems
                    velLimit = 0;

                if (velLimit < -0.7)
                    velLimit = -0.7f;
                else if (velLimit > 0.7)
                    velLimit = 0.7f;

                velocity = right * velLimit;

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

        public void SetKinematic(bool active)
        {
            rb.isKinematic = active;
        }

        public void EnableFeetIK()
        {
            enableFeetIK = true;
            lastPelvisPositionY = 0;
            leftFootIKPosition = Vector3.zero;
            rightFootIKPosition = Vector3.zero;
        }
        public void DisableFeetIK()
        {
            enableFeetIK = true;
            lastPelvisPositionY = 0;
            leftFootIKPosition = Vector3.zero;
            rightFootIKPosition = Vector3.zero;
        }

        public void ApplyGravity()
        {
            rb.velocity += Vector3.up * -0.300f;
        }


        #region Auto Step
        private void AutoStep()
        {
            if (controller.inSlope)
                return;

            Vector3 offset = new Vector3(0, 0.01f, 0);
            RaycastHit hit;
            if (controller.characterDetection.ThrowRayOnDirection(transform.position + offset, Vector3.forward, controller.collider.radius + 0.1f, out hit))
            {
                if (!controller.characterDetection.ThrowRayOnDirection(transform.position + offset + new Vector3(0, controller.stepHeight, 0), Vector3.forward, controller.collider.radius + 0.2f, out hit))
                {
                    //rb.position += new Vector3(0, controller.stepVelocity, 0);
                    rb.position += new Vector3(0, controller.stepVelocity, 0) + transform.forward * controller.stepVelocity;
                }
            }
            else if (controller.characterDetection.ThrowRayOnDirection(transform.position + offset, new Vector3(-1.5f, 0, 1), controller.collider.radius + 0.1f, out hit))
            {
                if (!controller.characterDetection.ThrowRayOnDirection(transform.position + offset + new Vector3(0, controller.stepHeight, 0), new Vector3(-1.5f,0,1), controller.collider.radius + 0.2f, out hit))
                {
                    rb.position += new Vector3(0, controller.stepVelocity, 0);
                    rb.position += new Vector3(0, controller.stepVelocity, 0) + transform.forward * controller.stepVelocity;
                }
            }
            else if (controller.characterDetection.ThrowRayOnDirection(transform.position + offset, new Vector3(1.5f, 0, 1), controller.collider.radius + 0.1f, out hit))
            {
                if (!controller.characterDetection.ThrowRayOnDirection(transform.position + offset + new Vector3(0, controller.stepHeight, 0), new Vector3(1.5f, 0, 1), controller.collider.radius + 0.2f, out hit))
                {
                    rb.position += new Vector3(0, controller.stepVelocity, 0);
                    rb.position += new Vector3(0, controller.stepVelocity, 0) + transform.forward * controller.stepVelocity;
                }
            }
        }

        #endregion 
    }

}