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
using Cinemachine;
using Cinemachine.Utility;

namespace Climbing
{
    public class SwitchCameras : MonoBehaviour
    {
        // Start is called before the first frame update
        Animator animator;

        enum CameraType
        {
            None,
            Freelook,
            Slide
        }

        CameraType curCam = CameraType.None;

        [SerializeField] private CinemachineFreeLook FreeLook;
        [SerializeField] private CinemachineVirtualCamera Slide;


        void Start()
        {
            animator = GetComponent<Animator>();

            FreeLookCam();
        }

        //Switches To FreeLook Cam
        public void FreeLookCam()
        {
            if (curCam != CameraType.Freelook)
            {
                Slide.Priority = 0;
                FreeLook.Priority = 1;
            }
        }

        //Switches To Slide Cam
        public void SlideCam()
        {
            if (curCam != CameraType.Slide)
            {
                FreeLook.Priority = 0;
                Slide.Priority = 1;
            }
        }
    }
}
