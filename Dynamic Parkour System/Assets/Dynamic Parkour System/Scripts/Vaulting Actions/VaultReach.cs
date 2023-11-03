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
    public class VaultReach : VaultAction
    {
        private float maxHeight = 0;
        private float height = 0;
        private Vector3 leftHandPosition;
        private string HandAnimVariableName;

        public VaultReach(ThirdPersonController _vaultingController, Action _actionInfo) : base(_vaultingController, _actionInfo)
        {
            ActionVaultReach action = (ActionVaultReach)_actionInfo;
            maxHeight = action.maxHeight;
            HandAnimVariableName = action.HandAnimVariableName;
        }

        public override bool CheckAction()
        {
            //Checks if the front obstacle tall enough to climb
            if (controller.characterInput.jump && !controller.isVaulting && !controller.isJumping && controller.isGrounded)
            {
                Vector3 origin = controller.transform.position + Vector3.up * controller.stepHeight;

                RaycastHit hit;
                if (controller.characterDetection.ThrowRayOnDirection(origin, controller.transform.forward, kneeRaycastLength, out hit, controller.characterDetection.climbLayer))
                {
                    if (hit.collider.gameObject.tag != tag)
                        return false;

                    Vector3 origin2 = hit.point + (-hit.normal * (landOffset)) + new Vector3(0, 5, 0);

                    RaycastHit hit2;
                    RaycastHit hit3;
                    controller.characterDetection.ThrowRayOnDirection(controller.transform.position, Vector3.down, 1, out hit3);
                    if (controller.characterDetection.ThrowRayOnDirection(origin2, Vector3.down, 10, out hit2, controller.characterDetection.climbLayer)) //Ground Hit
                    {
                        height = hit2.point.y - controller.transform.position.y;

                        //Avoids Climbing the same Obstacle like a Slope
                        if (hit.collider.gameObject.tag != tag || height > maxHeight || hit2.collider != hit.collider || hit3.collider == hit2.collider)
                            return false;

                        if (hit2.collider)
                        {
                            //Depending on the height of Obstacle Execute one animation or another
                            if(height <= 1)
                                controller.characterAnimation.animator.CrossFade("Reach", 0.1f);
                            else
                                controller.characterAnimation.animator.CrossFade("Reach High", 0.1f);

                            startPos = controller.transform.position;
                            startRot = controller.transform.rotation;
                            targetPos = hit2.point;
                            targetRot = Quaternion.LookRotation(-hit.normal);
                            vaultTime = 0;
                            animLength = clip.length + startDelay;
                            controller.DisableController();

                            //Calculate Hand Rest Position n Rotation
                            Vector3 right = Vector3.Cross(hit.normal, Vector3.up);
                            leftHandPosition = hit.point + (right * -0.5f);
                            leftHandPosition.y = hit2.point.y;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Executes Vaulting Animation
        /// </summary>
        public override bool Update()
        {
            bool ret = false;
            if (controller.isVaulting)
            {
                ret = true;

                float actualSpeed = Time.deltaTime / animLength;
                vaultTime += actualSpeed * animator.animState.speed;
                controller.transform.rotation = Quaternion.Lerp(startRot, targetRot, vaultTime * 4);

                if (animator.animState.IsName("Reach") || animator.animState.IsName("Reach High"))
                {
                    if (height <= 1)
                        controller.characterAnimation.SetMatchTarget(AvatarTarget.Root, targetPos, targetRot, Vector3.zero, 0, 1.0f);
                    else
                        controller.characterAnimation.SetMatchTarget(AvatarTarget.Root, targetPos, targetRot, Vector3.zero, 0, 0.25f);

                    //Animation Ended, set values to Normal
                    if (animator.animator.IsInTransition(0) && vaultTime > 0.5f)
                    {
                        controller.ToggleWalk();
                        controller.EnableController();
                        height = 0;
                        ret = false;
                    }
                }
            }

            return ret;
        }

        public override void OnAnimatorIK(int layerIndex)
        {
            if (height <= 1 || !controller.isVaulting)
                return;

            float curve = animator.animator.GetFloat(HandAnimVariableName);

            animator.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, curve);
            animator.animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
        }

        public override void DrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(leftHandPosition, 0.07f);
            Gizmos.DrawSphere(targetPos, 0.07f);
        }
    }
}
