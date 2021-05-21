using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Climbing;

public class ClimbController : MonoBehaviour
{
    bool ledgeFound = false;
    public bool onLedge = false;

    public DetectionCharacterController characterDetection;
    public ThirdPersonController characterController;
    public HandlePointConnection pointConnection;
    public float rootOffset;
    Vector3 target = Vector3.zero;
    public float lateralSpeed = 25f;

    public GameObject limitLHand;
    public GameObject limitRHand;

    GameObject curLedge;

    Point targetPoint = null;

    bool debug = false;

    // Start is called before the first frame update
    void Start()
    {
        curLedge = null;
    }

    private void OnDrawGizmos()
    {
        if (targetPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPoint.transform.position, 0.1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            onLedge = true;
        }

        if (!characterController.dummy)
        {
            onLedge = false;
            RaycastHit hit;
            ledgeFound = characterDetection.FindLedgeCollision(out hit);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button1))
            {
                if (ledgeFound)
                    ReachLedge(hit);
            }
        }

        if (onLedge)
        {
            ClimbMovement(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

            if (Input.GetKeyDown(KeyCode.C))
            {
                curLedge = null;
                characterController.EnableController();
            }
        }
    }

    public void ClimbMovement(float vertical, float horizontal)
    {
        Vector3 translation = transform.right * horizontal * (lateralSpeed * 0.001f);

        if (CheckValidMovement(translation) && !debug)
        {
            transform.position += translation;
        }
        else //Check for Near Ledge
        {
            Point point = null;
            
            if (horizontal > 0)//Right Movement
            {
                point = curLedge.GetComponentInChildren<HandlePoints>().furthestRight;
            }
            else if (horizontal < 0)// Left Movement
            {
                point = curLedge.GetComponentInChildren<HandlePoints>().furthestLeft;
            }

            if (point)
            {
                Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
                
                Neighbour toPoint = CandidatePointOnDirection(direction, point, point.neighbours);

                if (toPoint != null)
                {
                    if (toPoint.type == ConnectionType.direct) //Jump Reachable
                    {
                        Vector3 target = toPoint.target.transform.position - new Vector3(0, rootOffset, 0);
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
                        transform.position = target;
                        onLedge = false;
                    }
                    if (toPoint.type == ConnectionType.inBetween) //Continuous Ledge
                    {

                    }
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

    bool CheckValidMovement(Vector3 translation)
    {
        bool ret = false;

        if (translation.normalized.x < 0)
            ret = characterController.characterDetection.ThrowRayToLedge(limitLHand.transform.position);
        if (translation.normalized.x > 0)
            ret = characterController.characterDetection.ThrowRayToLedge(limitRHand.transform.position);
        if (translation.normalized.x == 0)
            ret = true;

        return ret;
    }

    void ReachLedge(RaycastHit hit)
    {
        curLedge = hit.transform.parent.gameObject;
        List<Point> points = hit.transform.parent.GetComponentInChildren<HandlePoints>().pointsInOrder;

        float dist = float.PositiveInfinity;
        for (int i = 0; i < points.Count; i++)
        {
            float point2root = Vector3.Distance(points[i].transform.position, transform.position);

            if (point2root < dist)
            {
                dist = point2root;
                target = points[i].transform.position;
                if (i == 0)//Left Point
                {
                    target.x += 0.5f;
                }
                else if(i == points.Count - 1)//Right Point
                {
                    target.x -= 0.5f;
                }
            }
        }

        characterController.DisableController();
        characterController.characterAnimation.HangLedge();
        onLedge = true;
        characterController.characterAnimation.animator.CrossFade("Hanging Idle", 0.0f);
        transform.rotation = Quaternion.LookRotation(-hit.normal);
        transform.position = target - new Vector3(0, rootOffset, 0);
    }

}
