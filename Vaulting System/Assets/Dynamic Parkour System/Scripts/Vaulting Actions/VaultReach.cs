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

        public VaultReach(ThirdPersonController _vaultingController, Action _actionInfo) : base(_vaultingController, _actionInfo)
        {
            ActionVaultReach action = (ActionVaultReach)_actionInfo;

            maxHeight = action.maxHeight;
            midHeight = action.midHeight;

            HandAnimVariableName = action.HandAnimVariableName;
        }

        public override bool CheckAction()
        {
            if (controller.characterInput.jump && !controller.isVaulting && !controller.isJumping && controller.isGrounded)
            {
                RaycastHit hit;
                //Vector3 origin = vaultingController.transform.position + kneeRaycastOrigin;
                Vector3 origin = controller.transform.position + Vector3.up * controller.stepHeight;

                if (Physics.Raycast(origin, controller.transform.forward, out hit, kneeRaycastLength, layer.value))
                {
                    if (hit.collider.gameObject.tag != tag)
                        return false;

                    Vector3 origin2 = hit.point + (-hit.normal * (landOffset)) + new Vector3(0, 5, 0);

                    RaycastHit hit2;
                    RaycastHit hit3;
                    Physics.Raycast(controller.transform.position, Vector3.down, out hit3, 1);
                    if (Physics.Raycast(origin2, Vector3.down, out hit2, 10, layer.value)) //Ground Hit
                    {
                        height = hit2.point.y - controller.transform.position.y;

                        if (hit.collider.gameObject.tag != tag || height > maxHeight || hit2.collider != hit.collider || hit3.collider == hit2.collider)
                            return false;

                        if (hit2.collider)
                        {
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
            Gizmos.DrawSphere(leftHandPosition, 10);
        }
    }
}
