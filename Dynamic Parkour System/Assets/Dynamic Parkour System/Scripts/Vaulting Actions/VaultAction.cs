/*
Dynamic Parkour System grants parkour capabilities to any humanoid character model.
Copyright (C) 2021  Èric Canela Sol
Contact: knela96@gmail.com or @knela96 twitter

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
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