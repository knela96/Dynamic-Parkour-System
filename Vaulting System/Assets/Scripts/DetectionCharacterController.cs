using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{

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

        public bool FindLedgeCollision(out RaycastHit hit)
        {
            Vector3 rayOrigin = transform.TransformDirection(OriginLedgeRay) + transform.position;

            for(int i = 0; i < 5; i++)
            {
                bool ret = ThrowRayToLedge(rayOrigin + new Vector3(0, 0.15f * i, 0), out hit);

                if (ret)
                {
                    return true;
                }
            }

            //Set invalid hit
            Physics.Raycast(Vector3.zero, Vector3.forward, out hit, 0, -1);
            return false;
        }

        bool ThrowRayToLedge(Vector3 origin, out RaycastHit hit)
        {
            if (showDebug)
            {
                Debug.DrawLine(origin, origin + transform.forward * OriginLedgeLength, Color.green);
            }

            if (Physics.Raycast(origin, transform.forward, out hit, OriginLedgeLength, climbLayer))
            {
                if (showDebug) //Normal
                {
                    Debug.DrawLine(hit.point, hit.point + hit.normal, Color.cyan);
                    //transform.rotation = Quaternion.FromToRotation(-transform.forward, hit.normal) * transform.rotation;
                }

                if(hit.normal == hit.transform.forward || hit.normal == -hit.transform.forward)
                    return true;

                //Vector3 rayOrigin2 = hit.point;
                //rayOrigin2.y += 1.0f;
                //
                //if (Physics.Raycast(rayOrigin2, Vector3.down, out hit, OriginLedgeLength, climbLayer))
                //{
                //    if (showDebug)
                //    {
                //        Debug.DrawLine(rayOrigin2, rayOrigin2 + Vector3.down * (rayOrigin2.y - hit.point.y), Color.green);
                //    }
                //
                //    return true;
                //}
            }
            return false;

        }

        public bool ThrowHandRayToLedge(Vector3 origin, out RaycastHit hit)
        {
            if (showDebug)
            {
                Debug.DrawLine(origin, origin + transform.forward * 0.25f, Color.green);
            }

            return Physics.Raycast(origin, transform.forward, out hit, 0.25f, climbLayer);
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
            return Physics.Linecast(transform.position, transform.position + new Vector3(0, -0.1f, 0));
        }

        public void OnTriggerEnterEvent(Collider other)
        {
            if (other.tag.Equals("Ledge"))
            {

            }
        }
    }
}
