using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{ 
    public abstract class VaultAction
    {
        protected AnimationClip clip;
        protected ThirdPersonController controller;
        protected AnimationCharacterController animator;
        protected Vector3 targetPos;
        protected Quaternion targetRot;
        protected Vector3 startPos;
        protected Quaternion startRot;
        protected float vaultTime = 0.0f;
        protected float animLength = 0.0f;
        protected Vector3 kneeRaycastOrigin;
        protected float kneeRaycastLength;
        protected float landOffset;
        protected float startDelay = 0f;
        protected LayerMask layer;
        protected string tag;

        public VaultAction(ThirdPersonController _controller)
        {
            controller = _controller;
            animator = controller.characterAnimation;
        }

        public VaultAction(ThirdPersonController _controller, Action action)
        {
            controller = _controller;
            animator = controller.characterAnimation;

            //Loads Action Info from Scriptable Object
            clip = action.clip;
            kneeRaycastOrigin = action.kneeRaycastOrigin;
            kneeRaycastLength = action.kneeRaycastLength;
            landOffset = action.landOffset;
            startDelay = Mathf.Abs(action.startDelay) * -1;
            layer = action.layer;
            tag = action.tag;
        }

        public abstract bool CheckAction();

        public virtual bool Update() { return true; }

        public virtual bool FixedUpdate() { return true; }

        public virtual void DrawGizmos() {}

        public virtual void OnAnimatorIK(int layerIndex) {}
    }
}