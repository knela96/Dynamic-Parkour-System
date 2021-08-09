using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{

    [RequireComponent(typeof(ThirdPersonController))]
    public class DetectionCharacterController : MonoBehaviour
    {
        public bool showDebug = true;

        [Header("Layers")]
        public LayerMask ledgeLayer;
        public LayerMask wallLayer;
        public LayerMask climbLayer;
        public LayerMask environmentLayer;

        [Header("Rays")]
        [SerializeField] private Vector3 OriginLedgeRay;
        [SerializeField] private Vector3 OriginFeetRay;
        [SerializeField] private float LedgeRayLength = 1.5f;
        [SerializeField] private float FeetRayLength = 0.6f;
        [SerializeField] private float FindLedgeNumRays = 7;
        [SerializeField] private float DropLedgeNumRays = 8;

        public bool FindLedgeCollision(out RaycastHit hit)
        {
            Vector3 rayOrigin = transform.TransformDirection(OriginLedgeRay) + transform.position;

            for(int i = 0; i < FindLedgeNumRays; i++)
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
            for (int i = 0; i < DropLedgeNumRays; i++)
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
            bool foundWall = true;

            Vector3 PointFoot1 = targetPos + rot * (new Vector3(-0.15f, -0.10f, 0) + OriginFeetRay);
            Vector3 PointFoot2 = targetPos + rot * (new Vector3(0.10f, 0, 0) + OriginFeetRay);

            RaycastHit hit;
            if (!Physics.Raycast(PointFoot1, -normal, out hit, FeetRayLength, wallLayer))
            {
                foundWall = false;
            }
            if (!Physics.Raycast(PointFoot2, -normal, out hit, FeetRayLength, wallLayer))
            {
                foundWall = false;
            }

            return foundWall;
        }

        public bool ThrowRayToLedge(Vector3 origin, out RaycastHit hit)
        {
            if (showDebug)
            {
                Debug.DrawLine(origin, origin + transform.forward * LedgeRayLength, Color.green);
            }

            if (Physics.Raycast(origin, transform.forward, out hit, LedgeRayLength, ledgeLayer))
            {
                if (showDebug) //Normal
                {
                    Debug.DrawLine(hit.point, hit.point + hit.normal, Color.cyan);
                }

                if (hit.normal == hit.transform.forward || hit.normal == -hit.transform.forward)
                    return true;

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
