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
        bool jumping = false;

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
        Vector3 LastLHandPosition;
        Vector3 LastRHandPosition;
        float startTime = 0.0f;
        float endTime = 0.0f;

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

        public bool debug = false;
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
            if (debug)
            {
                if (targetPoint != null && currentPoint != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(targetPoint.transform.position, 0.1f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(currentPoint.transform.position, 0.1f);
                }

                Gizmos.DrawSphere(HandPosition, 0.1f);
            }
        }

        // Update is called once per frame
        void Update()
        {
            //Arrived on Ledge
            if (onLedge && characterController.dummy)
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
                bool matchingTarget = false;
                //Idle To Ledge
                if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle To Braced Hang") ||
                    characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle To Freehang"))
                {
                    matchingTarget = true;

                    if (wallFound) //Braced
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * BracedHangOffset, 0, 0.56f);
                    else //Free
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * FreeHangOffset, 0, 0.56f);
                }

                //Jump Ledge to Ledge 
                if (!characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Hanging Movement") && jumping == true)
                {
                    matchingTarget = true;

                    if (wallFound) //Braced
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * BracedHangOffset, startTime, endTime);
                    else //Free
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * FreeHangOffset, 0, 0.56f);
                }

                if (matchingTarget)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.5f);

                    if (characterAnimation.animator.IsInTransition(0))
                    {
                        onLedge = true;
                        toLedge = false;
                        jumping = false;
                        LastLHandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                        LastRHandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                    }
                }
            }
        }

        void ChangeBracedFreeHang()
        {
            if (curLedge)
            {
                if (wallFound && curClimbState != ClimbState.BHanging)
                {
                    curClimbState = ClimbState.BHanging;
                    Vector3 offset = new Vector3(0, 0.15f, 0.0f);
                    HandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                    HandPosition.y = curLedge.transform.position.y + offset.y;
                }
                else if (!wallFound && curClimbState != ClimbState.FHanging)
                {
                    curClimbState = ClimbState.FHanging;
                    Vector3 offset = new Vector3(0, -0.1f, 0.0f);
                    HandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                    HandPosition.y = curLedge.transform.position.y;
                }

                if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Free Hang To Braced") ||
                    characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Braced To FreeHang"))
                {
                    characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, HandPosition, transform.rotation, Vector3.zero, 0.0f, 0.001f);
                }
            }
        }

        public void ClimbMovement(float vertical, float horizontal)
        {
            if (curClimbState == ClimbState.BHanging)
                curOriginGrabOffset = originHandIKBracedOffset;
            else if (curClimbState == ClimbState.FHanging)
                curOriginGrabOffset = originHandIKFreeOffset;

            //Only allow to jump if is braced and not transitioning from free hang
            if (Input.GetKeyDown(KeyCode.Space) && wallFound && characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Hanging Movement"))
            {
                JumpToLedge(horizontal, vertical);
            }
            else //Movement Behaviour on Ledge
            {
                //Detect change of input direction to allow movement again after reaching end of the ledge
                if (((horizontal >= 0 && horizontalMovement <= 0) || (horizontal <= 0 && horizontalMovement >= 0)) ||
                    !characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Hanging Movement"))
                {
                    reachedEnd = false;
                }

                if (!reachedEnd)//Stops Movement on Ledge
                {
                    horizontalMovement = horizontal; //Stores player input direction

                    if (!CheckValidMovement(horizontal)) //Reached End
                    {
                        reachedEnd = true;
                    }
                }

                if (reachedEnd)
                {
                    horizontal = 0; //Stops Horizontal Movement
                }

                //Solver to position Limbs + Check if need to change climb state from Braced and Free Hang
                IKSolver();

                //Change from Braced Hang <-----> Free Hang
                ChangeBracedFreeHang();

                characterAnimation.HangMovement(horizontal, (int)curClimbState); //Move on Ledge Animations
            }
        }

        void JumpToLedge(float horizontal, float vertical)
        {
            if (vertical == 0 && horizontal == 0)
                return;

            Point point = null;
            if (horizontalMovement <= 0)
                point = curLedge.GetComponentInChildren<HandlePointsV2>().GetClosestPoint(LastLHandPosition);
            else if (horizontalMovement > 0)
                point = curLedge.GetComponentInChildren<HandlePointsV2>().GetClosestPoint(LastRHandPosition);


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
                        targetRot = toPoint.target.transform.rotation;
                        curLedge = toPoint.target.transform.parent.parent.gameObject;
                        targetPoint = toPoint.target;

                        //if (toPoint.target == curLedge.GetComponentInChildren<HandlePointsV2>().furthestLeft)//Left Point
                        //{
                            //target += toPoint.target.transform.rotation * new Vector3(0.5f, 0, 0);
                        //}
                        if (toPoint.target == curLedge.GetComponentInChildren<HandlePointsV2>().furthestRight)//Right Point
                        {
                            target -= toPoint.target.transform.rotation * new Vector3(0.5f, 0, 0);
                        }
                        onLedge = false;
                        toLedge = true;
                        jumping = true;

                        characterController.characterAnimation.LedgeToLedge(curClimbState, toPoint.direction, ref startTime, ref endTime);
                    }
                }
            }
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
            else
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
            }
            if (characterController.characterDetection.ThrowFootRayToLedge(origin4, Vector3.forward, IKFootRayLength, out hit4))
            {
                rightFootPosition = hit4.point + hit4.normal * 0.15f;
            }
            else
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            }
        }
        bool CheckValidMovement(float translation)
        {
            bool ret = false;
            RaycastHit hit1;
            RaycastHit hit2;
            RaycastHit hit3;
            RaycastHit hit4;

            Vector3 origin1 = limitLHand.transform.position + (transform.rotation * (curOriginGrabOffset + new Vector3(-0.18f,0,0)));
            Vector3 origin2 = limitRHand.transform.position + (transform.rotation * (curOriginGrabOffset));
            origin1.y = transform.position.y + curOriginGrabOffset.y - 0.05f;
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
                origin3 = transform.position + (transform.rotation * (curOriginGrabOffset + new Vector3(-0.45f, 0, 0)));
                origin4 = transform.position + (transform.rotation * (curOriginGrabOffset + new Vector3(0.35f, 0, 0)));
                origin3.y = transform.position.y + 0.5f;
                origin4.y = origin3.y;
            }

            if (characterController.characterDetection.ThrowHandRayToLedge(origin1, Vector3.forward, IKHandRayLength, out hit1))
            {
                if (translation < 0)
                {
                    curLedge = hit1.collider.transform.parent.gameObject;
                    LastLHandPosition = hit1.point;
                    ret = true;
                }
            }
            if (characterController.characterDetection.ThrowHandRayToLedge(origin2, Vector3.forward, IKHandRayLength, out hit2)){
                if (translation > 0)
                {
                    curLedge = hit2.collider.transform.parent.gameObject; 
                    LastRHandPosition = hit2.point;
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
                Debug.DrawLine(origin, origin + -tangent * (raylength + 0.25f), Color.cyan);
                if (Physics.Raycast(origin, -tangent, out hit, raylength + 0.25f, characterDetection.climbLayer))
                {
                    Vector3 newPos = Vector3.zero;

                    newPos = (hit.point + tangent * raylength);

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

            return targetPos;
        }
    }
}