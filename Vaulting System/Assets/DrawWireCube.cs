using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    [ExecuteInEditMode]
    public class DrawWireCube : MonoBehaviour
    {
        public List<IKPositions> ikPos = new List<IKPositions>();
        public bool refresh;

        private void Update()
        {
            if(refresh)
            {
                ikPos.Clear();
                refresh = false;
            }
        }
    }

}

