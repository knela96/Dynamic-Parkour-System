/*
Dynamic Parkour System grants parkour capabilities to any humanoid character model.
Copyright (C) 2021  Èric Canela Sol
Contact: knela96@gmail.com or @knela96 twitter

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class CameraController : MonoBehaviour
    {
        private CinemachineCameraOffset cameraOffset;

        public Vector3 _offset;
        public Vector3 _default;
        private Vector3 _target;

        public float maxTime = 2.0f;
        private float curTime = 0.0f;
        private bool anim = false;


        void Start()
        {
            cameraOffset = GetComponent<CinemachineCameraOffset>();
        }


        void Update()
        {
            //Lerps Camera Position to the new offset
            if (anim)
            {
                curTime += Time.deltaTime / maxTime;
                cameraOffset.m_Offset = Vector3.Lerp(cameraOffset.m_Offset, _target, curTime);
            }

            if (curTime >= 1.0f)
                anim = false;
        }

        /// <summary>
        /// Adds Offset to the camera while being on Climbing or inGround
        /// </summary>
        public void newOffset(bool offset)
        {
            if (offset)
                _target = _offset;
            else
                _target = _default;

            anim = true;
            curTime = 0;
        }
    }
}