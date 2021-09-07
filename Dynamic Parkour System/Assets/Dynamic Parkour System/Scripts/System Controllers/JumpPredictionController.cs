/*
Dynamic Parkour System grants parkour capabilities to any humanoid character model.
Copyright (C) 2021  Èric Canela Sol
Contact: knela96@gmail.com or @knela96 twitter

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Climbing
{
    [RequireComponent(typeof(ThirdPersonController))]
    public class JumpPredictionController : MonoBehaviour
    {
        public bool showDebug = false;

        [Header("Jump Settings")]
        [SerializeField] private float maxHeight = 1.5f;
        [SerializeField] private float maxDistance = 5.0f;

        private ThirdPersonController controller;
        private float turnSmoothVelocity;
        private Vector3 origin;
        private Vector3 target;
        private float distance = 0;
        private float delay = 0;
        private bool move = false;
        private bool newPoint = false;
        private float actualSpeed = 0;
        private int accuracy = 50;

        [HideInInspector] public Point curPoint = null;

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

        /// <summary>
        /// Finds a Target Point to jump on
        /// </summary>
        public void CheckJump()
        {
            if (hasArrived() && !controller.isJumping && ((controller.isGrounded && curPoint == null) || curPoint != null) && controller.characterMovement.limitMovement)
            {
                if (controller.characterInput.jump && controller.characterInput.movement != Vector2.zero)
                {
                    List<HandlePoints> points = new List<HandlePoints>();
                    controller.characterDetection.FindAheadPoints(ref points);

                    float minRange = float.NegativeInfinity;
                    float minDist = float.PositiveInfinity;
                    Point p = null;
                    Point fp = curPoint;
                    newPoint = false;

                    //Gets direction relative to the input and the camera
                    Vector3 mov = new Vector3(controller.characterInput.movement.x, 0, controller.characterInput.movement.y);
                    Vector3 inputDir = controller.RotateToCameraDirection(mov) * Vector3.forward;

                    if (showDebug)
                        Debug.DrawLine(transform.position, transform.position + inputDir);

                    //Get below point for reference as first point
                    if (fp == null)
                    {
                        RaycastHit hit;
                        if (controller.characterDetection.ThrowRayOnDirection(transform.position, Vector3.down, 0.5f, out hit))
                        {
                            HandlePoints handle = hit.transform.GetComponentInChildren<HandlePoints>();
                            if (handle)
                            {
                                for (int i = 0; i < handle.pointsInOrder.Count; i++)
                                {
                                    if (handle.pointsInOrder[i] != null)
                                    {
                                        fp = handle.pointsInOrder[i];
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    //Find Possible Landing Points relative to player direction Input
                    foreach (var item in points)
                    {
                        if (item.pointType != PointType.Ledge)
                        {
                            foreach (var point in item.pointsInOrder)
                            {
                                if (point == null || point.type == PointType.Ledge || curPoint == point)
                                    continue;

                                Vector3 targetDirection = point.transform.position - transform.position;

                                Vector3 d1 = new Vector3(targetDirection.x, 0, targetDirection.z);

                                float dot = Vector3.Dot(d1.normalized, inputDir.normalized);

                                if (fp == null)//First Point
                                {
                                    fp = point;
                                }

                                if (fp.transform.parent == point.transform.parent)
                                    continue;

                                if (dot > 0.9 && targetDirection.sqrMagnitude < minDist && minRange - dot < 0.1f )
                                {
                                    p = point;
                                    minRange = dot;
                                    minDist = targetDirection.sqrMagnitude;
                                    newPoint = true;
                                }
                            }
                        }
                    }

                    bool target = false;

                    //Creates a new Jump to Landing Point
                    if (newPoint && p != null)
                    {
                        target = SetParabola(transform.position, p.transform.position);
                        if (target)
                        {
                            curPoint = p;

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
                            controller.isJumping = true;
                            controller.characterAnimation.animator.SetBool("PredictedJump", true);
                        }
                    }

                    //Creates a new Jump in case of not finding a Landing Point
                    if (!target)
                    {
                        Vector3 end = transform.position + inputDir * 4;

                        RaycastHit hit;
                        if(controller.characterDetection.ThrowRayOnDirection(transform.position, inputDir, 4, out hit))
                        {
                            Vector3 temp = hit.point;
                            temp.y = transform.position.y;
                            Vector3 dist = temp - transform.position;

                            if(dist.sqrMagnitude >= 2)
                            {   
                                end = hit.point + hit.normal * (controller.slidingCapsuleCollider.radius * 2);
                            }
                            else
                            {
                                end = Vector3.zero;
                            }
                        }

                        if (end != Vector3.zero)
                        {
                            if(SetParabola(transform.position, end))
                            {
                                controller.characterAnimation.JumpPrediction(false);
                                curPoint = null;
                                controller.characterMovement.stopMotion = true;
                                controller.DisableController();
                                controller.isJumping = true;
                            }
                        }
                    }

                    points.Clear();
                }
            }
        }

        /// <summary>
        /// While being on a pole check for next point to land
        /// </summary>
        /// <returns></returns>
        public bool isMidPoint()
        {
            if (curPoint == null || controller.characterInput.drop) //Player is Droping
            {
                curPoint = null;
                controller.EnableController();
            }
            else if (curPoint)
            {
                if (curPoint.type == PointType.Pole)
                {
                    Vector3 direction = new Vector3(controller.characterInput.movement.x, 0f, controller.characterInput.movement.y).normalized;
                    if (direction != Vector3.zero)
                        controller.RotatePlayer(direction);

                    controller.characterMovement.ResetSpeed();

                    //Check near points while OnPole
                    if (curPoint && !controller.isJumping)
                    {
                        //Delay between allowing new jump
                        if (delay < 0.1f)
                            delay += Time.deltaTime;
                        else
                            CheckJump();

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Moves the player through the previously created curve
        /// </summary>
        public void FollowParabola(float length)
        {
            if (move == true)
            {
                actualSpeed += Time.fixedDeltaTime / length;
                if (actualSpeed > 1)
                {
                    actualSpeed = 1;
                }
                controller.characterMovement.rb.position = SampleParabola(origin, target, maxHeight, actualSpeed);

                //Rotate Mesh to Movement
                Vector3 travelDirection = target - origin;
                float targetAngle = Mathf.Atan2(travelDirection.x, travelDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.1f);
                controller.characterMovement.rb.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        /// <summary>
        /// Checks if the jump has been completed
        /// </summary>
        public void hasEndedJump()
        {
            if (actualSpeed >= 1.0f && curPoint != null)
            {
                if (!controller.characterMovement.stopMotion)
                    controller.EnableController();

                controller.characterAnimation.animator.SetBool("PredictedJump", false);
                controller.isJumping = false;
                actualSpeed = 0.0f;
                delay = 0;
                move = false;
            }
            else if (actualSpeed >= 0.7f && curPoint == null) //
            {
                controller.EnableController();
                actualSpeed = 0.0f;
                delay = 0;
                move = false;
            }
        }

        public bool hasArrived()
        {
            return !move;
        }

        /// <summary>
        /// Checks if the startPos, endPos and maxHeight is valid to make the jump
        /// </summary>
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

        /// <summary>
        /// Creates a curve depending on the starting point, ending point and maxHeight
        /// </summary>
        Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
        {
            float parabolicT = t * 2 - 1;
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += (-parabolicT * parabolicT + 1) * height;

            return result;
        }

    }
}
