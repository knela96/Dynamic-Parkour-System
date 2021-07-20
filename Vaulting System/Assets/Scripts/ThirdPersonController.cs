using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Climbing;

public class ThirdPersonController : MonoBehaviour
{
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
    int counter = 0;
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
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    toTarget = jumpPrediction.SetParabola(transform, GameObject.Find("Target").transform);
        //
        //    if (toTarget)
        //    {
        //        DisableController();
        //        characterAnimation.Jump();
        //    }
        //}

        //if (!jumpPrediction.hasArrived() && toTarget)
        //{
        //    jumpPrediction.FollowParabola();
        //}
        //else if (jumpPrediction.hasArrived() && toTarget)
        //{
        //    characterAnimation.Land();
        //    EnableController();
        //    toTarget = false;
        //}

        if (!dummy && !characterAnimation.RootMotion())
        {
            AddMovementInput(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKey(KeyCode.Joystick1Button10))
            {
                ToggleRun();
            }
        }

        //Player is falling
        isGrounded = OnGround();
    }

    private bool OnGround()
    {
        RaycastHit hit;
        if (characterDetection.IsGrounded(out hit))
        {
            if (isJumping)
                return false;

            if (hit.normal != Vector3.up)
            {
                inSlope = true;
            }
            else
            {
                inSlope = false;
            }
            return true;
        }

        return false;
    }

    public void AddMovementInput(float vertical, float horizontal)
    {
        Vector3 translation = Vector3.zero;

        translation = GroundMovement(vertical, horizontal);
        characterMovement.SetVelocity(Vector3.ClampMagnitude(translation, 1.0f));
    }

    Vector3 GroundMovement(float vertical, float horizontal)
    {
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        camReference.eulerAngles = new Vector3(0, cam.eulerAngles.y, 0);
        Vector3 translation = camReference.transform.forward * vertical + camReference.transform.right * horizontal;
        translation.y = 0;

        if (translation.magnitude > 0)
        {
            //Get direction with camera rotation
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            //Rotate Mesh to Movement
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            //Move Player to camera directin
            characterAnimation.animator.SetBool("Released", false);
        }
        else
        {
            characterAnimation.animator.SetBool("Released", true);

            //Reset Sprint to Walk Velocity
            if (characterMovement.GetState() == MovementState.Running)
            {
                characterMovement.SetCurrentState(MovementState.Walking);
                ResetMovement();
            }
        }

        return translation;
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
            //characterAnimation.SetRootMotion(true);
            //characterMovement.ResetSpeed();
        }
    }


    public float GetCurrentVelocity()
    {
        return characterMovement.GetVelocity().magnitude;
    }

    public void DisableController()
    {
        characterMovement.SetKinematic(true);
        characterMovement.SetVelocity(Vector3.zero);
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