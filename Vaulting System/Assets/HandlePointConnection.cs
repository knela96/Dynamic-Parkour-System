using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [ExecuteInEditMode]
    public class HandlePointConnection : MonoBehaviour
    {
        public float minDistance = 2.5f;
        public float directThreshold = 1.0f;
        public bool updateConnections;
        public bool resetConnections;

        List<Point> allPoints = new List<Point>();
        Vector3[] availableDirections = new Vector3[8];

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
                FindDismountCandidates();
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
                for(int d = 0; d < availableDirections.Length; d++)
                {
                    List<Point> candidatePoints = CandidatePointsOnDirection(availableDirections[d], curPoint);

                    Point closest = ReturnClosest(candidatePoints, curPoint);

                    if(closest != null)
                    {
                        if(Vector3.Distance(curPoint.transform.position, closest.transform.position) < minDistance)
                        {
                            //Skip diagonal anchors
                            if(Mathf.Abs(availableDirections[d].y) > 0 &&
                                Mathf.Abs(availableDirections[d].x) > 0)
                            {
                                if(Vector3.Distance(curPoint.transform.position, closest.transform.position) > directThreshold)
                                {
                                    continue;
                                }
                            }
                            AddNeighbour(curPoint, closest, availableDirections[d]);
                        }
                    }
                
                }
            }
        }

        List<Point> CandidatePointsOnDirection(Vector3 targetDirection, Point from)
        {
            List<Point> retVal = new List<Point>();

            for (int p = 0; p < allPoints.Count; p++)
            {
                Point targetPoint = allPoints[p];

                Vector3 direction = targetPoint.transform.position - from.transform.position;
                Vector3 relativeDirection = from.transform.InverseTransformDirection(direction);

                if(IsDirectionValid(targetDirection, relativeDirection))
                {
                    retVal.Add(targetPoint);
                }
            }
            return retVal;
        }

        Point ReturnClosest(List<Point> l, Point from)
        {
            Point ret = null;
            float minDist = Mathf.Infinity;

            for(int i = 0; i < l.Count; i++)
            {
                float temp = Vector3.Distance(l[i].transform.position, from.transform.position);

                if(temp < minDist && l[i] != from)
                {
                    minDist = temp;
                    ret = l[i];
                }
            }

            return ret;
        }

        bool IsDirectionValid(Vector3 targetDirection, Vector3 candidate)
        {
            bool ret = false;

            float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.y) * Mathf.Rad2Deg;
            float angle = Mathf.Atan2(candidate.x, candidate.y) * Mathf.Rad2Deg;

            if(angle < targetAngle + 22.5f && angle > targetAngle - 22.5f)
            {
                ret = true;
            }

            return ret;
        }

        void AddNeighbour(Point from, Point target, Vector3 targetDir)
        {
            Neighbour n = new Neighbour();
            n.direction = targetDir;
            n.target = target;
            n.type = (Vector3.Distance(from.transform.position, target.transform.position) < directThreshold) ?
                ConnectionType.inBetween : ConnectionType.direct;
            from.neighbours.Add(n);
            UnityEditor.EditorUtility.SetDirty(from);
        }

        void RefreshAll()
        {
            DrawLine dl = transform.GetComponent<DrawLine>();

            if (dl != null)
                dl.refresh = true;

            for(int i = 0; i < allPoints.Count; i++)
            {
                DrawLineIndividual d = allPoints[i].transform.GetComponent<DrawLineIndividual>();
                if (d != null)
                    d.refresh = true;
            }
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
                    con.type = allPoints[i].neighbours[j].type;

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

        void FindDismountCandidates()
        {
            GameObject dismountPrefab = Resources.Load("Dismount") as GameObject;
            if(dismountPrefab == null)
            {
                Debug.Log("No Dismount Prefab Found");
                return;
            }
            HandlePoints[] hp = GetComponentsInChildren<HandlePoints>();
            List<Point> candidates = new List<Point>();

            for(int i = 0; i < hp.Length; i++)
            {
                if(hp[i].dismountPoint)
                {
                    candidates.AddRange(hp[i].pointsInOrder);
                }
            }

            if (candidates.Count > 0)
            {
                GameObject parent = new GameObject();
                parent.name = "Dismount Points";
                parent.transform.parent = transform;
                parent.transform.localPosition = Vector3.zero;
                parent.transform.position = candidates[0].transform.localPosition;

                foreach(Point p in candidates)
                {
                    Transform worldP = p.transform.parent;
                    GameObject dismountObject = Instantiate(dismountPrefab, worldP.position, worldP.rotation) as GameObject;

                    Vector3 targetPos = worldP.position + ((worldP.forward / 1.6f) + Vector3.up * 1.2f);
                    dismountObject.transform.position = targetPos;

                    Point dismountPoint = dismountObject.GetComponentInChildren<Point>();

                    Neighbour n = new Neighbour();
                    n.direction = Vector3.up;
                    n.target = dismountPoint;
                    n.type = ConnectionType.dismount;
                    p.neighbours.Add(n);

                    Neighbour n2 = new Neighbour();
                    n2.direction = -Vector3.up;
                    n2.target = p;
                    n2.type = ConnectionType.dismount;
                    dismountPoint.neighbours.Add(n2);

                    dismountObject.transform.parent = parent.transform;
                }
            }
        }
    }

    public class Connection
    {
        public Point target1;
        public Point target2;
        public ConnectionType type;
    }

}
