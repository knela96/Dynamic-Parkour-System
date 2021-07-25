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
        float rotTime = 0.0f;

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
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(target, 0.1f);
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
            else if (!wallFound)
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            }
        }

        // Update is called once per frame
        void Update()
        {
            //On Ledge
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
                    characterController.cameraController.newOffset(false);
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
                if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button1)) && !toLedge && !onLedge)
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
                        startTime = 0.0f;
                        endTime = 0.2f;
                    }
                    else
                    {
                        target = Vector3.zero;
                        targetRot = Quaternion.identity;
                    }
                }
                if (Input.GetKey(KeyCode.C) && !characterController.characterMovement.inBoundaries)
                {
                    characterDetection.FindDropLedgeCollision(out hit);
                    if (hit.collider)
                    {
                        target = ReachLedge(hit);
                        targetRot = Quaternion.LookRotation(-hit.normal);

                        wallFound = characterDetection.FindFootCollision(target, targetRot, hit.normal);

                        transform.rotation = Quaternion.FromToRotation(transform.forward, hit.normal) * transform.rotation;//rotates towards ledge direction

                        if (wallFound)
                        {
                            characterAnimation.DropToBraced((int)ClimbState.BHanging);
                            curClimbState = ClimbState.BHanging;
                        }
                        else
                        {
                            characterAnimation.DropToFree((int)ClimbState.FHanging);
                            curClimbState = ClimbState.FHanging;
                        }

                        startTime = 0.3f;
                        endTime = 0.45f;
                    }
                }
            }

            //Jump to Point
            if (toLedge)
            {
                bool matchingTarget = false;
                bool matchRotation = true;

                //Idle To Ledge
                if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle To Braced Hang") ||
                    characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle To Freehang"))
                {
                    matchingTarget = true;
                    rotTime = 0;

                    if (wallFound) //Braced
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * BracedHangOffset, startTime, 0.56f);
                    else //Free
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * FreeHangOffset, startTime, 0.56f);
                }

                //Jump Ledge to Ledge 
                if (!characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Hanging Movement") && jumping == true)
                {
                    matchingTarget = true;
                    rotTime = 0;

                    characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * BracedHangOffset, startTime, endTime);
                }

                //Climb 
                if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Braced Hang To Crouch") ||
                    characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Freehang Climb"))
                {
                    matchingTarget = true;
                    rotTime = 0;

                    if (curClimbState == ClimbState.BHanging)
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftFoot, target, targetRot, Vector3.zero, startTime, endTime);// Braced
                    else
                        characterAnimation.SetMatchTarget(AvatarTarget.RightFoot, target, targetRot, Vector3.zero, startTime, endTime);

                    curClimbState = ClimbState.None;
                    characterAnimation.DropLedge((int)curClimbState);

                }

                //Dismount
                if (characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Drop To Freehang") ||
                    characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Drop To Bracedhang"))
                {
                    matchingTarget = true;
                    matchRotation = false;
                    rotTime = 0;

                    if (wallFound)
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * -BracedHangOffset, startTime, endTime);
                    else
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, Vector3.zero, startTime, endTime);
                }

                if (matchingTarget)
                {
                    if (matchRotation)
                    {
                        if(characterAnimation.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= startTime && rotTime <= 1.0f)
                        {
                            rotTime += Time.deltaTime / (endTime - startTime - 0.1f);
                            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotTime);
                        }
                    }

                    if (characterAnimation.animator.IsInTransition(0)) //If MatchTarget animation ends, reset default values
                    {
                        if(curClimbState == ClimbState.None)//Enable controller if climbing animation ends
                            characterController.EnableController();

                        onLedge = true;
                        toLedge = false;
                        jumping = false;
                        leftHandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                        rightHandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.RightHand).position;
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

            //Only allow to jump if is on Hanging Movement
            if (Input.GetKeyDown(KeyCode.Space) && characterAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Hanging Movement"))
            {
                bool climb = false;
                
                //Check if can climb to ground
                if (vertical > 0)
                    climb = ClimbFromLedge();

                if (!climb && wallFound)
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

                //Solver to position Limbs + Checks if need to change climb state from Braced and Free Hang
                IKSolver();

                //Change from Braced Hang <-----> Free Hang
                ChangeBracedFreeHang();

                characterAnimation.HangMovement(horizontal, (int)curClimbState); //Move on Ledge Animations
            }
        }

        bool ClimbFromLedge()
        {
            bool ret;

            RaycastHit hit;

            Vector3 origin = leftHandPosition + (rightHandPosition - leftHandPosition) / 2;
            origin.y = leftHandPosition.y;

            if (characterController.characterDetection.ThrowClimbRay(origin, transform.forward, IKHandRayLength, out hit))
            {
                if (curClimbState == ClimbState.BHanging)
                {
                    characterAnimation.BracedClimb();
                    startTime = 0.70f;
                    endTime = 1.0f;
                }
                else
                {
                    characterAnimation.FreeClimb();
                    startTime = 0.80f;
                    endTime = 1.0f;
                }

                target = hit.point;
                targetRot = transform.rotation;
                toLedge = true;
                onLedge = false;
                ret = true;
                characterController.cameraController.newOffset(false);
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        void JumpToLedge(float horizontal, float vertical)
        {
            if (vertical == 0 && horizontal == 0)
                return;

            Point point = null;
            float xDistance = 0;
            
            if (horizontalMovement > 0 && reachedEnd)
                point = curLedge.GetComponentInChildren<HandlePointsV2>().GetClosestPoint(rightHandPosition);
            else
                point = curLedge.GetComponentInChildren<HandlePointsV2>().GetClosestPoint(leftHandPosition);

            currentPoint = point;

            if (point)
            {
                Vector3 direction = new Vector3(horizontal, vertical, 0f);

                Neighbour toPoint = CandidatePointOnDirection(direction, point, point.neighbours, ref xDistance);

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

                        direction = toPoint.direction;

                        if ((xDistance < 0.5f && xDistance > -0.5f) && direction.y != 0)
                            direction.x = 0;

                        wallFound = characterDetection.FindFootCollision(target, targetRot, -toPoint.target.transform.forward);

                        characterController.characterAnimation.LedgeToLedge(curClimbState, direction, ref startTime, ref endTime);
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


        public Neighbour CandidatePointOnDirection(Vector3 inputDirection, Point from, List<Neighbour> candidatePoints, ref float xDistance)
        {
            if (!from)
                return null;

            Neighbour retPoint = null;
            float minDist = float.PositiveInfinity;

            for (int p = 0; p < candidatePoints.Count; p++)
            {
                Neighbour targetPoint = candidatePoints[p];

                Vector3 direction = targetPoint.target.transform.position - from.transform.position;
                Vector3 pointDirection = from.transform.InverseTransformDirection(direction);
                Vector2 angles = pointConnection.IsDirectionAngleValid(inputDirection, pointDirection);

                if (angles != Vector2.zero)
                {
                    float dist = Mathf.Abs(angles.x - angles.y);
                    if (dist <= minDist)
                    {
                        Debug.Log(dist);
                        minDist = dist;
                        retPoint = targetPoint;
                        xDistance = pointDirection.x;
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

            //leftHandPosition = Vector3.zero;
            //rightHandPosition = Vector3.zero;
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
                    ret = true;
                }
            }
            if (characterController.characterDetection.ThrowHandRayToLedge(origin2, Vector3.forward, IKHandRayLength, out hit2)){
                if (translation > 0)
                {
                    curLedge = hit2.collider.transform.parent.gameObject;
                    ret = true;
                }
            }

            wallFound = true;
            if (!characterController.characterDetection.ThrowFootRayToLedge(origin3, Vector3.forward, IKFootRayLength + 0.3f, out hit3))
            {
                wallFound = false;
            }
            if (!characterController.characterDetection.ThrowFootRayToLedge(origin4, Vector3.forward, IKFootRayLength + 0.3f, out hit4))
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
                if (Physics.Raycast(origin, -tangent, out hit, raylength + 0.25f, characterDetection.ledgeLayer))
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
            characterController.cameraController.newOffset(true);

            return targetPos;
        }
    }
}