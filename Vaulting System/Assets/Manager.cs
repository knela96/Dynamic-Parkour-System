using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class Manager : MonoBehaviour
    {
        public List<Point> allPoints = new List<Point>();


        void Start()
        {
            PopulateAllPoints();
        }

        public void Init()
        {
            PopulateAllPoints();
        }

        void PopulateAllPoints()
        {
            Point[] allP = GetComponentsInChildren<Point>();

            foreach(Point p in allP)
            {
                if (!allPoints.Contains(p))
                {
                    allPoints.Add(p);
                }
            }
        }

        public Point ReturnNeighbourPointFromDirection(Vector3 inputDirection, Point curPoint)
        {
            Point ret = null;
            foreach(Neighbour n in curPoint.neighbours)
            {
                if(n.direction == inputDirection)
                {
                    ret = n.target;
                }
            }
            return ret;
        }
        public Neighbour ReturnNeighbour(Vector3 inputDirection, Point curPoint)
        {
            Neighbour ret = null;
            foreach (Neighbour n in curPoint.neighbours)
            {
                if (n.direction == inputDirection)
                {
                    ret = n;
                }
            }
            return ret;
        }

        public Point ReturnClosest(Vector3 from)
        {
            Point ret = null;

            float min_dist = Mathf.Infinity;

            for(int i = 0; i < allPoints.Count; i++)
            {
                float dis = Vector3.Distance(allPoints[i].transform.position, from);
                if(dis < min_dist)
                {
                    ret = allPoints[i];
                    min_dist = dis;
                }
            }

            return ret;
        }
    }

}