using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultDown : VaultAction
    {
        float timeDrop = 0;

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
                        if (!Physics.Raycast(origin, Vector3.down, out hit, 1.5f, controller.characterDetection.environmentLayer.value) ||
                            Physics.Raycast(origin2, controller.transform.forward, out hit, 1.0f, controller.characterDetection.environmentLayer.value))
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
