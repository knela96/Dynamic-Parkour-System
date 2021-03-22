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
    public float speed = 6f;
    private float smoothSpeed;


    //public CharacterController charactercontroller;

    MovementState currentState;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        rb = GetComponent<Rigidbody>();
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
            //Lerp with current velocity with Axis
            rb.velocity = new Vector3(velocity.x * smoothSpeed, rb.velocity.y, velocity.z * smoothSpeed);
            smoothSpeed = Mathf.Lerp(smoothSpeed, speed, Time.deltaTime*4);
        }
        else
        {
            //Lerp with current velocity of the rigidbody
            rb.velocity = new Vector3(Velocity.normalized.x * smoothSpeed, rb.velocity.y, Velocity.normalized.z * smoothSpeed);
            smoothSpeed = Mathf.Lerp(smoothSpeed, 0, Time.deltaTime*4);
        }
    }

    public Vector3 Velocity { get => rb.velocity; set => velocity = value; }
    
    public void SetCurrentState(MovementState state) 
    {
        currentState = state;

        switch (currentState)
        {
            case MovementState.Walking:
                speed = 6f;
                break;
            case MovementState.Running:
                speed = 9f;
                break;
        }
    }

    public MovementState GetState()
    {
        return currentState;
    }
}
