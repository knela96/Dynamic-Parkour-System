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
        ThirdPersonController controller;
        Animator animator;
        public VaultActions vaultActions;
        bool isVaulting = false;
        List<VaultAction> actions = new List<VaultAction>();
        VaultAction curAction;
        public bool debug;

        public void Start()
        {
            controller = GetComponent<ThirdPersonController>();
            animator = GetComponent<Animator>();

            if(vaultActions.HasFlag(VaultActions.Vault_Obstacle))
            {
                Action actionInfo = Resources.Load<Action>("Actions/VaultObstacle");
                AddComponent(gameObject.AddComponent<VaultObstacle>(), actionInfo);
            }
            if (vaultActions.HasFlag(VaultActions.Vault_Over))
            {
                Action actionInfo = Resources.Load<Action>("Actions/VaultOver");
                AddComponent(gameObject.AddComponent<VaultOver>(), actionInfo);
            }
            if (vaultActions.HasFlag(VaultActions.Slide))
            {
                //Action actionInfo = Resources.Load<Action>("Actions/Slide");
                //AddComponent(gameObject.AddComponent<VaultSlide>(), actionInfo);
            }
        }

        void AddComponent(VaultAction action, Action actionInfo)
        {
            action.Initialize(controller, animator, actionInfo);
            actions.Add(action);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.Space) && !isVaulting && !controller.dummy)
            {
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


        private void OnDrawGizmos()
        {
            if (curAction != null && isVaulting && debug)
            {
                curAction.DrawGizmos();
            }
        }
    }

}