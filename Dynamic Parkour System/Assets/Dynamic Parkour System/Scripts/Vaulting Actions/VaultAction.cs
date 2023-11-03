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
            tag = action.tag;
        }

        public abstract bool CheckAction();

        public virtual bool Update() { return true; }

        public virtual bool FixedUpdate() { return true; }

        public virtual void DrawGizmos() {}

        public virtual void OnAnimatorIK(int layerIndex) {}
    }
}