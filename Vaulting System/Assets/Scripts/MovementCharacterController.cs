using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementState { Walking, Running, Jumping }

[RequireComponent(typeof(ThirdPersonController))]
public class MovementCharacterController : MonoBehaviour
{
    #region Variables
    private ThirdPersonController controller;
    private Animator anim;
    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 animVelocity;
    public float speed = 6f;
    private float smoothSpeed;

    public float walkSpeed;
    public float JogSpeed;
    public float RunSpeed;
    public float jumpForce;
    public float fallForce;

    private Vector3 leftFootPosition, leftFootIKPosition, rightFootPosition, rightFootIKPosition;
    Quaternion leftFootIKRotation, rightFootIKRotation;
    private float lastPelvisPositionY, lastLeftFootPosition, lastRightFootPosition;
    [Header("Feet IK")]
    public bool enableFeetIK = true;
    [Range(0, 2f)][SerializeField] private float heightFromGroundRaycast = 1.4f;
    [Range(0, 2f)] [SerializeField] private float raycastDownDistance = 1.5f;
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private float pelvisOffset = 0f;
    [Range(0, 1f)] [SerializeField] private float pelvisUpandDownSpeed = 0.25f;
    [Range(0, 1f)] [SerializeField] private float feetToIKPositionSpeed = 0.25f;

    public string leftFootAnimVariableName = "LeftFootCurve";
    public string rightFootAnimVariableName = "RightFootCurve";

    public bool useProIKFeature = false;
    public bool showDebug = true;

    MovementState currentState;

    public delegate void OnLandedDelegate();
    public delegate void OnFallDelegate();
    public event OnLandedDelegate OnLanded;
    public event OnFallDelegate OnFall;

    //public CharacterController charactercontroller;


