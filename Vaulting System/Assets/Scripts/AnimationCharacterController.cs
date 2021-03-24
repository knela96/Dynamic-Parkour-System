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
    public void SetAnimVelocity(Vector3 value) { animVelocity = value; }
    public Vector3 GetAnimVelocity() { return animVelocity; }
}
