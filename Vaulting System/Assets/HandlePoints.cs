using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [ExecuteInEditMode]
    public class HandlePoints : MonoBehaviour
    {
        [Header("Helper Properties")]
        public bool dismountPoint;
        public bool fallPoint;
        public bool HangingPoints;
        public bool singlePoint;

        [Header("Update")]
        public bool updatePoints;

        [Header("Helper Utilities")]
        public bool deleteAll;
        public bool createIndicators;

        public GameObject pointPrefab;
        public float posInterval = 0.5f;

        public Point furthestLeft;
        public Point furthestRight;

        //[HideInInspector]
        public List<Point> pointsInOrder;

        void HandlePrefab()
        {
            pointPrefab = Resources.Load("Point") as GameObject;

            if (pointPrefab == null)
            {
                Debug.Log("No Point Prefab Found");
            }
        }

        void Update()
        {
            if (updatePoints)
            {
                HandlePrefab();
                UpdatePoints();
                updatePoints = false;
            }

            if (createIndicators)
            {
                HandlePrefab();

                if (!singlePoint)
                    CreateIndicators();
                else
                    CreateIndicators_Single();

                createIndicators = false;
            }

            if (deleteAll)
            {
                DeteleAll();
                deleteAll = false;
            }
        }

        void UpdatePoints()
        {
            Point[] ps = GetComponentsInChildren<Point>();

            if (singlePoint)
            {
                pointsInOrder = new List<Point>();

                for (int i = 0; i < ps.Length; i++)
                {
                    pointsInOrder.Add(ps[i]);
                }
                return;
            }

            if(ps.Length < 1)
            {
                Debug.Log("No edges Indicators Found");
                return;
            }

            DeletePrevious(ps, furthestLeft, furthestRight);

            ps = null;
            ps = GetComponentsInChildren<Point>();

            CreatePoints(furthestLeft, furthestRight);

        }

        void DeletePrevious(Point[] ps, Point furthestLeft, Point furthestRight)
        {
            for (int i = 0; i < ps.Length; i++)
            {
                if(ps[i] != furthestLeft && ps[i] != furthestRight)
                {
                    DestroyImmediate(ps[i].gameObject.transform.parent.gameObject);
                }
            }
        }

        void CreatePoints(Point furthestLeft, Point furthestRight)
        {
            float disLtoR = Vector3.Distance(GetPos(furthestLeft), GetPos(furthestRight));
            int pointCount = Mathf.FloorToInt(disLtoR / posInterval);
            Vector3 direction = GetPos(furthestRight) - GetPos(furthestLeft);
            direction.Normalize();
            Vector3[] positions = new Vector3[pointCount];

            float interval = 0.0f;
            pointsInOrder = new List<Point>();
            pointsInOrder.Add(furthestLeft);

            for (int i = 0; i < pointCount; i++)
            {
                interval += posInterval;

                positions[i] = GetPos(furthestLeft) + (direction * interval);

                if(Vector3.Distance(positions[i], GetPos(furthestRight)) >= posInterval)
                {
                    GameObject p = Instantiate(pointPrefab, positions[i], Quaternion.identity) as GameObject;
                    p.transform.parent = transform;
                    pointsInOrder.Add(p.GetComponentInChildren<Point>());
                }
                else
                {
                    furthestRight.transform.parent.transform.localPosition = transform.InverseTransformPoint(positions[i]);
                    pointsInOrder.Add(furthestRight);
                    break;
                }
            }
        }

        Vector3 GetPos(Point p)
        {
            return p.transform.parent.position;
        }

        void DeteleAll()
        {
            Point[] ps = GetComponentsInChildren<Point>();

            for (int i = 0; i < ps.Length; i++)
            {
                DestroyImmediate(ps[i].transform.parent.gameObject);
            }
        }

        void CreateIndicators()
        {
            GameObject leftPoint = Instantiate(pointPrefab) as GameObject;
            GameObject rightPoint = Instantiate(pointPrefab) as GameObject;

            leftPoint.transform.parent = transform;
            leftPoint.transform.localPosition = -(Vector3.right/2);

            rightPoint.transform.parent = transform;
            rightPoint.transform.localPosition = Vector3.right / 2;

            leftPoint.transform.localEulerAngles = Vector3.zero;
            rightPoint.transform.localEulerAngles = Vector3.zero;

            furthestLeft = leftPoint.GetComponentInChildren<Point>();
            furthestRight = rightPoint.GetComponentInChildren<Point>();
        }
        void CreateIndicators_Single()
        {
            GameObject leftPoint = Instantiate(pointPrefab) as GameObject;
            leftPoint.transform.parent = transform;
            leftPoint.transform.localPosition = Vector3.zero;
            leftPoint.transform.localEulerAngles = Vector3.zero;    
        }

        public Point GetClosestPoint(Vector3 playerPos)
        {
            Point ret = null;
            float minDist = float.PositiveInfinity;
            for (int i = 0; i < pointsInOrder.Count; i++)
            {
                float dist = Vector3.Distance(pointsInOrder[i].transform.position, playerPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    ret = pointsInOrder[i];
                }
            }

            return ret;
        }
    }

}
