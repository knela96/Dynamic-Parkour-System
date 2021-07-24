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
        [Range(0, 1f)] private float handToIKPositionSpeed;

        public VaultObstacle(VaultingController _vaultingController, Action _actionInfo) : base(_vaultingController)
        {
            ActionVaultObstacle action = (ActionVaultObstacle)_actionInfo;

            //Loads Action Info
            clip = action.clip;
            kneeRaycastOrigin = action.kneeRaycastOrigin;
            kneeRaycastLength = action.kneeRaycastLength;
            landOffset = action.landOffset;

            HandAnimVariableName = action.HandAnimVariableName;
            handToIKPositionSpeed = action.handToIKPositionSpeed;
        }

        public override bool CheckAction()
        {
            if (Input.GetKey(KeyCode.Space)){
                RaycastHit hit;
                Vector3 origin = vaultingController.transform.position + kneeRaycastOrigin;

                if (Physics.Raycast(origin, vaultingController.transform.forward, out hit, kneeRaycastLength))
                {
                    if ((hit.normal == hit.collider.transform.forward || 
                        hit.normal == -hit.collider.transform.forward) == false
                        || hit.transform.tag != "Vault")
                        return false;

                    Vector3 origin2 = origin + vaultingController.transform.forward * kneeRaycastLength;

                    RaycastHit hit2;
                    if (Physics.Raycast(origin2, Vector3.down, out hit2, 10)) //Ground Hit
                    {
                        if (hit2.collider)
                        {
                            controller.characterAnimation.animator.CrossFade("Vaulting", 0.2f);
                            isVaulting = true;
                            startPos = vaultingController.transform.position;
                            startRot = vaultingController.transform.rotation;
                            targetPos = hit2.point + (-hit.normal * (hit.transform.localScale.z + landOffset));
                            targetRot = Quaternion.LookRotation(targetPos - startPos);
                            vaultTime = 0;
                            animLength = clip.length;
                            controller.DisableController();

                            //Calculate Hand Rest Position n Rotation
                            Vector3 left = Vector3.Cross(hit.normal, Vector3.up);
                            leftHandPosition = hit.point + (-hit.normal * (hit.transform.localScale.z / 2));
                            leftHandPosition.y = hit.transform.position.y + hit.transform.localScale.y / 2;
                            leftHandPosition.x += left.x * animator.GetBoneTransform(HumanBodyBones.LeftHand).localPosition.x;
                            leftHandRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

                            return true;
                        }
                    }
                }
            }            

            return false;
        }

        public override bool ExecuteAction()
        {
            bool ret = false;
            if (isVaulting)
            {
                float actualSpeed = Time.deltaTime / animLength;
                vaultTime += actualSpeed * animator.GetCurrentAnimatorStateInfo(0).speed;

                if (vaultTime > 1)
                {
                    isVaulting = false;
                    controller.EnableController();
                }
                else
                {
                    vaultingController.transform.rotation = Quaternion.Lerp(startRot, targetRot, vaultTime * 4);
                    vaultingController.transform.position = Vector3.Lerp(startPos, targetPos, vaultTime);
                    ret = true;
                }
            }

            return ret;
        }

        public override void OnAnimatorIK(int layerIndex)
        {
            if (!isVaulting)
                return;

            float curve = animator.GetFloat(HandAnimVariableName);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, curve);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, curve);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRotation);
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(targetPos, 0.08f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(leftHandPosition, 0.08f);
        }
    }
}
