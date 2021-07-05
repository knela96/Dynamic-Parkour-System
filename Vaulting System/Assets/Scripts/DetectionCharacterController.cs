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
        public Vector3 OriginFeetRay;
        public float OriginLedgeLength;
        public float OriginFeetLength;
        public LayerMask climbLayer;
        public LayerMask WallLayer;

        public Vector3 LedgePosition;
        public float debugSphereSize = 0.05f;
        bool foundWall = false;
        Vector3 PointFoot1 = Vector3.zero;
        Vector3 PointFoot2 = Vector3.zero;
        Vector3 PointFootFwd = Vector3.zero;

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

        public bool FindFootCollision(Vector3 targetPos, Quaternion rot, Vector3 normal)
        {
            foundWall = true;

            PointFoot1 = targetPos + rot * (new Vector3(-0.15f, -0.10f, 0) + OriginFeetRay);
            PointFoot2 = targetPos + rot * (new Vector3(0.10f, 0, 0) + OriginFeetRay);
            PointFootFwd = -normal;

            RaycastHit hit;
            if (!Physics.Raycast(PointFoot1, -normal, out hit, OriginFeetLength, WallLayer))
            {
                foundWall = false;
            }
            if (!Physics.Raycast(PointFoot2, -normal, out hit, OriginFeetLength, WallLayer))
            {
                foundWall = false;
            }

            return foundWall;
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

            //Draw Feet IK Rays
            Color color = (foundWall == true) ? Color.green : Color.red;
            if (PointFoot1 != Vector3.zero)
                Debug.DrawLine(PointFoot1, PointFoot1 + PointFootFwd * OriginFeetLength, color);
            if (PointFoot2 != Vector3.zero)
                Debug.DrawLine(PointFoot2, PointFoot2 + PointFootFwd * OriginFeetLength, color);
        }

        public bool IsGrounded(out RaycastHit hit) {
            return Physics.Raycast(transform.position, Vector3.down, out hit, 0.2f);
        }

        public void OnTriggerEnterEvent(Collider other)
        {
            if (other.tag.Equals("Ledge"))
            {

            }
        }
    }
}
