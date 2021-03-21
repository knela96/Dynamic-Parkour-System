using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    public MovementCharacterController characterMovement;
    

    // Update is called once per frame
    void Update()
    {
    }

    public float GetCurrentVelocity()
    {
        return characterMovement.Velocity.magnitude;
    }
}
