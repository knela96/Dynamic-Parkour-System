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
