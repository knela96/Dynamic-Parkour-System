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
        public float numRays = 5;
        public Vector3 OriginFeetRay;
        public float OriginLedgeLength;
        public float OriginFeetLength;
        public LayerMask ledgeLayer;
        public LayerMask wallLayer;
        public LayerMask climbLayer;

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

        public bool FindLedgeCollision(out RaycastHit hit)
        {
            Vector3 rayOrigin = transform.TransformDirection(OriginLedgeRay) + transform.position;

            for(int i = 0; i < numRays; i++)
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
        public bool FindDropLedgeCollision(out RaycastHit hit)
        {
            for (int i = 0; i < numRays; i++)
            {
                Vector3 origin = transform.position + transform.forward * 0.8f - new Vector3(0, i * 0.15f, 0);

                Debug.DrawLine(origin, transform.position - new Vector3(0, i * 0.15f, 0));

                if(Physics.Raycast(origin, -transform.forward, out hit, 0.8f, ledgeLayer))
                {
                    if (showDebug) //Normal
                    {
                        Debug.DrawLine(hit.point, hit.point + hit.normal, Color.cyan);
                    }

                    if (hit.normal == -hit.transform.forward)
                    {
                        return true;
                    }
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
            if (!Physics.Raycast(PointFoot1, -normal, out hit, OriginFeetLength, wallLayer))
            {
                foundWall = false;
            }
            if (!Physics.Raycast(PointFoot2, -normal, out hit, OriginFeetLength, wallLayer))
            {
                foundWall = false;
            }

            return foundWall;
        }

        public bool ThrowRayToLedge(Vector3 origin, out RaycastHit hit)
        {
            if (showDebug)
            {
                Debug.DrawLine(origin, origin + transform.forward * OriginLedgeLength, Color.green);
            }

            if (Physics.Raycast(origin, transform.forward, out hit, OriginLedgeLength, ledgeLayer))
            {
                if (showDebug) //Normal
                {
                    Debug.DrawLine(hit.point, hit.point + hit.normal, Color.cyan);
                    //transform.rotation = Quaternion.FromToRotation(-transform.forward, hit.normal) * transform.rotation;
                }

                if (hit.normal == hit.transform.forward || hit.normal == -hit.transform.forward)
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
        public bool ThrowClimbRay(Vector3 origin, Vector3 direction, float length, out RaycastHit hit)
        {

            Vector3 origin1 = origin + new Vector3(0, 1.8f, 0);
            Vector3 origin2 = origin + new Vector3(0, 0.5f, 0);

            if (showDebug)
            {
                Debug.DrawLine(origin1, origin1 + direction * length, Color.red);
                Debug.DrawLine(origin2, origin2 + direction * length, Color.red);
            }

            if (!Physics.Raycast(origin1, direction, out hit, length) && !Physics.Raycast(origin2, direction, out hit, length)) //Check Forward
            {
                Vector3 origin3 = origin + direction * 0.15f + new Vector3(0,0.5f,0);

                if (showDebug)
                {
                    Debug.DrawLine(origin3, origin3 - Vector3.up * length, Color.cyan);
                }

                if (Physics.Raycast(origin3, -Vector3.up, out hit, length, climbLayer))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ThrowHandRayToLedge(Vector3 origin, Vector3 direction, float length, out RaycastHit hit)
        {
            if (showDebug)
            {
                Debug.DrawLine(origin, origin + transform.TransformDirection(direction) * length, Color.green);
            }

            return Physics.Raycast(origin, transform.TransformDirection(direction), out hit, length, ledgeLayer);
        }
        public bool ThrowFootRayToLedge(Vector3 origin, Vector3 direction, float length, out RaycastHit hit)
        {
            if (showDebug)
            {
                Debug.DrawLine(origin, origin + transform.TransformDirection(direction) * length, Color.green);
            }

            return Physics.Raycast(origin, transform.TransformDirection(direction), out hit, length, wallLayer);
        }

        public bool ThrowRayOnDirection(Vector3 origin, Vector3 direction, float length, out RaycastHit hit)
        {
            if (showDebug)
            {
                Debug.DrawLine(origin, origin + transform.TransformDirection(direction) * length, Color.green);
            }

            return Physics.Raycast(origin, transform.TransformDirection(direction), out hit, length);
        }

        public bool IsGrounded(float stepHeight) {
            if (showDebug)
            {
                Debug.DrawLine(transform.position + new Vector3(0, 0.5f, 0), transform.position + new Vector3(0, 0.5f, 0) + Vector3.down * 0.8f, Color.green);
            }
            RaycastHit hit;
            return Physics.Raycast(transform.position + new Vector3(0, 0.3f, 0), Vector3.down, out hit, 0.7f);//0.2f
        }

        public void FindAheadPoints(ref List<HandlePointsV2> list)
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, 5, climbLayer.value);

            foreach (var item in cols)
            {
                if (Vector3.Dot(item.transform.position, transform.position) > 0)
                {
                    HandlePointsV2 handle = item.GetComponentInChildren<HandlePointsV2>();
                    if (handle)
                        list.Add(handle);
                }
            }
        }
    }
}
