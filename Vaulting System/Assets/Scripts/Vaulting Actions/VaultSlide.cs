using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultSlide : VaultAction
    {
        float dis;
        public VaultSlide(VaultingController _vaultingController, Action _actionInfo) : base(_vaultingController, _actionInfo)
        {
        }

        public override bool CheckAction()
        {
            if (Input.GetKey(KeyCode.C) && !isVaulting)
            {
                RaycastHit hit;
                Vector3 origin = vaultingController.transform.position + kneeRaycastOrigin;

                if (Physics.Raycast(origin, vaultingController.transform.forward, out hit, kneeRaycastLength))
                {
                    Vector3 origin2 = origin + (-hit.normal * (hit.transform.localScale.z + landOffset));

                    Debug.DrawLine(origin, origin + vaultingController.transform.forward * kneeRaycastLength);//Forward Raycast

                    if ((hit.normal == hit.collider.transform.forward ||
                        hit.normal == -hit.collider.transform.forward) == false ||
                        Mathf.Abs(Vector3.Dot(-hit.normal, vaultingController.transform.forward)) < 0.60 ||
                        hit.transform.tag != tag)
                        return false;

                    RaycastHit hit2;
                    if (Physics.Raycast(origin2, Vector3.down, out hit2, 10)) //Ground Hit
                    {
                        if (hit2.collider)
                        {
                            controller.characterAnimation.animator.CrossFade("Running Slide", 0.2f);
                            dis = 4 / Vector3.Distance(startPos, targetPos);
                            controller.characterAnimation.animator.SetFloat("AnimSpeed", dis);
                            controller.characterAnimation.switchCameras.SlideCam();

                            isVaulting = true;
                            startPos = vaultingController.transform.position;
                            startRot = vaultingController.transform.rotation;
                            targetPos = hit2.point;
                            targetRot = Quaternion.LookRotation(targetPos - startPos);
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
                float actualSpeed = Time.deltaTime / animLength;
                vaultTime += actualSpeed * (animator.GetCurrentAnimatorStateInfo(0).speed + dis);

                if (vaultTime > 1)
                {
                    controller.characterAnimation.animator.SetFloat("AnimSpeed",1);
                    controller.characterAnimation.switchCameras.FreeLookCam();
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

        public override void DrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPos, 0.08f);
        }
    }
}
