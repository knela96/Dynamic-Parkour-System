using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using Climbing;

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
        Jump_Prediction = 1 << 5
    }

    public class VaultingController : MonoBehaviour
    {
        [HideInInspector] public ThirdPersonController controller;
        [HideInInspector] public Animator animator;
        public VaultActions vaultActions;
        private bool isVaulting = false;
        public bool debug;

        private List<VaultAction> actions = new List<VaultAction>();
        private VaultAction curAction;

        public void Start()
        {
            controller = GetComponent<ThirdPersonController>();
            animator = GetComponent<Animator>();

            if(vaultActions.HasFlag(VaultActions.Vault_Obstacle))
            {
                Action actionInfo = Resources.Load<Action>("Actions/VaultObstacle");
                Add(new VaultObstacle(this, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Vault_Over))
            {
                Action actionInfo = Resources.Load<Action>("Actions/VaultOver");
                Add(new VaultOver(this, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Slide))
            {
                Action actionInfo = Resources.Load<Action>("Actions/VaultSlide");
                Add(new VaultSlide(this, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Slide))
            {
                Action actionInfo = Resources.Load<Action>("Actions/VaultReach");
                Add(new VaultReach(this, actionInfo));
            }
            if (vaultActions.HasFlag(VaultActions.Climb_Ledge))
            {
                Add(new VaultClimbLedge(this));
            }
            if (vaultActions.HasFlag(VaultActions.Jump_Prediction))
            {
                Add(new VaultJumpPrediction(this));
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (!isVaulting)
            {
                curAction = null;
            }

            if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.C)) && !isVaulting)
            {
                curAction = null;

                foreach (var item in actions)
                {
                    isVaulting = item.CheckAction();

                    if (isVaulting)
                    {
                        curAction = item;
                        break;
                    }
                }
            }

            if (curAction != null && isVaulting)
            {
                isVaulting = curAction.ExecuteAction();
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
            if (curAction != null && isVaulting && debug)
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