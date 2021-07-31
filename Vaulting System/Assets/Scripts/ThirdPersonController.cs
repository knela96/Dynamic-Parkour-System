using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class ThirdPersonController : MonoBehaviour
    {
        public InputCharacterController characterInput;
        public MovementCharacterController characterMovement;
        public AnimationCharacterController characterAnimation;
        public DetectionCharacterController characterDetection;
        public JumpPredictionController jumpPrediction;
        public CameraController cameraController;

        public Transform cam;
        public Transform Transform_Mesh;
        private float turnSmoothTime = 0.1f;
        float turnSmoothVelocity;
        public Transform camReference;
        public bool moving = false;
        public bool isGrounded = false;
        public bool isJumping = false;
        public bool inSlope = false;
        public bool dummy = false;
        bool toTarget = false;
        Collider collider;

        private void Start()
        {
            //camReference = new GameObject("Camera Aux").transform;
            characterMovement.OnLanded += characterAnimation.Land;
            characterMovement.OnFall += characterAnimation.Fall;
            collider = GetComponent<Collider>();
        }

        // Update is called once per frame
        void Update()
        {
            //Player is falling
            isGrounded = OnGround();

            if (!dummy && !characterAnimation.RootMotion())
            {
                AddMovementInput(characterInput.movement);

                if (characterInput.run)
                {
                    ToggleRun();
                }
                else
                {
                    ToggleWalk();
                }
            }
        }

        private bool OnGround()
        {
            RaycastHit hit;
            if (characterDetection.IsGrounded(out hit))
            {
                return true;
            }
            return false;
        }

        public void AddMovementInput(Vector2 direction)
        {
            Vector3 translation = Vector3.zero;

            translation = GroundMovement(direction);

            characterMovement.SetVelocity(Vector3.ClampMagnitude(translation, 1.0f));
        }

        Vector3 GroundMovement(Vector2 input)
        {
            Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

            camReference.eulerAngles = new Vector3(0, cam.eulerAngles.y, 0);
            Vector3 translation = camReference.transform.forward * input.y + camReference.transform.right * input.x;
            translation.y = 0;

            if (translation.magnitude > 0)
            {
                RotatePlayer(direction);

                //Move Player to camera directin
                characterAnimation.animator.SetBool("Released", false);
                moving = true;
            }
            else
            {
                characterAnimation.animator.SetBool("Released", true);
                moving = false;

                ToggleWalk();
            }

            return translation;
        }

        public void RotatePlayer(Vector3 direction)
        {
            //Get direction with camera rotation
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            //Rotate Mesh to Movement
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        public void Jump()
        {
            if (isGrounded)
            {
                characterMovement.Jump();
                characterAnimation.Jump();
            }
        }

        public void ResetMovement()
        {
            characterMovement.ResetSpeed();
            //characterAnimation.animator.applyRootMotion = true;
        }

        public void ToggleRun()
        {
            if (characterMovement.GetState() != MovementState.Running)
            {
                characterMovement.SetCurrentState(MovementState.Running);
                characterMovement.speed = characterMovement.RunSpeed;
                characterAnimation.animator.SetBool("Run", true);
            }
        }
        public void ToggleWalk()
        {
            if (characterMovement.GetState() != MovementState.Walking)
            {
                characterMovement.SetCurrentState(MovementState.Walking);
                characterMovement.speed = characterMovement.walkSpeed;
                characterAnimation.animator.SetBool("Run", false);
                characterInput.run = false;
            }
        }


        public float GetCurrentVelocity()
        {
            return characterMovement.GetVelocity().magnitude;
        }

        public void DisableController()
        {
            characterAnimation.SetAnimVelocity(Vector3.zero);
            characterMovement.SetKinematic(true);
            //characterMovement.SetVelocity(Vector3.zero);
            characterMovement.enableFeetIK = false;
            collider.isTrigger = true;
            dummy = true;
        }
        public void EnableController()
        {
            characterMovement.SetKinematic(false);
            characterMovement.ApplyGravity();
            collider.isTrigger = false;
            dummy = false;
            toTarget = false;
        }
    }
}