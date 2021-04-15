using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonController))]
public class DetectionCharacterController : MonoBehaviour
{
    // Start is called before the first frame update
    CapsuleCollider collider;
    void Start()
    {
        collider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public bool IsGrounded() {
        return Physics.Linecast(transform.position, transform.position + new Vector3(0,-0.3f,0));
    }
}
