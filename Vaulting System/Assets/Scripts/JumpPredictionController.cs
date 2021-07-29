using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Climbing
{
    public class JumpPredictionController : MonoBehaviour
    {
        [SerializeField]
        public float maxHeight = 1.5f;
        [SerializeField]
        public float maxDistance = 5.0f;
        [SerializeField]
        public float maxTime = 2.0f;

        float turnSmoothVelocity;
        ThirdPersonController controller;

        Vector3 origin;
        Vector3 target;
        float distance = 0;
        float delay = 0;

        public bool showDebug = false;

        bool move = false;
        bool newPoint = false;

        protected float actualSpeed = 0;

        public int accuracy = 50;

        public Point curPoint = null;

        private void Start()
        {
            controller = GetComponent<ThirdPersonController>();
        }

        void OnDrawGizmos()
        {
            if (!showDebug)
                return;

            if (!Application.isPlaying)
                origin = transform.position;

            if (origin == Vector3.zero)
                return;

            //Draw the parabola by sample a few times
            Vector3 lastP = origin;
            for (float i = 0; i < accuracy; i++)
            {
                Vector3 p = SampleParabola(origin, target, maxHeight, i / accuracy);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(lastP, p);
                Gizmos.DrawWireSphere(p, 0.02f);
                lastP = p;
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void JumpUpdate()
        {
            if (hasArrived())
            {
                if (Input.GetKey(KeyCode.Space) && controller.characterMovement.limitMovement)
                {
                    List<HandlePointsV2> points = new List<HandlePointsV2>();
                    controller.characterDetection.FindAheadPoints(ref points);

                    float minRange = float.NegativeInfinity;
                    float minDist = float.PositiveInfinity;
                    Point p = null;
                    newPoint = false;

                    foreach (var item in points)
                    {
                        if (item.pointType != PointType.Ledge)
                        {
                            foreach (var point in item.pointsInOrder)
                            {
                                if (point == null || point.type == PointType.Ledge || curPoint == point)
                                    continue;

                                Vector3 targetDirection = point.transform.position - transform.position;

                                Vector2 d1 = new Vector2(targetDirection.x, targetDirection.z).normalized;
                                Vector2 d2 = new Vector2(transform.forward.x, transform.forward.z).normalized;
                                float dot = Vector3.Dot(d1, d2);

                                if (curPoint == null)
                                {
                                    p = point;
                                    curPoint = p;
                                    minRange = dot;
                                    minDist = targetDirection.sqrMagnitude;
                                    newPoint = true;
                                }
                                //else if (curPoint.transform.parent != point.transform.parent && dot > 0.8 && ((dot >= minRange && targetDirection.sqrMagnitude < minDist) || targetDirection.sqrMagnitude < minDist))
                                else if (curPoint.transform.parent != point.transform.parent && dot > 0.8 && targetDirection.sqrMagnitude < minDist)
                                {
                                    p = point;
                                    minRange = dot;
                                    minDist = targetDirection.sqrMagnitude;
                                    newPoint = true;
                                }

                            }
                        }
                    }

                    if (newPoint && p != null)
                    {
                        curPoint = p;

                        if (SetParabola(transform.position, curPoint.transform.position))
                        {
                            switch (curPoint.type)
                            {
                                case PointType.Pole:
                                    controller.characterMovement.stopMotion = true;
                                    controller.characterAnimation.JumpPrediction(true);
                                    break;
                                case PointType.Ground:
                                    controller.characterMovement.stopMotion = false;
                                    controller.characterAnimation.JumpPrediction(false);
                                    break;
                            }

                            controller.DisableController();
                        }
                    }

                    points.Clear();
                }
            }

        }

        public bool ExecuteFollow()
        {
            if (!hasArrived())
            {
                FollowParabola(0.7f);
                return true;
            }
            else
            {
                //Has arribed to destiny
                if (!controller.characterMovement.stopMotion)
                    return false;


                //On MidPoint
                if (curPoint)
                {
                    Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
                    if (direction != Vector3.zero)
                        controller.RotatePlayer(direction);

                    //Delay between jumps
                    if (delay < 0.1f)
                        delay += Time.deltaTime;
                    else
                        JumpUpdate();


                    return true;
                }
            }

            return false;
        }

        public void FollowParabola(float length)
        {
            if (move == true)
            {
                if (actualSpeed >= 1.0f)
                {
                    if (!controller.characterMovement.stopMotion)
                        controller.EnableController();

                    actualSpeed = 0.0f;
                    delay = 0;
                    move = false;
                }
                else
                {
                    actualSpeed += Time.deltaTime / length;
                    transform.position = SampleParabola(origin, target, maxHeight, actualSpeed);

                    //Rotate Mesh to Movement
                    Vector3 travelDirection = target - origin;
                    float targetAngle = Mathf.Atan2(travelDirection.x, travelDirection.z) * Mathf.Rad2Deg;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.1f);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }
            }
        }

        public bool hasArrived()
        {
            return !move;
        }

        public bool SetParabola(Vector3 start, Vector3 end)
        {
            Vector2 a = new Vector2(start.x, start.z);
            Vector2 b = new Vector2(end.x, end.z);
            distance = Vector3.Distance(start, end);

            if (end.y - start.y > maxHeight || (distance > maxDistance))
                return false;

            origin = start;
            target = end;
            move = true;
            newPoint = false;

            actualSpeed = 0.0f;

            return true;
        }

        Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
        {
            float parabolicT = t * 2 - 1;
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += (-parabolicT * parabolicT + 1) * height;

            //Vector3 travelDirection = end - start;
            //Vector3 result = start + t * travelDirection;
            //result.y += Mathf.Sin(t * Mathf.PI) * height;

            return result;

            /*
            if (Mathf.Abs(start.y - end.y) < 0.1f)
            {
                Vector3 travelDirection = end - start;
                Vector3 result = start + t * travelDirection;
                result.y += (-parabolicT * parabolicT + 1) * height;
                return result;
            }
            else
            {
                Vector3 travelDirection = end - start;
                //Vector3 levelDirection = end - new Vector3(start.x, end.y, start.z);
                //Vector3 right = Vector3.Cross(travelDirection, levelDirection);
                //Vector3 up = Vector3.Cross(right, levelDirection);
                Vector3 up = Vector3.up;
                //if (end.y > start.y) up = -up;
                Vector3 result = start + t * travelDirection;
                result += ((-parabolicT * parabolicT + 1) * height) * Vector3.up;
                return result;
            }
            */
        }

    }
}
