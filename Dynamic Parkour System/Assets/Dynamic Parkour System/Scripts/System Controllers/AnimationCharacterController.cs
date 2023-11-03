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
    [RequireComponent(typeof(ThirdPersonController))]
    [RequireComponent(typeof(Animator))]
    public class AnimationCharacterController : MonoBehaviour
    {
        private ThirdPersonController controller;
        private Vector3 animVelocity;

        [HideInInspector] public Animator animator;
        public SwitchCameras switchCameras;
        public AnimatorStateInfo animState;

        private MatchTargetWeightMask matchTargetWeightMask = new MatchTargetWeightMask(Vector3.one, 0);

        void Start()
        {
            controller = GetComponent<ThirdPersonController>();
            animator = GetComponent<Animator>();
            switchCameras = Camera.main.GetComponent<SwitchCameras>();
        }

        void Update()
        {
            animator.SetFloat("Velocity", animVelocity.magnitude);

            animState = animator.GetCurrentAnimatorStateInfo(0);

            if (animState.IsTag("Root") || animState.IsTag("Drop"))
            {
                animator.applyRootMotion = true;
            }
            else
            {
                animator.applyRootMotion = false;
            }
        }

        public void SetAnimVelocity(Vector3 value) { animVelocity = value; animVelocity.y = 0; }
        public Vector3 GetAnimVelocity() { return animVelocity; }

        public bool RootMotion() { return animator.applyRootMotion; }

        public void Fall()
        {
            animator.SetBool("Jump", false);
            animator.SetBool("onAir", true);
            animator.SetBool("Land", false);
            controller.characterMovement.DisableFeetIK();
        }

        public void Land()
        {
            animator.SetBool("Jump", false);
            animator.SetBool("onAir", false);
            animator.SetBool("Land", true);
            controller.characterMovement.EnableFeetIK();
        }

        public void HangLedge(ClimbController.ClimbState state)
        {
            if (state == ClimbController.ClimbState.BHanging)
                animator.CrossFade("Idle To Braced Hang", 0.2f);
            else if (state == ClimbController.ClimbState.FHanging)
                animator.CrossFade("Idle To Freehang", 0.2f);

            animator.SetBool("Land", false);
            animator.SetInteger("Climb State", (int)state);
            animator.SetBool("Hanging", true);
        }

        public void LedgeToLedge(ClimbController.ClimbState state, Vector3 direction, ref float startTime, ref float endTime)
        {
            if (state == ClimbController.ClimbState.BHanging)
            {
                if (direction.x == -1 && direction.y == 0 ||
                    direction.x == -1 && direction.y == 1 ||
                    direction.x == -1 && direction.y == -1)
                {
                    animator.CrossFade("Braced Hang Hop Left", 0.2f);
                    startTime = 0.2f;
                    endTime = 0.49f;
                }
                else if (direction.x == 1 && direction.y == 0 ||
                        direction.x == 1 && direction.y == -1 ||
                        direction.x == 1 && direction.y == 1)
                {
                    animator.CrossFade("Braced Hang Hop Right", 0.2f);
                    startTime = 0.2f;
                    endTime = 0.49f;
                }
                else if (direction.x == 0 && direction.y == 1)
                {
                    animator.CrossFade("Braced Hang Hop Up", 0.2f);
                    startTime = 0.3f;
                    endTime = 0.48f;
                }
                else if (direction.x == 0 && direction.y == -1)
                {

                    animator.CrossFade("Braced Hang Hop Down", 0.2f);
                    startTime = 0.3f;
                    endTime = 0.7f;
                }
            }

            animator.SetInteger("Climb State", (int)state);
            animator.SetBool("Hanging", true);
        }
        public void BracedClimb()
        {
            animator.CrossFade("Braced Hang To Crouch", 0.2f);
        }
        public void FreeClimb()
        {
            animator.CrossFade("Freehang Climb", 0.2f);
        }
        public void DropToFree(int state)
        {
            animator.CrossFade("Drop To Freehang", 0.1f);
            animator.SetInteger("Climb State", (int)state);
            animator.SetBool("Hanging", true);
            SetAnimVelocity(Vector3.forward);
        }
        public void DropToBraced(int state)
        {
            animator.CrossFade("Drop To Bracedhang", 0.1f);
            animator.SetInteger("Climb State", (int)state);
            animator.SetBool("Hanging", true);
            SetAnimVelocity(Vector3.forward);
        }

        public void DropLedge(int state)
        {
            animator.SetBool("Hanging", false);
            animator.SetInteger("Climb State", state);
        }

        public void HangMovement(float value, int climbstate)
        {
            animator.SetFloat("Horizontal", Mathf.Lerp(animator.GetFloat("Horizontal"), value, Time.deltaTime * 15));
            animator.SetInteger("Climb State", climbstate);
        }
        public void JumpPrediction(bool state)
        {
            controller.characterAnimation.animator.CrossFade("Predicted Jump", 0.1f);
            animator.SetBool("Crouch", state);
        }

        public void EnableIKSolver()
        {
            controller.characterMovement.EnableFeetIK();
        }
        public void EnableController()
        {
            controller.EnableController();
        }

        public void SetMatchTarget(AvatarTarget avatarTarget, Vector3 targetPos, Quaternion targetRot, Vector3 offset, float startnormalizedTime, float targetNormalizedTime)
        {
            if (animator.isMatchingTarget)
                return;

            float normalizeTime = Mathf.Repeat(animState.normalizedTime, 1f);

            if (normalizeTime > targetNormalizedTime)
                return;

            animator.SetTarget(avatarTarget, targetNormalizedTime); //Sets Target Bone for reference motion
            animator.MatchTarget(targetPos + offset, targetRot, avatarTarget, matchTargetWeightMask, startnormalizedTime, targetNormalizedTime, true);
        }
    }

}