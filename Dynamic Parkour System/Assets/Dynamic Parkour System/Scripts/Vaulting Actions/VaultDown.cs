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
    public class VaultDown : VaultAction
    {
        private float timeDrop = 0;

        public VaultDown(ThirdPersonController _vaultingController) : base(_vaultingController)
        {
        }

        public override bool CheckAction()
        {
            if (controller.isGrounded && !controller.isJumping && !controller.dummy)
            {
                //Checks if Player is in limit of a surface to Drop
                if (controller.characterMovement.limitMovement && controller.characterMovement.velLimit == 0 && timeDrop != -1 && !controller.isVaulting)
                {
                    timeDrop += Time.deltaTime;

                    //Checks if below surface is too low and denies auto drop
                    if (timeDrop > 0.15f)
                    {
                        Vector3 origin = controller.transform.position + controller.transform.forward * 1.0f;
                        Vector3 origin2 = controller.transform.position + Vector3.up * 0.1f;

                        RaycastHit hit;
                        if (!controller.characterDetection.ThrowRayOnDirection(origin, Vector3.down, 1.5f, out hit) ||
                             controller.characterDetection.ThrowRayOnDirection(origin2, controller.transform.forward, 1.0f, out hit))
                        {
                            timeDrop = 0;
                        }
                    }

                    //Drop if drop input or moving drop direction during 0.2s
                    if (controller.characterMovement.limitMovement && (controller.characterInput.drop || timeDrop > 0.15f) && controller.characterInput.movement != Vector2.zero)
                    {
                        animator.animator.CrossFade("Jump Down Slow", 0.1f);
                        timeDrop = -1;
                        controller.isJumping = true;
                        return true;
                    }
                }
                else
                {
                    timeDrop = 0;
                }
            }
            else
            {
                timeDrop = 0;
            }

            return false;
        }

        public override bool FixedUpdate()
        {
            bool ret = false;
            if (controller.isVaulting)
            {
                if (!controller.dummy && controller.isJumping)
                {
                    //Grants movement while falling
                    controller.characterMovement.rb.position += (controller.transform.forward * controller.characterMovement.walkSpeed) * Time.fixedDeltaTime;
                    ret = true;
                }
            }

            return ret;
        }

        public override void DrawGizmos()
        {
            Vector3 origin = controller.transform.position + controller.transform.forward * 1.0f;
            Vector3 origin2 = controller.transform.position + Vector3.up * 0.1f;

            Debug.DrawLine(origin, origin + Vector3.down * 1.5f);
            Debug.DrawLine(origin2, origin + controller.transform.forward * 0.5f);
        }
    }
}
