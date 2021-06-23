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

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Velocity", animVelocity.magnitude);

        if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Root"))
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
        animator.SetBool("Hanging", false);
        controller.characterMovement.enableFeetIK = false;
    }

    public void Land()
    {
        animator.SetBool("Jump", false);
        animator.SetBool("onAir", false);
        animator.SetBool("Land", true);
        controller.characterMovement.enableFeetIK = true;
    }

    public void HangLedge()
    {
        animator.SetBool("Hanging", true);
        animator.CrossFade("Idle To Braced Hang", 0.2f);
        animator.SetInteger("Climb State", 0);
    }
    public void DropLedge()
    {
        animator.SetBool("Hanging", false);
        animator.SetInteger("Climb State", 2);
    }
}
