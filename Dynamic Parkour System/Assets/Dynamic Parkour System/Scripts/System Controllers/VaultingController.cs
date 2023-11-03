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
using System;
using UnityEditor;

namespace Climbing
{
    [Flags]
    public enum VaultActions
    {
        Nothing = 0,
        Vault_Obstacle = 1 << 0,
        Vault_Over = 1 << 1,
        Slide = 1 << 2,
        Reach = 1 << 3,
        Climb_Ledge = 1 << 4,
        Jump_Prediction = 1 << 5,
        Vault_Down = 1 << 6,
    }

    public class VaultingController : MonoBehaviour
    {
        public bool debug;
        public VaultActions vaultActions;

        [HideInInspector] public ThirdPersonController controller;
        [HideInInspector] public Animator animator;

        private List<VaultAction> actions = new List<VaultAction>();
        private VaultAction curAction;

        public void Start()
        {
            controller = GetComponent<ThirdPersonController>();
            animator = GetComponent<Animator>();

            //Loads all Valt Actions Values
            if(vaultActions.HasFlag(VaultActions.Vault_Obstacle))
            {
                Action actionInfo = Resources.Load<Action>("ActionsConfig/VaultObstacle");
                Add(new VaultObstacle(controller, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Vault_Over))
            {
                Action actionInfo = Resources.Load<Action>("ActionsConfig/VaultOver");
                Add(new VaultOver(controller, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Slide))
            {
                Action actionInfo = Resources.Load<Action>("ActionsConfig/VaultSlide");
                Add(new VaultSlide(controller, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Slide))
            {
                Action actionInfo = Resources.Load<Action>("ActionsConfig/VaultReach");
                Add(new VaultReach(controller, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Climb_Ledge))
            {
                Add(new VaultClimbLedge(controller));
            }
            if (vaultActions.HasFlag(VaultActions.Jump_Prediction))
            {
                Add(new VaultJumpPrediction(controller));
            }
            if (vaultActions.HasFlag(VaultActions.Vault_Down))
            {
                Add(new VaultDown(controller));
            }
        }

        void Update()
        {
            if (!controller.isVaulting)
            {
                curAction = null;
            }

            //Check if vaulting action can be performed
            foreach (var item in actions)
            {
                if (item.CheckAction())
                {
                    curAction = item;
                    controller.isVaulting = true;
                    break;
                }
            }

            //Update logic of current vaulting Action
            if (curAction != null && controller.isVaulting)
            {
                if (!curAction.Update())
                    controller.isVaulting = false;

            }
        }

        private void FixedUpdate()
        {
            //Fixed Update logic of current vaulting Action
            if (curAction != null && controller.isVaulting)
            {
                if(!curAction.FixedUpdate())
                    controller.isVaulting = false;

            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (curAction != null)
            {
                curAction.OnAnimatorIK(layerIndex);
            }
        }

        private void OnDrawGizmos()
        {
            if (curAction != null && debug)
            {
                curAction.DrawGizmos();
            }
        }

        private void Add(VaultAction action)
        {
            if (action != null)
                actions.Add(action);
        }
    }

}