    #endregion
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        rb = GetComponent<Rigidbody>();
        anim = controller.characterAnimation.animator;
        SetCurrentState(MovementState.Walking);
    }

    // Update is called once per frame
    void Update()
    {
        //charactercontroller.Move(Velocity.normalized * speed * Time.deltaTime);

        ApplyInputMovement();

        if (controller.inAir)
        {
            if (controller.characterDetection.IsGrounded() && rb.velocity.y < 0)
            {
                OnLanded();
                controller.inAir = false;
            }
            else
            {
                OnFall();
            }
        }
    }
    private void FixedUpdate()
    {
        if (!enableFeetIK)
            return;
        if (anim == null)
            return;

        AdjustFeetTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
        AdjustFeetTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

        //Raycast to Ground
        FeetPositionSolver(rightFootPosition, ref rightFootIKPosition, ref rightFootIKRotation);
        FeetPositionSolver(leftFootPosition, ref leftFootIKPosition, ref leftFootIKRotation);
    }

    #region Movement
    public void ApplyInputMovement()
    {
        //Apply fall multiplier
        if(rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallForce - 1) * Time.deltaTime;
        }

        if (GetState() == MovementState.Running)
        {
            velocity.Normalize();
        }

        if (velocity.magnitude > 0)
        {
            smoothSpeed = Mathf.Lerp(smoothSpeed, speed, Time.deltaTime * 2);
            rb.velocity = new Vector3(velocity.x * smoothSpeed, rb.velocity.y, velocity.z * smoothSpeed);
            controller.characterAnimation.SetAnimVelocity(rb.velocity);
        }
        else
        {
            //Lerp down with current velocity of the rigidbody
            smoothSpeed = Mathf.SmoothStep(smoothSpeed, 0, Time.deltaTime * 20);
            rb.velocity = new Vector3(rb.velocity.normalized.x * smoothSpeed, rb.velocity.y, rb.velocity.normalized.z * smoothSpeed);
            controller.characterAnimation.SetAnimVelocity(controller.characterAnimation.GetAnimVelocity().normalized * smoothSpeed);
        }
    }

    public Vector3 GetVelocity() { return rb.velocity; }
    public void SetVelocity(Vector3 value) { velocity = value; }

    public void ResetSpeed()
    {
        smoothSpeed = 0;
    }
    
    public void SetCurrentState(MovementState state) 
    {
        currentState = state;

        switch (currentState)
        {
            case MovementState.Walking:
                speed = walkSpeed;
                break;
            case MovementState.Running:
                speed = RunSpeed;
                smoothSpeed = speed;
                break;
        }
    }

    public MovementState GetState()
    {
        return currentState;
    }

    public void Jump()
    {
        if (controller.isGrounded)
        {
            rb.velocity = Vector3.up * jumpForce;
            controller.inAir = true;
        }
    }

    #endregion

    #region Foot IK


    private void OnAnimatorIK(int layerIndex)
    {
        if (!enableFeetIK)
            return;
        if (anim == null)
            return;

        MovePelvisHeight();

        //Right Foot IK Position and Rotation
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);

        if (useProIKFeature)
        {
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, anim.GetFloat(rightFootAnimVariableName));
        }

        MoveFeetToIKPoint(AvatarIKGoal.RightFoot, rightFootIKPosition, rightFootIKRotation, ref lastRightFootPosition);

        //Left Foot IK Position and Rotation
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);

        if (useProIKFeature)
        {
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, anim.GetFloat(leftFootAnimVariableName));
        }

        MoveFeetToIKPoint(AvatarIKGoal.LeftFoot, leftFootIKPosition, leftFootIKRotation, ref lastLeftFootPosition);
    }

    void MoveFeetToIKPoint(AvatarIKGoal foot, Vector3 positionIKHolder, Quaternion rotationHolder, ref float lastFootPositionY)
    {
        Vector3 targetIKPosition = anim.GetIKPosition(foot);
        
        if(positionIKHolder != Vector3.zero)
        {
            targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
            positionIKHolder = transform.InverseTransformPoint(positionIKHolder);
            float yVariable = Mathf.Lerp(lastFootPositionY, positionIKHolder.y,feetToIKPositionSpeed);
            targetIKPosition.y += yVariable;

            lastFootPositionY = yVariable;

            targetIKPosition = transform.TransformPoint(targetIKPosition);

            anim.SetIKRotation(foot, rotationHolder);
        }
        anim.SetIKPosition(foot, targetIKPosition);
    }

    private void MovePelvisHeight()
    {
        if(rightFootIKPosition == Vector3.zero || leftFootIKPosition == Vector3.zero || lastPelvisPositionY == 0)
        {
            lastPelvisPositionY = anim.bodyPosition.y;
            return;
        }

        float leftOffsetPosition = leftFootIKPosition.y - transform.position.y;
        float rightOffsetPosition = rightFootIKPosition.y - transform.position.y;

        float totalOffset = leftOffsetPosition < rightOffsetPosition ? leftOffsetPosition : rightOffsetPosition;

        Vector3 newPelvisPosition = anim.bodyPosition + Vector3.up * totalOffset;

        newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpandDownSpeed);

        anim.bodyPosition = newPelvisPosition;

        lastPelvisPositionY = anim.bodyPosition.y;
    }

    //Raycast handler
    private void FeetPositionSolver(Vector3 fromRaycastPosition, ref Vector3 feetIKPositions, ref Quaternion feetIKRotations)
    {
        RaycastHit feetHit;

        if (showDebug)
            Debug.DrawLine(fromRaycastPosition, fromRaycastPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast),Color.green);

        if (Physics.Raycast(fromRaycastPosition, Vector3.down, out feetHit, raycastDownDistance + raycastDownDistance, environmentLayer))
        {
            feetIKPositions = fromRaycastPosition;
            feetIKPositions.y = feetHit.point.y + pelvisOffset;
            feetIKRotations = Quaternion.FromToRotation(Vector3.up, feetHit.normal) * transform.rotation;
            return;
        }

        feetIKPositions = Vector3.zero;
    }

    private void AdjustFeetTarget(ref Vector3 feetPositions, HumanBodyBones foot)
    {
        feetPositions = anim.GetBoneTransform(foot).position;
        feetPositions.y = transform.position.y + heightFromGroundRaycast;
    }

    #endregion


    #region Climb
    
    public void SetKinematic(bool active)
    {
        rb.isKinematic = active;
    }

    #endregion
}
