using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Climbing
{
    [ExecuteInEditMode]
    public class HandlePointConnection : MonoBehaviour
    {
        public float maxDistance = 1.5f;
        public float minDistance = 0.5f;
        public bool updateConnections;
        public bool resetConnections;

        public List<Point> allPoints = new List<Point>();
        Vector3[] availableDirections = new Vector3[8];
        public float validAngleRange = 22.5f;

        //Directions to Connect Climb Points
        void CreateDirections()
        {
            availableDirections[0] = new Vector3(1, 0, 0);
            availableDirections[1] = new Vector3(-1, 0, 0);
            availableDirections[2] = new Vector3(0, 1, 0);
            availableDirections[3] = new Vector3(0, -1, 0);
            availableDirections[4] = new Vector3(-1, -1, 0);
            availableDirections[5] = new Vector3(1, 1, 0);
            availableDirections[6] = new Vector3(1, -1, 0);
            availableDirections[7] = new Vector3(-1, 1, 0);
        }

        void Update()
        {
            if (updateConnections)
            {
                GetPoints();
                CreateDirections();
                CreateConnections();
                RefreshAll();
                updateConnections = false;
            }

            if (resetConnections)
            {
                GetPoints();
                for(int p = 0; p < allPoints.Count; p++)
                {
                    allPoints[p].neighbours.Clear();
                }
                RefreshAll();
                resetConnections = false;
            }
        }

        //Get all Child Points
        void GetPoints()
        {
            allPoints.Clear();
            Point[] hp = GetComponentsInChildren<Point>();
            allPoints.AddRange(hp);
        }

        void CreateConnections()
        {
            for(int p = 0; p < allPoints.Count; p++)
            {
                Point curPoint = allPoints[p]; 
                CandidatePointsOnDirection(curPoint);
            }
        }

        //Connects all points near with all neighbours on all directions
        void CandidatePointsOnDirection(Point from)
        {
            for (int p = 0; p < allPoints.Count; p++)
            {
                Point targetPoint = allPoints[p];
                float dis = Vector3.Distance(from.transform.position, targetPoint.transform.position);
                if (dis < maxDistance && dis > minDistance /*&& from.transform.parent != targetPoint.transform.parent*/)
                {
                    Vector3 direction = targetPoint.transform.position - from.transform.position;
                    Vector3 relativeDirection = from.transform.InverseTransformDirection(direction);
                    relativeDirection.z = 0;

                    for (int d = 0; d < availableDirections.Length; d++)
                    {
                        if(IsDirectionValid(availableDirections[d], relativeDirection))
                            AddNeighbour(from, targetPoint, availableDirections[d]);
                    }
                }
            }
        }

        public bool IsDirectionValid(Vector3 targetDirection, Vector3 candidate)
        {
            bool ret = false;

            float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.y) * Mathf.Rad2Deg;
            float angle = Mathf.Atan2(candidate.x, candidate.y) * Mathf.Rad2Deg;
            if (targetAngle < 0)
                targetAngle += 360;

            if (angle < 0)
                angle += 360;

            if (angle <= targetAngle + validAngleRange && angle >= targetAngle - validAngleRange)
            {
                ret = true;
            }

            return ret;
        }

        public Vector2 IsDirectionAngleValid(Vector3 inputDirection, Vector3 pointDirection)
        {
            Vector2 angles = Vector2.zero;

            if (inputDirection == Vector3.zero) //No Input Direction
                return Vector2.zero;

            //Get Angle of direction + Fix negative Atan2 negative angles
            float inputAngle = (Mathf.Atan2(inputDirection.x, inputDirection.y) + ((inputDirection.x < 0) ? 2 * Mathf.PI : 0)) * Mathf.Rad2Deg;
            float pointAngle = (Mathf.Atan2(pointDirection.x, pointDirection.y) + ((pointDirection.x < 0) ? 2 * Mathf.PI : 0)) * Mathf.Rad2Deg;

            if ((pointAngle <= inputAngle + validAngleRange && pointAngle >= inputAngle - validAngleRange) ||
                pointAngle <= inputAngle + validAngleRange + 360 && pointAngle >= (inputAngle - validAngleRange + 360) % 360)
            {
                angles = new Vector2(pointAngle, inputAngle);
            }

            return angles;
        }

        //Creates 1 connection between a point with another
        void AddNeighbour(Point from, Point target, Vector3 direction)
        {
            Neighbour n = new Neighbour();
            n.target = target;
            n.direction = direction;
            from.neighbours.Add(n);

            #if UNITY_EDITOR
                EditorUtility.SetDirty(from);
            #endif
        }

        void RefreshAll()
        {
            DrawLine dl = transform.GetComponent<DrawLine>();

            if (dl)
                dl.refresh = true;
        }

        public List<Connection> GetAllConnections()
        {
            List<Connection> ret = new List<Connection>();

            for(int i = 0; i < allPoints.Count; i++)
            {
                for(int j = 0; j < allPoints[i].neighbours.Count; j++)
                {
                    Connection con = new Connection();
                    con.target1 = allPoints[i];
                    con.target2 = allPoints[i].neighbours[j].target;

                    if(!ContainsConnection(ret, con))
                    {
                        ret.Add(con);
                    }
                }
            }

            return ret;
        }

        bool ContainsConnection(List<Connection> l, Connection c)
        {
            bool ret = false;

            for(int i = 0; i <l.Count; i++)
            {
                if(l[i].target1 == c.target1 && l[i].target2 == c.target2 || 
                   l[i].target2 == c.target1 && l[i].target1 == c.target2)
                {
                    ret = true;
                    break;
                }
            }
            return ret;
        }        
    }

    public class Connection
    {
        public Point target1;
        public Point target2;
    }

}
