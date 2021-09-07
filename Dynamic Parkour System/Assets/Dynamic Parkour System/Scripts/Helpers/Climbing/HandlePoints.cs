using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [ExecuteInEditMode]
    public class HandlePoints : MonoBehaviour
    {
        [Header("Helper Properties")]
        public PointType pointType;
        [SerializeField] private GameObject pointPrefab;
        [SerializeField] private float posInterval = 0.5f;

        [Header("Update")]
        [SerializeField] private bool deleteAll;
        [SerializeField] private bool createIndicators;
        [SerializeField] private bool updatePoints;

        [Header("Helper Utilities")]
        public Point furthestLeft;
        public Point furthestRight;
        public List<Point> pointsInOrder;

        void HandlePrefab()
        {
            pointPrefab = Resources.Load("Climbing/GPoint") as GameObject;

            if (pointPrefab == null)
            {
                Debug.Log("No Point Prefab Found");
            }
        }

        void Update()
        {
            if (createIndicators)
            {
                createIndicators = false;
                HandlePrefab();
                DeteleAll();
                CreateIndicators();
                UpdatePoints();
            }

            if (updatePoints)
            {
                updatePoints = false;
                HandlePrefab();
                UpdatePoints();
            }

            if (deleteAll)
            {
                deleteAll = false;
                DeteleAll();
            }
        }

        void UpdatePoints()
        {
            Point[] ps = GetComponentsInChildren<Point>();

            if (ps.Length < 1)
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
                if (ps[i] != furthestLeft && ps[i] != furthestRight)
                {
                    Destroy(ps[i].gameObject.transform.gameObject);
                }
            }
        }

        void CreatePoints(Point furthestLeft, Point furthestRight)
        {
            float disLtoR = Vector3.Distance(furthestLeft.transform.position, furthestRight.transform.position);
            int pointCount = Mathf.FloorToInt(disLtoR / posInterval);
            Vector3 direction = furthestRight.transform.position - furthestLeft.transform.position;
            direction.Normalize();

            Vector3[] positions = new Vector3[pointCount];

            float interval = 0.0f;
            pointsInOrder = new List<Point>();
            pointsInOrder.Add(furthestLeft);

            for (int i = 0; i < pointCount; i++)
            {
                interval += posInterval;

                positions[i] = furthestLeft.transform.position + (direction * interval);

                if (Vector3.Distance(positions[i], furthestRight.transform.position) >= posInterval)
                {
                    GameObject p = Instantiate(pointPrefab, positions[i], furthestLeft.transform.rotation);
                    p.transform.parent = transform;
                    Point point = p.GetComponent<Point>();
                    point.type = pointType;
                    pointsInOrder.Add(point);
                }
                else
                {
                    furthestRight.transform.localPosition = transform.InverseTransformPoint(positions[i]);
                    pointsInOrder.Add(furthestRight);
                    break;
                }
            }
        }

        void DeteleAll()
        {
            Point[] ps = GetComponentsInChildren<Point>();

            for (int i = 0; i < ps.Length; i++)
            {
                DestroyImmediate(ps[i].transform.gameObject);
            }

            pointsInOrder.Clear();
        }

        void CreateIndicators()
        {
            GameObject leftPoint = Instantiate(pointPrefab) as GameObject;
            GameObject rightPoint = Instantiate(pointPrefab) as GameObject;

            leftPoint.transform.parent = transform;
            leftPoint.transform.localPosition = -(Vector3.right / 2) + new Vector3(0.1f, 0f, 0f);

            rightPoint.transform.parent = transform;
            rightPoint.transform.localPosition = (Vector3.right / 2) + new Vector3(-0.1f, 0f, 0f);

            leftPoint.transform.localEulerAngles = Vector3.zero;
            rightPoint.transform.localEulerAngles = Vector3.zero;

            furthestLeft = leftPoint.GetComponent<Point>();
            furthestRight = rightPoint.GetComponent<Point>();
            furthestLeft.type = pointType;
            furthestRight.type = pointType;
        }

        public Point GetClosestPoint(Vector3 playerPos)
        {
            Point ret = null;
            float minDist = float.PositiveInfinity;
            for (int i = 0; i < pointsInOrder.Count; i++)
            {
                if (pointsInOrder[i] == null)
                    continue;

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
