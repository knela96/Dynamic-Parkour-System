using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [System.Serializable]
    public class Neighbour
    {
        public Vector3 direction;
        public Point target;
    }

    public enum PointType
    {
        Ledge = 0,
        Pole,
        Ground
    }

    [System.Serializable]
    public class Point : MonoBehaviour
    {
        public List<Neighbour> neighbours = new List<Neighbour>();
        public PointType type = PointType.Ledge;

        public Neighbour ReturnNeighbour(Point target)
        {
            Neighbour retVal = null;

            for (int i = 0; i < neighbours.Count; i++)
            {
                if (neighbours[i].target == target)
                {
                    retVal = neighbours[i];
                    break;
                }
            }
            return retVal;
        }
    }
}

