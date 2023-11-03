/*
MIT License

Copyright (c) 2023 Èric Canela
Contact: knela96@gmail.com or @knela96 twitter

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (Dynamic Parkour System), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public enum MovementState { Walking, Running }

    [RequireComponent(typeof(ThirdPersonController))]
    public class MovementCharacterController : MonoBehaviour
    {
        public bool showDebug = true;

        [HideInInspector] public Rigidbody rb;
        [HideInInspector] public bool limitMovement = false;
        [HideInInspector] public bool stopMotion = false;
        [HideInInspector] public float velLimit = 0;
        [HideInInspector] public float curSpeed = 6f;

        private ThirdPersonController controller;
        private MovementState currentState;
        private Animator anim;
        private Vector3 velocity;
        private float smoothSpeed;

        public delegate void OnLandedDelegate();
        public delegate void OnFallDelegate();
        public event OnLandedDelegate OnLanded;
        public event OnFallDelegate OnFall;

        [Header("Movement Settings")]
        public float walkSpeed;
        public float JogSpeed;
        public float RunSpeed;
        public float fallForce;

        [Header("Feet IK")]
        public bool enableFeetIK = true;

        [SerializeField] private float heightFromGroundRaycast = 0.7f;
        [Range(0, 2f)] [SerializeField] private float raycastDownDistance = 1.5f;
        [SerializeField] private float pelvisOffset = 0f;

        [Range(0, 1f)] [SerializeField] private float pelvisUpDownSpeed = 0.25f;
        [Range(0, 1f)] [SerializeField] private float feetToIKPositionSpeed = 0.25f;

        public string leftFootAnim = "LeftFootCurve";
        public string rightFootAnim = "RightFootCurve";

        private Vector3 leftFootPosition, leftFootIKPosition, rightFootPosition, rightFootIKPosition;
        private Quaternion leftFootIKRotation, rightFootIKRotation;
        private float lastPelvisPositionY, lastLeftFootPosition, lastRightFootPosition;

        void Start()
        {
            controller = GetComponent<ThirdPersonController>();
            rb = GetComponent<Rigidbody>();
            anim = controller.characterAnimation.animator;
            SetCurrentState(MovementState.Walking);
        }

        void Update()
        {
            //Handle Player Jumps and Landings
            if (controller.isJumping)
            {
                controller.allowMovement = true;

                if (!controller.isGrounded)
                {
                    Fall();
                }
                else if (controller.isGrounded && controller.onAir)
                {
                    Landed();
                }
            }
        }

        private void FixedUpdate()
        {
            //Limits player movement to avoid falling
            if (controller.isGrounded)
            {
                limitMovement = CheckBoundaries();
            }

            //Apply Player Movement
            if (!controller.dummy)
            {
                if (!stopMotion && !controller.characterAnimation.animState.IsName("Fall"))
                {
                    ApplyInputMovement();
                }
            }

            //Grant movement while falling
            if (!controller.dummy && controller.isJumping && controller.characterInput.movement != Vector2.zero && !controller.isVaulting)
            {
                rb.position += (transform.forward * walkSpeed) * Time.fixedDeltaTime;
            }

            //IK Positioning
            if (!enableFeetIK || controller.dummy)
                return;
            if (anim == null)
                return;

            //Get IK Positions
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
                //Applies Input Movement to the RigidBody
                smoothSpeed = Mathf.Lerp(smoothSpeed, curSpeed, Time.fixedDeltaTime * 2);
                rb.velocity = new Vector3(velocity.x * smoothSpeed, velocity.y * smoothSpeed + rb.velocity.y, velocity.z * smoothSpeed);

                //Detect Player on Irregular Surface and adjust movement to avoid slowing down and undesired jumps
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

                //If player fins Small Obstacle Auto Steps it without affecting the movement
                AutoStep();

                //Sets velocity for movement animations
                controller.characterAnimation.SetAnimVelocity(rb.velocity);
            }
            else
            {
                //Lerp down with current velocity of the rigidbody when no input detected
                smoothSpeed = Mathf.SmoothStep(smoothSpeed, 0, Time.fixedDeltaTime * 20);
                rb.velocity = new Vector3(rb.velocity.normalized.x * smoothSpeed, rb.velocity.y, rb.velocity.normalized.z * smoothSpeed);
                controller.characterAnimation.SetAnimVelocity(controller.characterAnimation.GetAnimVelocity().normalized * smoothSpeed);
            }

            //Apply fall multiplier as gravity
            if (rb.velocity.y <= 0)
            {
                rb.velocity += Vector3.up * Physics.gravity.y * (fallForce - 1) * Time.fixedDeltaTime;
            }
        }

        /// <summary>
        /// Avoids the Player from falling unintentionally
        /// </summary>
        public bool CheckBoundaries()
        {
            bool ret = false;

            Vector3 origin = transform.position + transform.forward * 0.5f + new Vector3(0, 0.5f, 0);

            float right = 0.25f;

            //Throws raycasts down to detect limits of surface
            if (!controller.characterDetection.ThrowRayOnDirection(origin, Vector3.down, 1))
                ret = CheckSurfaceBoundary();
            else if (!controller.characterDetection.ThrowRayOnDirection(origin + transform.right * right, Vector3.down, 1) && ret == false)
                ret = CheckSurfaceBoundary();
            else if (!controller.characterDetection.ThrowRayOnDirection(origin + transform.right * -right, Vector3.down, 1) && ret == false)
                ret = CheckSurfaceBoundary();

            if (showDebug)
            {
                Debug.DrawLine(origin, origin + Vector3.down * raycastDownDistance);
                Debug.DrawLine(origin, origin + Vector3.down * 1);
                Debug.DrawLine(origin + transform.right * right, origin + transform.right * right + Vector3.down * 1);
                Debug.DrawLine(origin + transform.right * -right, origin + transform.right * -right + Vector3.down * 1);
            }

            return ret;
        }

        /// <summary>
        /// Calculates the velocity necessary to avoid falling from a surface
        /// </summary>
        private bool CheckSurfaceBoundary()
        {
            Vector3 origin2 = transform.position + transform.forward * 0.8f + new Vector3(0, -0.05f, 0);

            if (showDebug)
                Debug.DrawLine(origin2, transform.position + new Vector3(0, -0.05f, 0));

            //Throws a raycast towards the player from a lower position to the surface the player is on
            RaycastHit hit1;
            if (controller.characterDetection.ThrowRayOnDirection(origin2, -transform.forward, 1, out hit1))
            {
                if (showDebug)
                    Debug.DrawLine(hit1.point, hit1.point + hit1.normal, Color.cyan);

                //Throws two more raycasts to detect corners
                RaycastHit hit2;
                RaycastHit hit3;
                controller.characterDetection.ThrowRayOnDirection(origin2 + transform.right * 0.05f, -transform.forward, 1, out hit2);
                controller.characterDetection.ThrowRayOnDirection(origin2 + transform.right * -0.05f, -transform.forward, 1, out hit3);

                if (hit2.normal == Vector3.zero)
                    hit2.normal = hit1.normal;

                if (hit3.normal == Vector3.zero)
                    hit3.normal = hit1.normal;

                Vector3 right = Vector3.Cross(Vector3.up, hit1.normal); //Get tangent of current surface (Right Vector)
                velLimit = Vector3.Dot(velocity.normalized, right); //We get the projection of velocity vector to the tangent

                if (hit1.normal != hit2.normal || hit1.normal != hit3.normal) //These normal checks are used to detect corners and stop movement
                    velLimit = 0;

                if (velLimit < 0.4 && velLimit > -0.4) //VelLimit direction is almost forward to the normal of the plane, stop movement
                {
                    velLimit = 0;
                }

                //Clamp velocity
                if (velLimit < -0.7)
                    velLimit = -0.7f;
                else if (velLimit > 0.7)
                    velLimit = 0.7f;

                //Calculate new velocity
                velocity = right * velLimit;

                return true;
            }

            return false;
        }

        public Vector3 GetVelocity() { 
            return rb.velocity; 
        }

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
                    curSpeed = walkSpeed;
                    break;
                case MovementState.Running:
                    curSpeed = RunSpeed;
                    smoothSpeed = curSpeed;
                    break;
            }
        }

        public MovementState GetState()
        {
            return currentState;
        }

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

        public void Fall()
        {
            controller.onAir = true;
            OnFall();
        }

        public void Landed()
        {
            OnLanded();
            controller.isJumping = false;
            controller.onAir = false;
        }

        #endregion

        #region Foot IK

        private void OnAnimatorIK(int layerIndex)
        {
            if (!enableFeetIK || controller.dummy || anim == null)
                return;

            MovePelvisHeight();

            //Left Foot IK Position and Rotation
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, anim.GetFloat(leftFootAnim));
            MoveFeetToIKPoint(AvatarIKGoal.LeftFoot, leftFootIKPosition, leftFootIKRotation, ref lastLeftFootPosition);

            //Right Foot IK Position and Rotation
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, anim.GetFloat(rightFootAnim));
            MoveFeetToIKPoint(AvatarIKGoal.RightFoot, rightFootIKPosition, rightFootIKRotation, ref lastRightFootPosition);
        }

        void MoveFeetToIKPoint(AvatarIKGoal foot, Vector3 positionIKHolder, Quaternion rotationHolder, ref float lastFootPositionY)
        {
            Vector3 targetIKPosition = anim.GetIKPosition(foot);

            if (positionIKHolder != Vector3.zero)
            {
                targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
                positionIKHolder = transform.InverseTransformPoint(positionIKHolder);

                float yVariable = Mathf.Lerp(lastFootPositionY, positionIKHolder.y, feetToIKPositionSpeed);
                lastFootPositionY = yVariable;
                targetIKPosition.y += yVariable;
                targetIKPosition = transform.TransformPoint(targetIKPosition);
            }

            anim.SetIKRotation(foot, rotationHolder);
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
            newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpDownSpeed);
            anim.bodyPosition = newPelvisPosition;

            lastPelvisPositionY = anim.bodyPosition.y;
        }

        //Raycast handler to find feet position on ground
        private void FeetPositionSolver(Vector3 fromRaycastPosition, ref Vector3 feetIKPositions, ref Quaternion feetIKRotations)
        {
            RaycastHit feetHit;

            if (showDebug)
                Debug.DrawLine(fromRaycastPosition, fromRaycastPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast), Color.green);

            if (controller.characterDetection.ThrowRayOnDirection(fromRaycastPosition, Vector3.down, raycastDownDistance + heightFromGroundRaycast, out feetHit))
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

        #region Auto Step

        private void AutoStep()
        {
            if (controller.inSlope)
                return;

            Vector3 offset = new Vector3(0, 0.01f, 0);
            RaycastHit hit;
            if (controller.characterDetection.ThrowRayOnDirection(transform.position + offset, transform.forward, controller.slidingCapsuleCollider.radius + 0.1f, out hit))
            {
                if (!controller.characterDetection.ThrowRayOnDirection(transform.position + offset + new Vector3(0, controller.stepHeight, 0), transform.forward, controller.slidingCapsuleCollider.radius + 0.2f, out hit))
                {
                    rb.position += new Vector3(0, controller.stepVelocity, 0);
                }
            }
            else if (controller.characterDetection.ThrowRayOnDirection(transform.position + offset, transform.TransformDirection(new Vector3(-1.5f, 0, 1)), controller.slidingCapsuleCollider.radius + 0.1f, out hit))
            {
                if (!controller.characterDetection.ThrowRayOnDirection(transform.position + offset + new Vector3(0, controller.stepHeight, 0), transform.TransformDirection(new Vector3(-1.5f,0,1)), controller.slidingCapsuleCollider.radius + 0.2f, out hit))
                {
                    rb.position += new Vector3(0, controller.stepVelocity, 0);
                }
            }
            else if (controller.characterDetection.ThrowRayOnDirection(transform.position + offset, transform.TransformDirection(new Vector3(1.5f, 0, 1)), controller.slidingCapsuleCollider.radius + 0.1f, out hit))
            {
                if (!controller.characterDetection.ThrowRayOnDirection(transform.position + offset + new Vector3(0, controller.stepHeight, 0), transform.TransformDirection(new Vector3(1.5f, 0, 1)), controller.slidingCapsuleCollider.radius + 0.2f, out hit))
                {
                    rb.position += new Vector3(0, controller.stepVelocity, 0);
                }
            }
        }

        #endregion 
    }

}