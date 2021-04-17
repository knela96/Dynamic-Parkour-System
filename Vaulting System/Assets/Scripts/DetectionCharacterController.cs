using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonController))]
public class DetectionCharacterController : MonoBehaviour
{
    // Start is called before the first frame update
    CapsuleCollider collider;
    public bool showDebug = true;

    public Vector3 OriginLedgeRay;
    public float OriginLedgeLength;
    public LayerMask climbLayer;

    public Vector3 LedgePosition;
    public float debugSphereSize = 0.05f;

    void Start()
    {
        collider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool LedgeCollision(Vector3 position)
    {
        RaycastHit feetHit;

        if (Physics.Raycast(position + OriginLedgeRay, Vector3.down, out feetHit, OriginLedgeLength, climbLayer))
        {
            if (showDebug)
                Debug.DrawLine(position + OriginLedgeRay, position + OriginLedgeRay + Vector3.down * OriginLedgeLength, Color.green);
            position.y = feetHit.point.y;

            if (Physics.Raycast(position, Vector3.forward, out feetHit, OriginLedgeLength, climbLayer))
            {
                if (showDebug)
                    Debug.DrawLine(position, position + Vector3.forward * OriginLedgeLength, Color.green);

                LedgePosition = feetHit.point;
                return true;
            }
            else
            {
                if (showDebug)
                    Debug.DrawLine(position, position + Vector3.forward * OriginLedgeLength, Color.red);
                LedgePosition = Vector3.zero;
            }


        }

        if (showDebug)
            Debug.DrawLine(position + OriginLedgeRay, position + OriginLedgeRay + Vector3.down * OriginLedgeLength, Color.red);
        return false;
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        if (LedgePosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(LedgePosition, debugSphereSize);
        }
    }

    public bool IsGrounded() {
        return Physics.Linecast(transform.position, transform.position + new Vector3(0,-0.3f,0));
    }

    public void OnTriggerEnterEvent(Collider other)
    {
        if (other.tag.Equals("Ledge"))
        {

        }
    }
}
