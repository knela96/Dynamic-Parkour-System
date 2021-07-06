using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Climbing;

namespace Climbing
{
    public class ClimbController : MonoBehaviour
    {
        bool ledgeFound = false;
        bool wallFound = false;
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
        public float grabLedgeOffset = 0.28f;
        public Vector3 FreeHangOffset;
        public Vector3 BracedHangOffset;
        public Vector3 originHandIKOffset;

        public GameObject limitLHand;
        public GameObject limitRHand;
        bool dropping = false;
        bool reachedEnd = false;

        GameObject curLedge;

        Point targetPoint = null;
        Point currentPoint = null;

        Vector3 leftHandPosition, rightHandPosition, leftFootPosition, rightFootPosition = Vector3.zero;
        public string LHandAnimVariableName = "LHandCurve";
        public string RHandAnimVariableName = "RHandCurve";
        public string LFootAnimVariableName = "LeftFootCurve";
        public string RFootAnimVariableName = "RightFootCurve";

        bool debug = false;
        public enum ClimbState { None, BHanging, FHanging};
        private ClimbState curClimbState = ClimbState.None;

        float horizontalMovement = 0.0f;
        float lasthorizontalMovement = 0.0f;

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

                //Dismount from Ledge
                if (Input.GetKeyDown(KeyCode.C))
                {
                    curLedge = null;
                    targetPoint = null;
                    currentPoint = null;
                    curClimbState = ClimbState.None;
                    characterAnimation.DropLedge((int)curClimbState);
                }

                //Enable Controller when dismount animation ends
                if (!characterAnimation.animator.GetBool("ClimbAnimations"))
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
                        //transform.position = new Vector3(target.x, transform.position.y, target.z) + (hit.normal * grabLedgeOffset);
                        //characterController.jumpPrediction.SetParabola(transform.position, target - new Vector3(0, rootOffset, 0)); //target - new Vector3(0, rootOffset, 0);

                        wallFound = characterDetection.FindFootCollision(target, targetRot, hit.normal);

                        if (wallFound)
                            curClimbState = ClimbState.BHanging;
                        else
                            curClimbState = ClimbState.FHanging;

                        characterController.characterAnimation.HangLedge(curClimbState);
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
                if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle To Braced Hang") ||
                    characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle To Freehang"))
                {
                    if(wallFound) //Braced
                        characterAnimation.SetMatchTarget(target, targetRot, targetRot * BracedHangOffset);
                    else //Free
                        characterAnimation.SetMatchTarget(target, targetRot, targetRot * FreeHangOffset);

                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 0.5f);

                    if (characterAnimation.animator.IsInTransition(0)){
                        onLedge = true;
                        toLedge = false;
                    }
                }
            }
        }

        public void ClimbMovement(float vertical, float horizontal)
        {

            if ((horizontal >= 0 && horizontalMovement <= 0) || (horizontal <= 0 && horizontalMovement >= 0))//Detect change of input direction
                reachedEnd = false;

            if (!reachedEnd)
            {
                horizontalMovement = horizontal; //Stores player input direction

                Vector3 translation = transform.right * horizontal;// * (lateralSpeed * 0.001f);

                if (!CheckValidMovement(translation)) //Reached End
                {
                    reachedEnd = true;
                }
            }

            if (reachedEnd)
            {
                horizontal = 0; //Stops Horizontal Movement
            }
            

            characterAnimation.HangMovement(horizontal);

            /*
            if (valid)
            {
                transform.position += translation;
            }
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
            curLedge = null;
            RaycastHit hit1;
            RaycastHit hit2;

            Vector3 origin1 = limitLHand.transform.position + (transform.rotation * originHandIKOffset);
            Vector3 origin2 = limitRHand.transform.position + (transform.rotation * originHandIKOffset);
            origin1.y = transform.position.y + originHandIKOffset.y;
            origin2.y = origin1.y;

            if (characterController.characterDetection.ThrowHandRayToLedge(origin1, new Vector3(0.25f,-0.15f,1), out hit1) && translation.normalized.x < 0)
                curLedge = hit1.collider.transform.parent.gameObject;
            if (characterController.characterDetection.ThrowHandRayToLedge(origin2, new Vector3(-0.25f, -0.15f, 1), out hit2) && translation.normalized.x > 0)
                curLedge = hit2.collider.transform.parent.gameObject;

            CalculateIKPositions(hit1, hit2);

            return (curLedge != null) ? true : false;
        }

        void CalculateIKPositions(RaycastHit LHand, RaycastHit RHand)
        {
            leftHandPosition = transform.InverseTransformPoint(characterAnimation.animator.GetIKPosition(AvatarIKGoal.LeftHand));
            leftHandPosition.z = LHand.point.z;
            leftHandPosition = transform.TransformPoint(leftHandPosition);

            rightHandPosition = RHand.point;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, characterAnimation.animator.GetFloat(LHandAnimVariableName));
            characterAnimation.animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
            Debug.Log("Hand: " + limitLHand.transform.position + "Ray: " + leftHandPosition);
            //characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, characterAnimation.animator.GetFloat(RHandAnimVariableName));
            //characterAnimation.animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPosition);
            //characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, characterAnimation.animator.GetFloat(LFootAnimVariableName));
            //characterAnimation.animator.SetIKPosition(AvatarIKGoal.LeftHand, leftFootPosition);
            //characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, characterAnimation.animator.GetFloat(RFootAnimVariableName));
            //characterAnimation.animator.SetIKPosition(AvatarIKGoal.LeftHand, rightFootPosition);
        }

        Vector3 ReachLedge(RaycastHit hit)
        {
            Vector3 targetPos = Vector3.zero;

            curLedge = hit.transform.parent.gameObject;
            List<Point> points = hit.transform.parent.GetComponentInChildren<HandlePointsV2>().pointsInOrder;

            float dist = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                float point2root = Vector3.Distance(points[i].transform.position, transform.position);

                if (point2root < dist)
                {
                    dist = point2root;
                    targetPos = points[i].transform.position;
                    /*if (i == 0)//Left Point
                    {
                        //targetPos += hit.transform.right * 0.1f;
                    }*/
                    if (i == points.Count - 1)//Right Point Offset to place the player on Ledge
                    {
                        targetPos -= hit.transform.right * 0.5f;
                    }
                }
            }

            characterController.DisableController();
            toLedge = true;

           // wallFound = characterDetection.FindFootCollision(target, hit.normal);
           //
           // if (wallFound)
           //     curClimbState = ClimbState.BHanging;
           // else
           //     curClimbState = ClimbState.FHanging;
           //
           // characterController.characterAnimation.HangLedge(curClimbState);

            return targetPos;
        }
    }
}