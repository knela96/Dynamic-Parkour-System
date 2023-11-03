/*
MIT License

Copyright (c) 2023 Èric Canela
Contact: knela96@gmail.com or @knela96 twitter

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (Dynamic Parkour System), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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