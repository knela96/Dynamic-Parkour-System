using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Climbing
{
    [System.Serializable]
    public class Point : MonoBehaviour
    {
        public List<Neighbour> neighbours = new List<Neighbour>();
        public List<IKPositions> iks = new List<IKPositions>();

        public IKPositions ReturnIK(AvatarIKGoal goal)
        {
            IKPositions retVal = null;

            for(int i = 0; i < iks.Count; i++)
            {
                if(iks[i].ik == goal)
                {
                    retVal = iks[i];
                    break;
                }
            }
            return retVal;
        }

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

    [System.Serializable]
    public class Neighbour
    {
        public Vector3 direction;
        public Point target;
        public ConnectionType type;
    }
    [System.Serializable]
    public class IKPositions
    {
        public AvatarIKGoal ik;
        public Transform target;
        public Transform hint;
    }

    public enum ConnectionType
    {
        inBetween,
        direct,
        dismount,
        fall
    }
}

