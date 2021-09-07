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
