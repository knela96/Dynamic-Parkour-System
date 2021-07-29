using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{ 
    public abstract class VaultAction
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
        protected float startDelay = 0f;
        protected LayerMask layer;
        protected string tag;

        protected VaultingController vaultingController;

        public VaultAction(VaultingController _vaultingController)
        {
            vaultingController = _vaultingController;
        }

        public VaultAction(VaultingController _vaultingController, Action action)
        {
            vaultingController = _vaultingController;
            controller = vaultingController.controller;
            animator = vaultingController.animator;

            //Loads Action Info
            clip = action.clip;
            kneeRaycastOrigin = action.kneeRaycastOrigin;
            kneeRaycastLength = action.kneeRaycastLength;
            landOffset = action.landOffset;
            startDelay = Mathf.Abs(action.startDelay) * -1;
            layer = action.layer;
            tag = action.tag;
        }

        public abstract bool CheckAction();

        public abstract bool ExecuteAction();

        public virtual void DrawGizmos() {}

        public virtual void OnAnimatorIK(int layerIndex) {}
    }
}