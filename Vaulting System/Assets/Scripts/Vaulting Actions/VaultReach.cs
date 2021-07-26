using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultReach : VaultAction
    {
        float maxHeight = 0;

        public VaultReach(VaultingController _vaultingController, Action _actionInfo) : base(_vaultingController, _actionInfo)
        {
            ActionVaultReach action = (ActionVaultReach)_actionInfo;

            maxHeight = action.maxHeight;
        }

        public override bool CheckAction()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                RaycastHit hit;
                Vector3 origin = vaultingController.transform.position + kneeRaycastOrigin;

                if (Physics.Raycast(origin, vaultingController.transform.forward, out hit, kneeRaycastLength, layer.value))
                {
                    if (hit.collider.gameObject.tag != tag)
                        return false;

                    Vector3 origin2 = hit.point + (-hit.normal * (landOffset)) + new Vector3(0, 2, 0);

                    RaycastHit hit2;
                    if (Physics.Raycast(origin2, Vector3.down, out hit2, 10)) //Ground Hit
                    {
                        if (hit2.point.y - vaultingController.transform.position.y > maxHeight)
                            return false;

                        if (hit2.collider)
                        {
                            controller.characterAnimation.animator.CrossFade("Reach", 0.1f);
                            isVaulting = true;
                            startPos = vaultingController.transform.position;
                            startRot = vaultingController.transform.rotation;
                            targetPos = hit2.point;
                            targetRot = Quaternion.LookRotation(new Vector3(targetPos.x - startPos.x, 0, targetPos.z - startPos.z));
                            vaultTime = startDelay;
                            animLength = clip.length + startDelay;
                            controller.DisableController();

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

                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Reach"))
                {
                    vaultingController.controller.characterAnimation.SetMatchTarget(AvatarTarget.RightFoot, targetPos, targetRot, Vector3.zero, 0, 0.5f);

                    if (animator.IsInTransition(0))
                    {
                        controller.EnableController();
                        isVaulting = false;
                        ret = false;
                    }
                }
            }

            return ret;
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(targetPos, 10);
        }
    }
}
