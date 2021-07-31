using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultReach : VaultAction
    {
        float maxHeight = 0;
        float midHeight = 0;

        float height = 0;


        private Vector3 leftHandPosition;

        private string HandAnimVariableName;

        public VaultReach(VaultingController _vaultingController, Action _actionInfo) : base(_vaultingController, _actionInfo)
        {
            ActionVaultReach action = (ActionVaultReach)_actionInfo;

            maxHeight = action.maxHeight;
            midHeight = action.midHeight;

            HandAnimVariableName = action.HandAnimVariableName;
        }

        public override bool CheckAction()
        {
            if (vaultingController.controller.characterInput.jump && !isVaulting)
            {
                RaycastHit hit;
                Vector3 origin = vaultingController.transform.position + kneeRaycastOrigin;

                if (Physics.Raycast(origin, vaultingController.transform.forward, out hit, kneeRaycastLength, layer.value))
                {
                    if (hit.collider.gameObject.tag != tag)
                        return false;

                    Vector3 origin2 = hit.point + (-hit.normal * (landOffset)) + new Vector3(0, 5, 0);

                    RaycastHit hit2;
                    if (Physics.Raycast(origin2, Vector3.down, out hit2, 10, layer.value)) //Ground Hit
                    {
                        height = hit2.point.y - vaultingController.transform.position.y;

                        if (hit.collider.gameObject.tag != tag || height > maxHeight || hit2.collider != hit.collider)
                            return false;

                        if (hit2.collider)
                        {
                            if(height <= 1)
                                controller.characterAnimation.animator.CrossFade("Reach", 0.1f);
                            else
                                controller.characterAnimation.animator.CrossFade("Reach High", 0.1f);

                            isVaulting = true;
                            startPos = vaultingController.transform.position;
                            startRot = vaultingController.transform.rotation;
                            targetPos = hit2.point;
                            targetRot = Quaternion.LookRotation(-hit.normal);
                            vaultTime = startDelay;
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

        public override bool ExecuteAction()
        {
            bool ret = false;
            if (isVaulting)
            {
                ret = true;

                float actualSpeed = Time.deltaTime / animLength;
                vaultTime += actualSpeed * animator.GetCurrentAnimatorStateInfo(0).speed;
                vaultingController.transform.rotation = Quaternion.Lerp(startRot, targetRot, vaultTime * 4);

                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Reach") || animator.GetCurrentAnimatorStateInfo(0).IsName("Reach High"))
                {
                    if (height <= 1)
                        vaultingController.controller.characterAnimation.SetMatchTarget(AvatarTarget.RightFoot, targetPos, targetRot, Vector3.zero, 0, 0.5f);
                    else
                        vaultingController.controller.characterAnimation.SetMatchTarget(AvatarTarget.RightFoot, targetPos, targetRot, Vector3.zero, 0.11f, 0.2f);


                    if (animator.IsInTransition(0) && vaultTime > 0.5f)
                    {
                        controller.ToggleWalk();
                        controller.EnableController();
                        isVaulting = false;
                        height = 0;
                        ret = false;
                    }
                }
            }

            return ret;
        }

        public override void OnAnimatorIK(int layerIndex)
        {
            if (height <= 1 || !isVaulting)
                return;

            float curve = animator.GetFloat(HandAnimVariableName);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, curve);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(leftHandPosition, 10);
        }
    }
}
