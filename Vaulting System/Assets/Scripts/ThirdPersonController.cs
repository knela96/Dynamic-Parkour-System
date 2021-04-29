using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    public MovementCharacterController characterMovement;
    public AnimationCharacterController characterAnimation;
    public DetectionCharacterController characterDetection;
    public JumpPredictionController jumpPrediction;

    public Transform cam;
    public Transform Transform_Mesh;
    private float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    public Transform camReference;
    int counter = 0;
    public bool isGrounded = false;
    public bool inAir = false;
    public bool dummy = false;
    bool ledgeFound = false;
    bool toTarget = false;

    private void Start()
    {
        //camReference = new GameObject("Camera Aux").transform;
        characterMovement.OnLanded += characterAnimation.Land;
        characterMovement.OnFall += characterAnimation.Fall;
    }

    // Update is called once per frame
    void Update()
    {
        //Player is falling
        isGrounded = (characterDetection.IsGrounded() && characterMovement.GetVelocity().y <= 0) ? true : false;

        if (!isGrounded && characterMovement.GetVelocity().y < 0)
        {
            inAir = true;
        }

        if (!isGrounded)
        {
            RaycastHit hit;
            ledgeFound = characterDetection.LedgeCollision();

            if (ledgeFound && !dummy)
            {
                DisableController();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            toTarget = jumpPrediction.SetParabola(transform, GameObject.Find("Target").transform);

            if (toTarget)
            {
                DisableController();
                characterAnimation.Jump();
            }
        }

        if (!jumpPrediction.hasArrived() && toTarget)
        {
            jumpPrediction.FollowParabola();
        }
        else if (jumpPrediction.hasArrived() && toTarget)
        {
            characterAnimation.Land();
            toTarget = false;
        }

        //Dismount
        if (Input.GetKeyDown(KeyCode.C))
            EnableController();
    }

    public void AddMovementInput(float vertical, float horizontal)
    {
        if (dummy)
            return;

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
        characterMovement.SetVelocity(Vector3.ClampMagnitude(translation, 1.0f));

    }

    public void Jump()
    {
        if (isGrounded == true && !inAir)
        {
            characterMovement.Jump();
            characterAnimation.Jump();
        }
    }

    public void ResetMovement()
    {
        characterMovement.ResetSpeed();
        characterAnimation.animator.applyRootMotion = true;
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
        GetComponent<Collider>().isTrigger = true;
        dummy = true;
    }
    public void EnableController()
    {
        characterMovement.SetKinematic(false);
        GetComponent<Collider>().isTrigger = false;
        dummy = false;
    }
}