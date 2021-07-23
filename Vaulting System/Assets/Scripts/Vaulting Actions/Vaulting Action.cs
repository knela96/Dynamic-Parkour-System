using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class VaultAction : MonoBehaviour
{
    protected AnimationClip clip;
    protected ThirdPersonController controller;
    protected Animator animator;
    protected Vector3 targetPos;
    protected Quaternion targetRot;
    protected Vector3 startPos;
    protected Quaternion startRot;
    protected float vaultTime = 0.0f;
    protected float animLength = 0.0f;
    protected bool isVaulting;
    protected Vector3 kneeRaycastOrigin;
    protected float kneeRaycastLength;
    protected float landOffset;

    public virtual void Initialize(ThirdPersonController controller, Animator animator, Action actionInfo) {}

    public virtual void Start() {}

    public virtual bool CheckAction() { return false; }

    public virtual bool ExecuteAction() { return false; }

    public virtual void DrawGizmos() {}


}