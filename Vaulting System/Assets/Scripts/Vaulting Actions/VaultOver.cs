using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class VaultOver : VaultAction
    {
        public Action action;

        public override void Initialize(ThirdPersonController _controller, Animator _animator, Action _actionInfo)
        {
            action = _actionInfo;
            controller = _controller;
            animator = _animator;
        }

        public override void Start()
        {
            clip = action.clip;
            kneeRaycastOrigin = action.kneeRaycastOrigin;
            kneeRaycastLength = action.kneeRaycastLength;
            landOffset = action.landOffset;
        }

        public override bool CheckAction()
        {
            RaycastHit hit;
            Vector3 origin = transform.position + kneeRaycastOrigin;

            if (Physics.Raycast(origin, transform.forward, out hit, kneeRaycastLength))
            {
                Vector3 origin2 = origin + (-hit.normal * (hit.transform.localScale.z + landOffset));

                Debug.DrawLine(origin, origin + transform.forward * kneeRaycastLength);//Forward Raycast
                Debug.DrawLine(hit.point, hit.point + hit.normal, Color.cyan); //Face Normal
                Debug.DrawLine(origin2, origin2 + Vector3.down);//Down Raycast

                // If direction not the same as object don't do anything
                // or angle of movement not valid
                if ((hit.normal == hit.collider.transform.forward || hit.normal == -hit.collider.transform.forward) == false ||
                    Mathf.Abs(Vector3.Dot(-hit.normal,transform.forward)) < 0.60) 
                    return false;

                RaycastHit hit2;
                if (Physics.Raycast(origin2, Vector3.down, out hit2, 10)) //Ground Hit
                {
                    if (hit.transform.tag == "Deep Jump" && hit2.collider)
                    {
                        controller.characterAnimation.animator.CrossFade("Deep Jump", 0.2f);
                        isVaulting = true;
                        startPos = transform.position;
                        startRot = transform.rotation;
                        targetPos = hit2.point;
                        targetRot = Quaternion.LookRotation(targetPos - startPos);
                        vaultTime = 0;
                        animLength = clip.length;
                        controller.DisableController();

                        return true;
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
                    transform.rotation = Quaternion.Lerp(startRot, targetRot, vaultTime * 4);
                    transform.position = Vector3.Lerp(startPos, targetPos, vaultTime);
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
