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
        Vault_Over = 1 << 10,
        Slide = 1 << 2,
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
        }

        // Update is called once per frame
        void Update()
        {
            if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.C))&& !isVaulting && !controller.dummy)
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
                if (!isVaulting)
                {
                    curAction = null;
                }
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (curAction != null && isVaulting)
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