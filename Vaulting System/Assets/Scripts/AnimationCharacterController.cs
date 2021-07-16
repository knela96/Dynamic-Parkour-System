using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Climbing;

[RequireComponent(typeof(ThirdPersonController))]
public class AnimationCharacterController : MonoBehaviour
{
    public Animator animator;
    private ThirdPersonController controller;
    private Vector3 animVelocity;

    public AvatarTarget avatarTarget;
    public Transform target;
    public float startnormalizedTime;
    public float targetNormalizedTime;
    private MatchTargetWeightMask matchTargetWeightMask = new MatchTargetWeightMask(Vector3.one, 0);
    private bool isMatchTargeted = true;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Velocity", animVelocity.magnitude);

        if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Root") || animator.GetCurrentAnimatorStateInfo(0).IsTag("Drop"))
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

    public void Jump()
    {
        animator.SetBool("Jump", true);
        animator.SetBool("onAir", false);
        animator.SetBool("Land", false);
        controller.characterMovement.enableFeetIK = false;
    }
    public void Fall()
    {
        animator.SetBool("Jump", false);
        animator.SetBool("onAir", true);
        animator.SetBool("Land", false);
        controller.characterMovement.enableFeetIK = false;
    }

    public void Land()
    {
        animator.SetBool("Jump", false);
        animator.SetBool("onAir", false);
        animator.SetBool("Land", true);
        //controller.characterMovement.enableFeetIK = true;
    }

    public void HangLedge(ClimbController.ClimbState state)
    {
        if (state == ClimbController.ClimbState.BHanging)
            animator.CrossFade("Idle To Braced Hang", 0.2f);
        else if (state == ClimbController.ClimbState.FHanging)
            animator.CrossFade("Idle To Freehang", 0.2f);

        animator.SetInteger("Climb State", (int)state);
        animator.SetBool("Hanging", true);
    }

    public void LedgeToLedge(ClimbController.ClimbState state, Vector3 direction)
    {
        if (state == ClimbController.ClimbState.BHanging)
            if(direction.x > 0)
                animator.CrossFade("Braced Hang Hop Right", 0.2f);
            else if(direction.x < 0)
                animator.CrossFade("Braced Hang Hop Left", 0.2f);

            else if (state == ClimbController.ClimbState.FHanging)
            animator.CrossFade("Idle To Freehang", 0.2f);

        animator.SetInteger("Climb State", (int)state);
        animator.SetBool("Hanging", true);
    }

    public void DropLedge(int state)
    {
        animator.SetBool("Hanging", false);
        animator.SetInteger("Climb State", state);
    }

    public void HangMovement(float value, int climbstate)
    {
        animator.SetFloat("Horizontal", Mathf.Lerp(animator.GetFloat("Horizontal"), value, Time.deltaTime * 10));
        animator.SetInteger("Climb State", climbstate);
    }

    public void EnableIKSolver()
    {
        controller.characterMovement.EnableFeetIK();
    }

    public void EnableController()
    {
        controller.EnableController();
    }
    public void SetMatchTarget(Vector3 targetPos, Quaternion targetRot, Vector3 offset)
    {
        if (animator.isMatchingTarget)
            return;

        float normalizeTime = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);

        if (normalizeTime > targetNormalizedTime)
            return;
        
        animator.SetTarget(avatarTarget, targetNormalizedTime); //Just for our reference. Not used here.
        animator.MatchTarget(targetPos + offset, targetRot, avatarTarget, matchTargetWeightMask, startnormalizedTime, targetNormalizedTime);

    }
    public void SetMatchTarget(Vector3 targetPos, Quaternion targetRot, Vector3 offset, float startnormalizedTime, float targetNormalizedTime)
    {
        if (animator.isMatchingTarget)
            return;

        float normalizeTime = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);

        if (normalizeTime > targetNormalizedTime)
            return;

        animator.SetTarget(avatarTarget, targetNormalizedTime); //Just for our reference. Not used here.
        animator.MatchTarget(targetPos + offset, targetRot, avatarTarget, matchTargetWeightMask, startnormalizedTime, targetNormalizedTime, true);

    }
}
