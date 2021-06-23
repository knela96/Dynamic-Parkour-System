using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Climbing;

namespace Climbing
{
    public class ClimbController : MonoBehaviour
    {
        bool ledgeFound = false;
        public bool onLedge = false;
        public bool toLedge = false;

        public DetectionCharacterController characterDetection;
        public ThirdPersonController characterController;
        public HandlePointConnection pointConnection;
        private AnimationCharacterController characterAnimation;
        public float rootOffset;
        Vector3 target = Vector3.zero;
        Quaternion targetRot = Quaternion.identity;
        public float lateralSpeed = 25f;

        public GameObject limitLHand;
        public GameObject limitRHand;
        bool dropping = false;

        GameObject curLedge;

        Point targetPoint = null;
        Point currentPoint = null;

        bool debug = false;
        public enum ClimbState { ToHang, Hanging, Dropping };
        private ClimbState curClimbState = ClimbState.ToHang;

        // Start is called before the first frame update
        void Start()
        {
            curLedge = null;
            characterAnimation = characterController.characterAnimation;
        }

        private void OnDrawGizmos()
        {
            if (targetPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(targetPoint.transform.position, 0.1f);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentPoint.transform.position, 0.1f);
            }
        }

        // Update is called once per frame
        void Update()
        {
            //Arrived on Ledge
            if (onLedge)
            {
                ClimbMovement(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal")); //Movement on Ledge

                if (Input.GetKeyDown(KeyCode.C))
                {
                    curLedge = null;
                    targetPoint = null;
                    currentPoint = null;
                    curClimbState = ClimbState.Dropping;
                    characterAnimation.DropLedge();
                }
                if (characterAnimation.animator.GetInteger("Climb State") == (int)ClimbState.Dropping && characterAnimation.animator.IsInTransition(0))
                {
                    characterController.EnableController();
                }
            }

            //Groud
            if (!characterController.dummy)
            {
                onLedge = false;
                RaycastHit hit;
                if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button1)) && !toLedge && !onLedge)
                {
                    ledgeFound = characterDetection.FindLedgeCollision(out hit);

                    if (ledgeFound)
                    {
                        target = ReachLedge(hit);
                        targetRot = Quaternion.LookRotation(-hit.normal);
                        transform.position = new Vector3(target.x, transform.position.y, target.z) + (hit.normal * 0.3f);
                        //characterController.jumpPrediction.SetParabola(transform.position, target - new Vector3(0, rootOffset, 0)); //target - new Vector3(0, rootOffset, 0);
                    }
                    else
                    {
                        target = Vector3.zero;
                        targetRot = Quaternion.identity;
                    }
                }
            }

            //Jump to Point
            if (toLedge)
            {
                //characterController.jumpPrediction.FollowParabola(2.0f);
                //transform.position = target - new Vector3(0, rootOffset, 0);

                //if (characterController.jumpPrediction.hasArrived())
                if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle To Braced Hang") && characterAnimation.animator.IsInTransition(0))
                {
                    onLedge = true;
                    toLedge = false;
                }
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 0.5f);
            }
        }

        public void ClimbMovement(float vertical, float horizontal)
        {
            Vector3 translation = transform.right * horizontal * (lateralSpeed * 0.001f);
            bool valid = CheckValidMovement(translation);

            if (valid)
            {
                transform.position += translation;
            }
            /*
            else if(!valid && Input.GetKeyDown(KeyCode.Space)) //Check for Near Ledge
            {
                Point point = null;            

                point = curLedge.GetComponentInChildren<HandlePoints>().GetClosestPoint(transform.position);
                currentPoint = point;

                if (point)
                {
                    Vector3 direction = new Vector3(horizontal, vertical, 0f).normalized;

                    Neighbour toPoint = CandidatePointOnDirection(direction, point, point.neighbours);

                    if (toPoint != null)
                    {
                        if (toPoint.type == ConnectionType.direct) //Jump Reachable
                        {
                            target = toPoint.target.transform.position;
                            curLedge = toPoint.target.transform.parent.parent.parent.gameObject;
                            targetPoint = toPoint.target;

                            if (toPoint.target == curLedge.GetComponentInChildren<HandlePoints>().furthestLeft)//Left Point
                            {
                                target.x += 0.5f;
                            }
                            else if (toPoint.target == curLedge.GetComponentInChildren<HandlePoints>().furthestRight)//Right Point
                            {
                                target.x -= 0.5f;
                            }
                            //transform.position = target - new Vector3(0, rootOffset, 0);
                            characterController.jumpPrediction.SetParabola(transform.position, target - new Vector3(0, rootOffset, 0)); //target - new Vector3(0, rootOffset, 0);
                            toLedge = true;
                        }
                    }
                }
            }
            */
        }

        public Neighbour CandidatePointOnDirection(Vector3 targetDirection, Point from, List<Neighbour> candidatePoints)
        {
            if (!from)
                return null;

            Neighbour retPoint = null;
            float minDist = pointConnection.minDistance;

            for (int p = 0; p < candidatePoints.Count; p++)
            {
                Neighbour targetPoint = candidatePoints[p];

                Vector3 direction = targetPoint.target.transform.position - from.transform.position;
                Vector3 relativeDirection = from.transform.InverseTransformDirection(direction).normalized;

                if (pointConnection.IsDirectionValid(targetDirection, relativeDirection))
                {
                    float dist = Vector3.Distance(from.transform.position, targetPoint.target.transform.position);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        retPoint = targetPoint;
                    }
                }
            }

            return retPoint;
        }

        bool CheckValidMovement(Vector3 translation)
        {
            bool ret = false;
            RaycastHit hit;

            if (translation.normalized.x < 0)
            {
                ret = characterController.characterDetection.ThrowHandRayToLedge(limitLHand.transform.position, out hit);
                if (ret)
                    curLedge = hit.collider.transform.parent.gameObject;
            }
            else if (translation.normalized.x > 0)
            {
                ret = characterController.characterDetection.ThrowHandRayToLedge(limitRHand.transform.position, out hit);
                if (ret)
                    curLedge = hit.collider.transform.parent.gameObject;
            }

            return ret;
        }

        Vector3 ReachLedge(RaycastHit hit)
        {
            Vector3 targetPos = Vector3.zero;

            curLedge = hit.transform.parent.gameObject;
            List<Point> points = hit.transform.parent.GetComponentInChildren<HandlePoints>().pointsInOrder;

            float dist = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                float point2root = Vector3.Distance(points[i].transform.position, transform.position);

                if (point2root < dist)
                {
                    dist = point2root;
                    targetPos = points[i].transform.position;
                    if (i == 0)//Left Point
                    {
                        targetPos.x += 0.5f;
                    }
                    else if (i == points.Count - 1)//Right Point
                    {
                        targetPos.x -= 0.5f;
                    }
                }
            }

            characterController.DisableController();
            characterController.characterAnimation.HangLedge();
            toLedge = true;


            return targetPos;
        }
    }
}