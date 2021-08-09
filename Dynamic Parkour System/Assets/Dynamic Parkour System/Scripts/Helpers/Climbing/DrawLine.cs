using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [ExecuteInEditMode]
    public class DrawLine : MonoBehaviour
    {
        public List<Connection> ConnectedPoints = new List<Connection>();

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
