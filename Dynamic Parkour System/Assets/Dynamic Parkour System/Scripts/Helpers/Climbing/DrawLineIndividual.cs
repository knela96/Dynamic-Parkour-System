using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [ExecuteInEditMode]
    public class DrawLineIndividual : MonoBehaviour
    {
        public List<Neighbour> ConnectedPoints = new List<Neighbour>();

        public bool refresh;

        void Update()
        {
            if (refresh)
            {
                ConnectedPoints.Clear();
                refresh = false;
            }
        }
    }

}
