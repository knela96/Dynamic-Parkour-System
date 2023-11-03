/*
MIT License

Copyright (c) 2023 Èric Canela
Contact: knela96@gmail.com or @knela96 twitter

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (Dynamic Parkour System), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class ClimbController : MonoBehaviour
    {
        public bool debug = false;
        public enum ClimbState { None, BHanging, FHanging };
        private ClimbState curClimbState = ClimbState.None;

        private bool active = false;
        private bool ledgeFound = false;
        private bool wallFound = false;
        private bool reachedEnd = false;
        private bool onLedge = false;
        private bool toLedge = false;
        private bool jumping = false;

        private float startTime = 0.0f;
        private float endTime = 0.0f;
        private float rotTime = 0.0f;
        private float horizontalMovement = 0.0f;
        private float smallHopMaxDistance = 0.35f; 
        private float distanceToLedgeBraced = 0.3f;
        private float distanceToLedgeFree = 0.1f;

        private ThirdPersonController characterController;
        private DetectionCharacterController characterDetection;
        private AnimationCharacterController characterAnimation;
        private GameObject curLedge;
        private Point targetPoint = null;
        private Point currentPoint = null;

        private Vector3 target = Vector3.zero;
        private Quaternion targetRot = Quaternion.identity;
        private Vector3 curOriginGrabOffset = Vector3.zero;
        private Vector3 HandPosition = Vector3.zero;
        private Vector3 leftHandPosition, rightHandPosition, leftFootPosition, rightFootPosition = Vector3.zero;

        [Header("Offset Positions")]
        [SerializeField] private Vector3 FreeHangOffset;
        [SerializeField] private Vector3 BracedHangOffset;

        [Header("IK Settings")]
        [SerializeField] private Vector3 originHandIKBracedOffset;
        [SerializeField] private Vector3 originHandIKFreeOffset;
        [SerializeField] private Vector3 originFootIKOffset;
        [SerializeField] private float IKHandRayLength = 0.5f;
        [SerializeField] private float IKFootRayLength = 0.5f;

        [Header("IK GameObjects")]
        [Tooltip("Auto Search the bones when not specified")]
        [SerializeField] private bool AutoSearchBones;
        [SerializeField] private GameObject LHand;
        [SerializeField] private GameObject RHand;
        [SerializeField] private GameObject LFoot;
        [SerializeField] private GameObject RFoot;

        [Header("Animation Curves")]
        public string LHandAnimVariableName = "LHandCurve";
        public string RHandAnimVariableName = "RHandCurve";
        public string LFootAnimVariableName = "LeftFootCurve";
        public string RFootAnimVariableName = "RightFootCurve";

        // Start is called before the first frame update
        void Start()
        {
            curLedge = null;
            characterController = GetComponent<ThirdPersonController>();
            characterAnimation = characterController.characterAnimation;
            characterDetection = characterController.characterDetection;

            if (LHand == null || RHand == null || LFoot == null || RFoot == null)
            {
                if (AutoSearchBones)
                {
                    Debug.LogWarning("In the Player ClimbController script is recommended to set the bones of Hands and Feet");

                    if (LHand == null)
                        LHand = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject;
                    if (RHand == null)
                        RHand = characterAnimation.animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject;
                    if (LFoot == null)
                        LFoot = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftFoot).gameObject;
                    if (RFoot == null)
                        RFoot = characterAnimation.animator.GetBoneTransform(HumanBodyBones.RightFoot).gameObject;
                }
                else
                {
                    Debug.LogError("In the Player check that the ClimbController script has the GameObjects of the Hands and Feet assigned");
                }
            }
        }

        public void onDrawGizmos()
        {
            if (targetPoint != null && currentPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(targetPoint.transform.position, 0.1f);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentPoint.transform.position, 0.1f);
            }
        }
        public void onAnimatorIK(int layerIndex)
        {
            //Reset IK Weight Position to default if not on Ledge
            if (!onLedge)
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                return;
            }

            //IK Position of Feet and Hands
            characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            CalculateIKPositions(AvatarIKGoal.LeftHand, ref leftHandPosition);
            characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            CalculateIKPositions(AvatarIKGoal.RightHand, ref rightHandPosition);

            if (wallFound && curClimbState == ClimbState.BHanging) //Activate Foot IK if Braced Hang
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                CalculateIKPositions(AvatarIKGoal.LeftFoot, ref leftFootPosition);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                CalculateIKPositions(AvatarIKGoal.RightFoot, ref rightFootPosition);
            }
            else if (!wallFound) //Disable Foot IK if Free Hang
            {
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                characterAnimation.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            }
        }

        /// <summary>
        /// Checks on Ground if the player can grab on a Ledge
        /// </summary>
        public bool ClimbCheck()
        {
            active = false;
            if (!characterController.dummy && characterController.isGrounded)
            {
                onLedge = false;
                RaycastHit hit;
                if (characterController.characterInput.jump && !toLedge && !onLedge)
                {
                    //Throw Raycast to find Ledges
                    ledgeFound = characterDetection.FindLedgeCollision(out hit);

                    if (ledgeFound)
                    {
                        //Find Target Point
                        target = ReachLedge(hit);
                        targetRot = Quaternion.LookRotation(-hit.normal);

                        //Check if Ledge is a Braced or FreeHand Point
                        wallFound = characterDetection.FindFootCollision(target, targetRot, hit.normal);

                        if (wallFound)
                            curClimbState = ClimbState.BHanging;
                        else
                            curClimbState = ClimbState.FHanging;

                        characterController.characterAnimation.HangLedge(curClimbState);
                        startTime = 0.0f;
                        endTime = 0.2f;
                        active = true;
                        characterController.ToggleWalk();
                    }
                    else
                    {
                        target = Vector3.zero;
                        targetRot = Quaternion.identity;
                    }
                }

                //If player wants to drop to a Ledge from a Top Surface
                if (characterController.characterInput.drop && characterController.isGrounded)
                {
                    //Throw Rays below Player
                    characterDetection.FindDropLedgeCollision(out hit);
                    if (hit.collider)
                    {
                        //Find Target Point
                        target = ReachLedge(hit);
                        targetRot = Quaternion.LookRotation(-hit.normal); 
                        transform.rotation = Quaternion.FromToRotation(transform.forward, hit.normal) * transform.rotation;//rotates towards ledge direction

                        //Check if Ledge is a Braced or FreeHand Point
                        wallFound = characterDetection.FindFootCollision(target, targetRot, hit.normal);

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
                        active = true;
                        characterController.ToggleWalk();
                    }
                }
            }
            return active;
        }

        /// <summary>
        /// Main climbing update that checks climbing movement and inTransition animations
        /// </summary>
        public bool ClimbUpdate()
        {
            if (!characterController.dummy && curLedge == null)
            {
                active = false;
            }

            if (onLedge && characterController.dummy)
            {
                //Movement on Ledge
                ClimbMovement(characterController.characterInput.movement); 

                //Dismount from Ledge
                if (characterController.characterInput.drop && characterController.characterInput.movement == Vector2.zero)
                {
                    wallFound = false;
                    curLedge = null;
                    onLedge = false;
                    targetPoint = null;
                    currentPoint = null;
                    characterController.isJumping = true;
                    curClimbState = ClimbState.None;
                    characterAnimation.DropLedge((int)curClimbState);
                    characterController.cameraController.newOffset(false);
                }
            }

            //Controls Climbing Transitions
            if (toLedge)
            {
                bool matchingTarget = false;
                bool matchRotation = true;

                //Idle To Ledge
                if (characterAnimation.animState.IsName("Idle To Braced Hang") ||
                    characterAnimation.animState.IsName("Idle To Freehang"))
                {
                    matchingTarget = true;
                    rotTime = 0;

                    if (wallFound) //Braced
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * BracedHangOffset, startTime, 0.56f);
                    else //Free
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * FreeHangOffset, startTime, 0.56f);
                }

                //Jump Ledge to Ledge 
                if (!characterAnimation.animState.IsName("Hanging Movement") && jumping == true)
                {
                    matchingTarget = true;
                    rotTime = 0;

                    characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * BracedHangOffset, startTime, endTime);
                }

                //Climb 
                if (characterAnimation.animState.IsName("Braced Hang To Crouch") ||
                    characterAnimation.animState.IsName("Freehang Climb"))
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
                if (characterAnimation.animState.IsName("Drop To Freehang") ||
                    characterAnimation.animState.IsName("Drop To Bracedhang"))
                {
                    matchingTarget = true;
                    rotTime = 0;

                    if (wallFound)
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, targetRot * -BracedHangOffset, startTime, endTime);
                    else
                        characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, target, targetRot, Vector3.zero, startTime, endTime);
                }

                //Move Player and Rotate to Target Point
                if (matchingTarget)
                {
                    if (matchRotation)
                    {
                        if (characterAnimation.animState.normalizedTime >= startTime && rotTime <= 1.0f)
                        {
                            rotTime += Time.deltaTime / 0.15f;
                            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotTime);
                        }
                    }

                    //If MatchTarget animation ends, reset default values
                    if (characterAnimation.animator.IsInTransition(0)) 
                    {
                        onLedge = true;
                        toLedge = false;
                        jumping = false;
                        leftHandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                        rightHandPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                        leftFootPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                        rightFootPosition = characterAnimation.animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                        //Enable controller if climbing State Ends
                        if (curClimbState == ClimbState.None)
                        {
                            active = false;
                            onLedge = false;
                        }
                    }
                }
            }
            return active;
        }

        public void ClimbMovement(Vector2 direction)
        {
            if (curClimbState == ClimbState.BHanging)
                curOriginGrabOffset = originHandIKBracedOffset;
            else if (curClimbState == ClimbState.FHanging)
                curOriginGrabOffset = originHandIKFreeOffset;

            //Detect change of input direction to allow movement again after reaching end of the ledge
            if (((direction.x >= 0 && horizontalMovement <= 0) || (direction.x <= 0 && horizontalMovement >= 0)) ||
                !characterAnimation.animState.IsName("Hanging Movement"))
            {
                reachedEnd = false;
            }

            if (!reachedEnd)//Stops Movement on Ledge
            {
                horizontalMovement = direction.x; //Stores player input direction

                if (!CheckValidMovement(direction.x))
                {
                    reachedEnd = true;
                }
            }

            //Stops Horizontal Movement if Reached End of Ledge
            if (reachedEnd)
                direction.x = 0;

            //Solver to position Limbs + Checks if need to change climb state from Braced and Free Hang
            IKSolver();

            //Change from Braced Hang <-----> Free Hang
            ChangeBracedFreeHang();

            characterAnimation.HangMovement(direction.x, (int)curClimbState); //Move on Ledge Animations

            //Only allow to jump to another ledge if is on Hanging Movement
            if ((characterController.characterInput.jump || characterController.characterInput.drop) && characterAnimation.animState.IsName("Hanging Movement"))
            {
                bool drop = false;
                if (characterController.characterInput.jump)
                {
                    drop = false;
                }
                if (characterController.characterInput.drop)
                {
                    drop = true;
                }

                //Check if can climb on surface
                bool climbing = false;
                if (characterController.characterInput.movement.y > 0.8f && characterController.characterInput.movement.x < 0.3 && characterController.characterInput.movement.x > -0.3 && onLedge)
                    climbing = ClimbFromLedge();

                if (wallFound && !climbing)
                    JumpToLedge(characterController.characterInput.movement.x, characterController.characterInput.movement.y, drop);

            }
        }

        /// <summary>
        /// Climbs From Ledge to Upwards Surface
        /// </summary>
        bool ClimbFromLedge()
        {
            bool ret;

            Vector3 origin = leftHandPosition + (rightHandPosition - leftHandPosition) / 2;
            origin.y = leftHandPosition.y;

            //Checks if the player fits on the top surface to climb
            RaycastHit hit;
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

        /// <summary>
        /// Checks available points to jump Ledge to Ledge dependng on the input direction
        /// </summary>
        void JumpToLedge(float horizontal, float vertical, bool drop)
        {
            if (vertical == 0 && horizontal == 0)
                return;

            Point point = null;
            float xDistance = 0;
            
            if (horizontalMovement > 0 && reachedEnd)
                point = curLedge.GetComponentInChildren<HandlePoints>().GetClosestPoint(rightHandPosition);
            else
                point = curLedge.GetComponentInChildren<HandlePoints>().GetClosestPoint(leftHandPosition);

            currentPoint = point;

            if (point)
            {
                Vector3 direction = new Vector3(horizontal, vertical, 0f);

                Neighbour toPoint = CandidatePointOnDirection(direction, point, point.neighbours, ref xDistance, drop);

                if (toPoint != null)
                {
                    curLedge = toPoint.target.transform.parent.parent.gameObject;
                    target = toPoint.target.transform.position;
                    targetRot = curLedge.transform.rotation;
                    targetPoint = toPoint.target;

                    //Reposition Player if target is a Right Point
                    if (toPoint.target == curLedge.GetComponentInChildren<HandlePoints>().furthestRight)
                    {
                        target -= toPoint.target.transform.rotation * new Vector3(0.5f, 0, 0);
                    }

                    onLedge = false;
                    toLedge = true;
                    jumping = true;

                    direction = toPoint.direction;

                    if ((xDistance < smallHopMaxDistance && xDistance > -smallHopMaxDistance) && direction.y != 0)
                        direction.x = 0;

                    wallFound = characterDetection.FindFootCollision(target, targetRot, -toPoint.target.transform.forward);

                    characterController.characterAnimation.LedgeToLedge(curClimbState, direction, ref startTime, ref endTime);
                }
            }
        }

        /// <summary>
        /// Changes Between Braced and Free Hang
        /// </summary>
        void ChangeBracedFreeHang()
        {
            if (curLedge)
            {
                if (wallFound && curClimbState != ClimbState.BHanging)
                {
                    curClimbState = ClimbState.BHanging;
                    Vector3 offset = new Vector3(0, 0f, 0.0f);
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

                //Adjust Next Animation to the previous Anim Hand Position
                if (characterAnimation.animState.IsName("Free Hang To Braced") ||
                    characterAnimation.animState.IsName("Braced To FreeHang"))
                {
                    characterAnimation.SetMatchTarget(AvatarTarget.LeftHand, HandPosition, transform.rotation, Vector3.zero, 0.0f, 0.001f);
                }
            }
        }

        /// <summary>
        /// Checks if input direction is within angle direction range of Target Point
        /// </summary>
        public Neighbour CandidatePointOnDirection(Vector3 inputDirection, Point from, List<Neighbour> candidatePoints, ref float xDistance, bool drop)
        {
            if (!from)
                return null;

            Neighbour retPoint = null;
            float minAngle = float.PositiveInfinity;

            for (int p = 0; p < candidatePoints.Count; p++)
            {
                Neighbour targetPoint = candidatePoints[p];

                if (candidatePoints[p].target == null)
                    continue;

                if (drop && targetPoint.target.transform.position.y >= from.transform.position.y)
                    continue;

                if (!drop && targetPoint.target.transform.position.y + 0.6f < from.transform.position.y)
                    continue;

                Vector3 direction = targetPoint.target.transform.position - from.transform.position;
                Vector3 pointDirection = from.transform.InverseTransformDirection(direction);
                pointDirection.z = 0;

                //This returns the angle between input and target direction
                float angle = Mathf.Acos(Vector3.Dot(inputDirection.normalized, pointDirection.normalized)) * Mathf.Rad2Deg;

                //Stores closest target with angle difference between 40 degrees
                if (angle < minAngle && Mathf.Abs(angle) < 40)
                {
                    minAngle = angle;
                    retPoint = targetPoint;
                    xDistance = pointDirection.x;
                }
            }

            return retPoint;
        }

        /// <summary>
        /// Computes IK Solver to place the limbs at the correct Ledge and Wall Position
        /// </summary>
        void IKSolver()
        {
            RaycastHit hit1;
            RaycastHit hit2;
            RaycastHit hit3;
            RaycastHit hit4;

            Vector3 origin1 = LHand.transform.position + (transform.rotation * new Vector3(-curOriginGrabOffset.x, curOriginGrabOffset.y, curOriginGrabOffset.z));
            Vector3 origin2 = RHand.transform.position + (transform.rotation * new Vector3(curOriginGrabOffset.x, curOriginGrabOffset.y, curOriginGrabOffset.z));
            Vector3 origin3 = LFoot.transform.position + (transform.rotation * originFootIKOffset);
            Vector3 origin4 = RFoot.transform.position + (transform.rotation * originFootIKOffset);
            origin1.y = transform.position.y + curOriginGrabOffset.y;
            origin2.y = origin1.y;

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

        /// <summary>
        /// Moves player on Ledge and checks if current movement is valid
        /// </summary>
        bool CheckValidMovement(float translation)
        {
            bool ret = false;
            RaycastHit hit1;
            RaycastHit hit2;
            RaycastHit hit3;
            RaycastHit hit4;

            Vector3 origin1 = LHand.transform.position + (transform.rotation * (curOriginGrabOffset + new Vector3(-0.18f,0,0)));
            Vector3 origin2 = RHand.transform.position + (transform.rotation * (curOriginGrabOffset));
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

            // Checks if Player can move on the ledge with the current movement
            if (characterController.characterDetection.ThrowHandRayToLedge(origin1, Vector3.forward, IKHandRayLength, out hit1))
            {
                if (translation < 0)
                {
                    curLedge = hit1.collider.transform.gameObject;
                    ret = true;
                }
            }
            if (characterController.characterDetection.ThrowHandRayToLedge(origin2, Vector3.forward, IKHandRayLength, out hit2)){
                if (translation > 0)
                {
                    curLedge = hit2.collider.transform.gameObject;
                    ret = true;
                }
            }

            //Checks if Foot detects a wall to place the feet
            if(curClimbState == ClimbState.BHanging)
            {
                bool b1 = characterController.characterDetection.ThrowFootRayToLedge(origin3, Vector3.forward, IKFootRayLength + 0.1f, out hit3);
                bool b2 = characterController.characterDetection.ThrowFootRayToLedge(origin4, Vector3.forward, IKFootRayLength + 0.1f, out hit4);
                if (!b1 && !b2)
                {
                    wallFound = false;
                }
            }
            else if (curClimbState == ClimbState.FHanging)
            {
                bool b1 = characterController.characterDetection.ThrowFootRayToLedge(origin3, Vector3.forward, IKFootRayLength + 0.1f, out hit3);
                bool b2 = characterController.characterDetection.ThrowFootRayToLedge(origin4, Vector3.forward, IKFootRayLength + 0.1f, out hit4);
                if (b1 && b2)
                {
                    wallFound = true;
                }
            }

            //If movement is valid adjust player with the motion
            if (hit1.collider != null && hit2.collider != null)
            {
                //Rotates the character towards the ledge while moving
                Vector3 direction = hit2.point - hit1.point;
                Vector3 tangent = Vector3.Cross(Vector3.up, direction).normalized;
                transform.rotation = Quaternion.LookRotation(-tangent);

                //Sets the model at a relative distance from the ledge without clipping into surface
                Vector3 origin = transform.position - transform.forward * 0.25f;
                origin.y += curOriginGrabOffset.y;

                float raylength = (curClimbState == ClimbState.BHanging) ? distanceToLedgeBraced + 0.2f : distanceToLedgeFree + 0.12f;

                if(debug)
                    Debug.DrawLine(origin, origin + -tangent * (raylength), Color.cyan);

                RaycastHit hit;
                if (characterDetection.ThrowRayOnDirection(origin, -tangent, raylength, out hit, characterDetection.ledgeLayer))
                {
                    raylength = (curClimbState == ClimbState.BHanging) ? distanceToLedgeBraced : distanceToLedgeFree;
                    Vector3 newPos = (hit.point + hit.normal * raylength);
                    transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
                }
            }

            return ret;
        }

        /// <summary>
        /// Calculates the IK Position to place the limb
        /// </summary>
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

        /// <summary>
        /// Gets the closes Point to the player to climb on the Ledge from ground
        /// </summary>
        Vector3 ReachLedge(RaycastHit hit)
        {
            Vector3 targetPos = Vector3.zero;

            curLedge = hit.transform.gameObject;
            HandlePoints handle = curLedge.GetComponentInChildren<HandlePoints>();
            List<Point> points = handle.pointsInOrder;

            float dist = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null)
                    continue;

                float point2root = Vector3.Distance(points[i].transform.position, transform.position);

                //Finds closes point on ledge relative to the player
                if (point2root < dist)
                {
                    dist = point2root;
                    targetPos = points[i].transform.position;

                    //Right Point Offset to place the player on Ledge
                    if (handle.furthestRight == points[i])
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