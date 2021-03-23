using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementState { Walking, Running }

[RequireComponent(typeof(ThirdPersonController))]
public class MovementCharacterController : MonoBehaviour
{
    private ThirdPersonController controller;
    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 animVelocity;
    public float speed = 6f;
    private float smoothSpeed;

    public float walkSpeed;
    public float JogSpeed;
    public float RunSpeed;


    //public CharacterController charactercontroller;

    MovementState currentState;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        rb = GetComponent<Rigidbody>();
        SetCurrentState(MovementState.Walking);
    }

    // Update is called once per frame
    void Update()
    {
        //charactercontroller.Move(Velocity.normalized * speed * Time.deltaTime);

        if (GetState() == MovementState.Running)
        {
            velocity.Normalize();
        }

        if (velocity.magnitude > 0)
        {
            smoothSpeed = Mathf.Lerp(smoothSpeed, speed, Time.deltaTime * 2);
            rb.velocity = new Vector3(velocity.x * smoothSpeed, rb.velocity.y, velocity.z * smoothSpeed);
            UpdateAnimVelocity(rb.velocity);
        }
        else
        {
            //Lerp with current velocity of the rigidbody
            smoothSpeed = Mathf.Lerp(smoothSpeed, 0, Time.deltaTime * 6);
            rb.velocity = new Vector3(GetVelocity().normalized.x * smoothSpeed, rb.velocity.y, GetVelocity().normalized.z * smoothSpeed);
            UpdateAnimVelocity(GetAnimVelocity().normalized * smoothSpeed);
        }
    }

    public Vector3 GetVelocity() { return rb.velocity; }
    public Vector3 GetAnimVelocity() { return animVelocity; }
    public void SetVelocity(Vector3 value) { velocity = value; }
    public void UpdateAnimVelocity(Vector3 value) { animVelocity = value; }

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
}
