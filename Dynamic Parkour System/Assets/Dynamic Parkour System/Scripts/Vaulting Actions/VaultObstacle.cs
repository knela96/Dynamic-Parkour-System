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
    public class VaultObstacle : VaultAction
    {
        private Vector3 leftHandPosition;
        private Quaternion leftHandRotation;
        private string HandAnimVariableName;

        public VaultObstacle(ThirdPersonController _vaultingController, Action _actionInfo) : base(_vaultingController, _actionInfo)
        {
            ActionVaultObstacle action = (ActionVaultObstacle)_actionInfo;

            //Loads Action Info
            HandAnimVariableName = action.HandAnimVariableName;
        }

        /// <summary>
        /// Checks if can Vault the Fence Obstacle
        /// </summary>
        public override bool CheckAction()
        {
            if (controller.characterInput.jump && !controller.isVaulting)
            {
                RaycastHit hit;
                Vector3 origin = controller.transform.position + kneeRaycastOrigin;

                //Checks if Vault obstacle in front
                if (controller.characterDetection.ThrowRayOnDirection(origin, controller.transform.forward, kneeRaycastLength, out hit))
                {
                    // If direction not the same as object don't do anything
                    // or angle of movement not valid
                    if ((hit.normal == hit.collider.transform.forward || 
                        hit.normal == -hit.collider.transform.forward) == false
                        || hit.transform.tag != tag)
                        return false;


                    //Gets Fence width and adds an offset for the downward ray
                    Vector3 origin2 = origin + (-hit.normal * (hit.transform.localScale.z + landOffset));

                    RaycastHit hit2;
                    //Get landing position
                    if (controller.characterDetection.ThrowRayOnDirection(origin2, Vector3.down, 10, out hit2))
                    {
                        if (hit2.collider)
                        {
                            controller.characterAnimation.animator.CrossFade("Vaulting", 0.2f);

                            startPos = controller.transform.position;
                            startRot = controller.transform.rotation;
                            targetPos = hit2.point;
                            targetRot = Quaternion.LookRotation(targetPos - startPos);
                            vaultTime = startDelay; //This adds a delay to allow animation start in correct time
                            animLength = clip.length + startDelay;
                            controller.DisableController();

                            //Calculate Hand Rest Position n Rotation
                            Vector3 left = Vector3.Cross(hit.normal, Vector3.up);
                            leftHandPosition = hit.point + (-hit.normal * (hit.transform.localScale.z / 2));
                            leftHandPosition.y = hit.transform.position.y + hit.transform.localScale.y / 2;
                            leftHandPosition.x += left.x * animator.animator.GetBoneTransform(HumanBodyBones.LeftHand).localPosition.x;
                            leftHandRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

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
            if (controller.isVaulting)
            {
                float actualSpeed = Time.deltaTime / animLength;
                vaultTime += actualSpeed * animator.animState.speed;

                if (vaultTime > 1)
                {
                    controller.EnableController();
                }
                else
                {
                    if (vaultTime >= 0)
                    {
                        controller.transform.rotation = Quaternion.Lerp(startRot, targetRot, vaultTime * 4);
                        controller.transform.position = Vector3.Lerp(startPos, targetPos, vaultTime);
                    }
                    return true;
                }
            }

            return false;
        }

        public override void OnAnimatorIK(int layerIndex)
        {
            if (!controller.isVaulting)
                return;

            float curve = animator.animator.GetFloat(HandAnimVariableName);
            animator.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, curve);
            animator.animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
            animator.animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, curve);
            animator.animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRotation);
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(targetPos, 0.08f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(leftHandPosition, 0.08f);
        }
    }
}
