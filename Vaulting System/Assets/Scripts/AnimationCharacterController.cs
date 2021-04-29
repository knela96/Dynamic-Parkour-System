using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        controller.characterMovement.enableFeetIK = true;
    }
}
