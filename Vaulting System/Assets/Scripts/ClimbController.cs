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
        public Vector3 FreeHangOffset;
        public Vector3 BracedHangOffset;
        public Vector3 originHandIKBracedOffset;
        public Vector3 originHandIKFreeOffset;
        private Vector3 curOriginGrabOffset;
        public Vector3 originFootIKOffset;
        public float IKHandRayLength = 0.5f;
        public float IKFootRayLength = 0.5f;
        public float distanceToLedgeBraced = 0.40f;
        public float distanceToLedgeFree = 0.25f;
        Vector3 HandPosition;

        public GameObject limitLHand;
        public GameObject limitRHand;
        public GameObject limitLFoot;
        public GameObject limitRFoot;

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
                    wallFound = false;
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
            if (curClimbState == ClimbState.BHanging)
                curOriginGrabOffset = originHandIKBracedOffset;
            else if (curClimbState == ClimbState.FHanging)
                curOriginGrabOffset = originHandIKFreeOffset;

            //Detect change of input direction & Braced To Free animation Ended
            if ((horizontal >= 0 && horizontalMovement <= 0) || (horizontal <= 0 && horizontalMovement >= 0) || 
                (!characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Hanging Movement") &&
                characterAnimation.animator.IsInTransition(0)))
            {
                reachedEnd = false;
            }

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

            //Solver to position Limbs + Check if need to change climb state
            IKSolver();

            //Change from Braced Hang <-----> Free Hang

            if (wallFound && curClimbState != ClimbState.BHanging)
            {
                curClimbState = ClimbState.BHanging; //reachedEnd = true;
            }
            else if(!wallFound && curClimbState != ClimbState.FHanging)
            {
                curClimbState = ClimbState.FHanging; //reachedEnd = true;
            }

            if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Free Hang To Braced") ||
                characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Braced To FreeHang"))
            {

                HandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftHand).position;

                if (curClimbState == ClimbState.FHanging)
                    HandPosition.y = curLedge.transform.position.y;
                else
                    HandPosition.y = curLedge.transform.position.y + 0.05f;

                characterAnimation.SetMatchTarget(HandPosition, transform.rotation, Vector3.zero, 0, 0.5f);
            }

            //Move on Ledge
            characterAnimation.HangMovement(horizontal, (int)curClimbState);

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

        void IKSolver()
        {
            RaycastHit hit1;
            RaycastHit hit2;
            RaycastHit hit3;
            RaycastHit hit4;

            Vector3 origin1 = limitLHand.transform.position + (transform.rotation * new Vector3(-curOriginGrabOffset.x, curOriginGrabOffset.y, curOriginGrabOffset.z));
            Vector3 origin2 = limitRHand.transform.position + (transform.rotation * new Vector3(curOriginGrabOffset.x, curOriginGrabOffset.y, curOriginGrabOffset.z));
            Vector3 origin3 = limitLFoot.transform.position + (transform.rotation * originFootIKOffset);
            Vector3 origin4 = limitRFoot.transform.position + (transform.rotation * originFootIKOffset);
            origin1.y = transform.position.y + curOriginGrabOffset.y;
            origin2.y = origin1.y;
            

            leftHandPosition = Vector3.zero;
            rightHandPosition = Vector3.zero;
            leftFootPosition = Vector3.zero;
            rightFootPosition = Vector3.zero;

            if (characterController.characterDetection.ThrowHandRayToLedge(origin1, new Vector3(0.25f, -0.15f, 1).normalized, IKHandRayLength, out hit1))
            {
                leftHandPosition = hit1.point;
            }
            if (characterController.characterDetection.ThrowHandRayToLedge(origin2, new Vector3(-0.25f, -0.15f, 1).normalized, IKHandRayLength, out hit2))
            {
                rightHandPosition = hit2.point;
            }

            if (characterController.characterDetection.ThrowFootRayToLedge(origin3, Vector3.forward, IKFootRayLength, out hit3))
            {
                leftFootPosition = hit3.point + hit3.normal * 0.15f;
            }
            if (characterController.characterDetection.ThrowFootRayToLedge(origin4, Vector3.forward, IKFootRayLength, out hit4))
            {
                rightFootPosition = hit4.point + hit4.normal * 0.15f;
            }
        }
        bool CheckValidMovement(Vector3 translation)
        {
            bool ret = false;
            RaycastHit hit1;
            RaycastHit hit2;
            RaycastHit hit3;
            RaycastHit hit4;

            Vector3 origin1 = limitLHand.transform.position + (transform.rotation * (curOriginGrabOffset + new Vector3(-0.18f,0,0)));
            Vector3 origin2 = limitRHand.transform.position + (transform.rotation * (curOriginGrabOffset));
            origin1.y = transform.position.y + curOriginGrabOffset.y;
            origin2.y = origin1.y;

            Vector3 origin3 = Vector3.zero;
            Vector3 origin4 = Vector3.zero;
            if (ClimbState.BHanging == curClimbState)
            {
                origin3 = transform.position + (transform.rotation * new Vector3(-0.10f, 0, 0));
                origin4 = transform.position + (transform.rotation * new Vector3(0.10f, 0, 0));
            }
            else
            {
                origin3 = transform.position + (transform.rotation * (curOriginGrabOffset + new Vector3(-0.43f, 0, 0)));
                origin4 = transform.position + (transform.rotation * (curOriginGrabOffset + new Vector3(0.35f, 0, 0)));
                origin3.y = transform.position.y;
                origin4.y = origin3.y;
            }

            if (characterController.characterDetection.ThrowHandRayToLedge(origin1, Vector3.forward, IKHandRayLength, out hit1))
            {
                if (translation.normalized.x < 0)
                {
                    curLedge = hit1.collider.transform.parent.gameObject;
                    ret = true;
                }
            }
            if (characterController.characterDetection.ThrowHandRayToLedge(origin2, Vector3.forward, IKHandRayLength, out hit2)){
                if (translation.normalized.x > 0)
                {
                    curLedge = hit2.collider.transform.parent.gameObject;
                    ret = true;
                }
            }

            wallFound = true;
            if (!characterController.characterDetection.ThrowFootRayToLedge(origin3, Vector3.forward, IKFootRayLength, out hit3))
            {
                wallFound = false;
            }
            if (!characterController.characterDetection.ThrowFootRayToLedge(origin4, Vector3.forward, IKFootRayLength, out hit4))
            {
                wallFound = false;
            }

            //If movement is valid adjust player with the motion
            if (hit1.collider != null && hit2.collider != null)
            {
                //Rotates the character towards the ledge while moving
                Vector3 direction = hit2.point - hit1.point;
                Vector3 tangent = Vector3.Cross(Vector3.up, direction).normalized;
                transform.rotation = Quaternion.LookRotation(-tangent);

                //Sets the model at a relative distance from the ledge
                Vector3 origin = transform.position - transform.forward * 0.25f;
                origin.y += curOriginGrabOffset.y;

                float raylength = (curClimbState == ClimbState.BHanging) ? distanceToLedgeBraced : distanceToLedgeFree;

                RaycastHit hit;
                Debug.DrawLine(origin, origin + -tangent * raylength, Color.cyan);
                if (Physics.Raycast(origin, -tangent, out hit, raylength))
                {
                    Vector3 newPos = Vector3.zero;

                    if (curClimbState == ClimbState.BHanging)
                        newPos = (hit.point + tangent * 0.25f);
                    else if (curClimbState == ClimbState.FHanging)
                        newPos = (hit.point + tangent * 0.05f);

                    transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
                }
            }

            return ret;
        }

        void CalculateIKPositions(AvatarIKGoal IKGoal, ref Vector3 IKPosition)
        {
            Vector3 targetIKPosition = characterAnimation.animator.GetIKPosition(IKGoal);

            if (IKPosition != Vector3.zero)
            {
                Vector3 _IKPosition = transform.InverseTransformPoint(IKPosition);
                targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
                targetIKPosition.z = _IKPosition.z;
                targetIKPosition = transform.TransformPoint(targetIKPosition);
            }

            characterAnimation.animator.SetIKPosition(IKGoal, targetIKPosition);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!onLedge)
                return;

            characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            CalculateIKPositions(AvatarIKGoal.LeftHand, ref leftHandPosition);
            characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            CalculateIKPositions(AvatarIKGoal.RightHand, ref rightHandPosition);

            if (wallFound && curClimbState == ClimbState.BHanging)
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                CalculateIKPositions(AvatarIKGoal.LeftFoot, ref leftFootPosition);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                CalculateIKPositions(AvatarIKGoal.RightFoot, ref rightFootPosition);
            }
            else if(!wallFound)
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            }
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