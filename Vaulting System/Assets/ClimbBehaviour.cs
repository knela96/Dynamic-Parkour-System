using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class ClimbBehaviour : MonoBehaviour
    {
        public bool climbing;
        bool initClimb;
        bool waitToStartClimb;

        Animator anim;
        ClimbIK ik;

        Manager curManager;
        Point targetPoint;
        Point curPoint;
        Point prevPoint;
        Neighbour neighbour;
        ConnectionType curConnection;

        ClimbStates climbState;
        ClimbStates targetState;

        public enum ClimbStates
        {
            onPoint,
            betweenPoints,
            inTransit
        }

        //CurvesHolder curvesHolder;
        //BezierCurve directCurveHorizontal;
        //BezierCurve directCurveVertical;
        //BezierCurve dismountCurve;
        //BezierCurve mountCurve;
        //BezierCurve curCurve;

        Vector3 _startPos;
        Vector3 _targetPos;
        float distance;
        float _t;
        bool initTransit;
        bool rootReached;
        bool ikLandSideReached;
        bool ikFollowSideReached;

        bool lockInput;
        Vector3 inputDirection;
        Vector3 targetDirection;

        public Vector3 rootOffset = new Vector3(0, -0.88f, 0);
        public float speed_linear = 1.3f;
        public float speed_direct = 2;

        public AnimationCurve a_jumpintCurve;
        public AnimationCurve a_mountCurve;
        public bool enableRootMovement;
        float _rmMax = 0.25f;
        float _rmT;

        void SetCurveReferences()
        {
            GameObject curvePrefab = Resources.Load("CurvesHolder") as GameObject;
            GameObject chGO = Instantiate(curvePrefab);

            //curvesHolder = chGO.GetComponent<CurvesHolder>();
            //
            //directCurveHorizontal = curvesHolder.ReturnCurve(CurvesHolder.CurveType.horizontal);
            //directCurveVertical = curvesHolder.ReturnCurve(CurvesHolder.CurveType.vertical);
            //dismountCurve = curvesHolder.ReturnCurve(CurvesHolder.CurveType.dismount);
            //mountCurve = curvesHolder.ReturnCurve(CurvesHolder.CurveType.mount);
        }

        void Start()
        {
            anim = GetComponentInChildren<Animator>();
            ik = GetComponentInChildren<ClimbIK>();
            SetCurveReferences();
        }

        void FixedUpdate()
        {
            if(climbing)
            {
                if (!waitToStartClimb)
                {
                    HandleClimbing();
                    InitiateFallOff();
                }
                else
                {
                    InitClimbing();
                    HandleMount();
                }
            }
            else
            {
                if (initClimb)
                {
                    transform.parent = null;
                    initClimb = false;
                }
                if (Input.GetKey(KeyCode.Space))
                {
                    LookForClimbSpot();
                }
            }
        }

        void LookForClimbSpot()
        {
            Transform camTransform = Camera.main.transform;
            Ray ray = new Ray(camTransform.position, camTransform.forward);

            RaycastHit hit;
            LayerMask lm = (1 << gameObject.layer) | (1 << 3);
            lm = ~lm;

            float maxDistance = 20;

            if (Physics.Raycast(ray, out hit, maxDistance, lm))
            {
                if (hit.transform.GetComponentInParent<Manager>())
                {
                    Manager tm = hit.transform.GetComponentInParent<Manager>();
                    Point closesPoint = tm.ReturnClosest(transform.position);
                    float distanceToPoint = Vector3.Distance(transform.position, closesPoint.transform.parent.position);

                    if (distanceToPoint < 5)
                    {
                        curManager = tm;
                        targetPoint = closesPoint;
                        _targetPos = closesPoint.transform.position;
                        curPoint = closesPoint;
                        climbing = true;
                        lockInput = true;
                        targetState = ClimbStates.onPoint;

                        anim.CrossFade("To_Climb", 0.4f);
                        GetComponent<ThirdPersonController>().DisableController();

                        waitToStartClimb = true;
                    }
                }
            }
        }

        void InitClimbing()
        {
            if (!initClimb)
            {
                initClimb = true;
                if(ik != null)
                {
                    //ik.UpdateAllPointsOnOne(targetPoint);
                    //ik.UpdateAllTargetPositions(targetPoint);
                    //ik.ImmediatePlaceHelpers();
                }

                curConnection = ConnectionType.direct;
                targetState = ClimbStates.onPoint;
            }
        }

        void HandleMount()
        {
            if (!initTransit)
            {
                initTransit = true;
                ikFollowSideReached = false;
                ikLandSideReached = false;
                _t = 0;
                _startPos = transform.position;
                //_targetPos = targetPosition + rootOffset;
                //
                //curCurve = mountCurve;
                //curCurve.transform.rotation = targetPoint.transform.rotation;
                //BezierPoint[] points = curCurve.GetAnchorPoints();
                //points[0].transform.position = _startPos;
                //points[points.Length - 1].transform.position = _targetPos;
            }

            if (enableRootMovement)
                _t += Time.deltaTime * 2;

            if(_t >= 0.99f)
            {
                _t = 1;
                waitToStartClimb = false;
                lockInput = false;
                initTransit = false;
                ikLandSideReached = false;
                climbState = targetState;
            }

            //Vector3 targetPos = curCurve.GetPointAt(_t);
            //transform.position = targetPos;
            //
            //HandleWeightAll(_t, a_mountCurve);

            HandleRotation();
        }

        void HandleRotation()
        {
            Vector3 targetDir = targetPoint.transform.forward;

            if (targetDir == Vector3.zero)
                targetDir = transform.forward;

            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2);
        }

        void InitiateFallOff()
        {
            if(climbState == ClimbStates.onPoint)
            {
                if (Input.GetKeyUp(KeyCode.C))
                {
                    climbing = false;
                    initTransit = false;
                    //ik.AddWeightInfluenceAll(0);
                    GetComponent<ThirdPersonController>().EnableController();
                    anim.SetBool("onAir", true);
                }
            }
        }

        void HandleClimbing()
        {
            if (!lockInput)
            {
                inputDirection = Vector3.zero;

                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");

                inputDirection = ConvertToInputDirection(h, v);

                if(inputDirection != Vector3.zero)
                {
                    switch (climbState)
                    {
                        case ClimbStates.onPoint:
                            OnPoint(inputDirection);
                            break;
                        case ClimbStates.betweenPoints:
                            BetweenPoints(inputDirection);
                            break;
                    }
                }

                transform.parent = curPoint.transform.parent;

                if(climbState == ClimbStates.onPoint)
                {
                    //ik.UpdateAllTargetPositions(curPoint);
                    //ik.ImmediatePlaceHelpers();
                }

            }
            else
            {
                InTransit(inputDirection);
            }
        }

        Vector3 ConvertToInputDirection(float horizontal, float vertical)
        {
            int h = (horizontal != 0) ?
                (horizontal < 0) ? -1 : 1
                : 0;

            int v = (vertical != 0) ?
                (vertical < 0) ? -1 : 1
                : 0;

            int z = v + h;

            z = (z != 0) ?
                (z < 0) ? -1 : 1
                : 0;

            Vector3 retVal = Vector3.zero;
            retVal.x = h;
            retVal.y = v;

            return retVal;
        }

        void OnPoint(Vector3 dir)
        {
            neighbour = null;
            neighbour = curManager.ReturnNeighbour(dir, curPoint);

            if(neighbour != null)
            {
                targetPoint = neighbour.target;
                prevPoint = curPoint;
                climbState = ClimbStates.inTransit;
                UpdateCOnnectionTransitionByType(neighbour, dir);
                lockInput = true;
            }
        }

        void BetweenPoints(Vector3 dir)
        {
            Neighbour n = targetPoint.ReturnNeighbour(prevPoint);

            if(n != null)
            {
                if (dir == n.direction)
                    targetPoint = prevPoint;
            }
            else
            {
                targetPoint = curPoint;
            }

            //targetPosition = targetPoint.transform.position;
            climbState = ClimbStates.inTransit;
            targetState = ClimbStates.onPoint;
            prevPoint = curPoint;
            lockInput = true;
            anim.SetBool("Move", true);
        }

        void UpdateCOnnectionTransitionByType(Neighbour n, Vector3 dir)
        {
            Vector3 desiredPos = Vector3.zero;
            curConnection = n.type;

            Vector3 direction = targetPoint.transform.position - curPoint.transform.position;
            direction.Normalize();

            switch (n.type)
            {
                case ConnectionType.inBetween:
                    float distance = Vector3.Distance(curPoint.transform.position, targetPoint.transform.position);
                    desiredPos = curPoint.transform.position + (direction * (distance / 2));
                    targetState = ClimbStates.betweenPoints;
                    TransitDir transitDir = ReturnTransitDirection(direction, false);
                    PlayAnim(transitDir);
                    break;
                case ConnectionType.direct:
                    desiredPos = targetPoint.transform.position;
                    targetState = ClimbStates.onPoint;
                    TransitDir transitDir2 = ReturnTransitDirection(direction, true);
                    PlayAnim(transitDir2,true);
                    break;
                case ConnectionType.dismount:
                    desiredPos = targetPoint.transform.position;
                    anim.SetInteger("JumpType", 20);
                    anim.SetBool("Move", true);
                    break;
            }
        }

        void InTransit(Vector3 dir)
        {
            switch(curConnection)
            {
                //case ConnectionType.inBetween:
                //    UpdateLinearVariables();
                //    Linear_RootMovement();
                //    LerpIKLandingSide_Linear();
                //    WrapUp();
                //    break;
                //case ConnectionType.direct:
                //    UpdateDirectVariables(inputDirection);
                //    Direct_RootMovement();
                //    DirectHandleIK();
                //    WrapUp(true);
                //    break;
                //case ConnectionType.dismount:
                //    HandleDismountVariables();
                //    Dismount_RootMovement();
                //    HandleDismountIK();
                //    DismountWrapUp(true);
                //    break;
            }
        }

        TransitDir ReturnTransitDirection(Vector3 dir, bool jump)
        {
            TransitDir ret = default(TransitDir);

            float targetAngle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

            if (!jump)
            {
                if(Mathf.Abs(dir.y) > 0)
                {
                    ret = TransitDir.m_ver;
                }
                else
                {
                    ret = TransitDir.m_hor;
                }
            }
            else
            {
                if (targetAngle < 22.5f && targetAngle > -22.5f)
                    ret = TransitDir.j_up;
                else if (targetAngle < 180 + 22.5f && targetAngle > 180 - 22.5f)
                    ret = TransitDir.j_down;
                else if (targetAngle < 90 + 22.5f && targetAngle > 90 - 22.5f)
                    ret = TransitDir.j_down;
                else if (targetAngle < -90 + 22.5f && targetAngle > -90 - 22.5f)
                    ret = TransitDir.j_down;

                if(Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
                {
                    if (dir.y < 0)
                        ret = TransitDir.j_down;
                    else
                        ret = TransitDir.j_up;
                }
            }

            return ret;
        }

        void PlayAnim(TransitDir dir, bool jump = false)
        {
            int target = 0;

            switch (dir)
            {
                case TransitDir.m_hor:
                    target = 5;
                    break;
                case TransitDir.m_ver:
                    target = 6;
                    break;
                case TransitDir.j_up:
                    target = 0;
                    break;
                case TransitDir.j_down:
                    target = 1;
                    break;
                case TransitDir.j_left:
                    target = 3;
                    break;
                case TransitDir.j_right:
                    target = 2;
                    break;
            }
            anim.SetInteger("JumpType", target);

            if (!jump)
                anim.SetBool("Move", true);
            else
                anim.SetBool("Jump", true);
        }

        enum TransitDir
        {
            m_hor,
            m_ver,
            j_up,
            j_down,
            j_left,
            j_right,
        }

    }
}