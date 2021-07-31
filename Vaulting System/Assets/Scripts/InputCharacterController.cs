using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Climbing
{
    public class InputCharacterController : MonoBehaviour
    {
        ThirdPersonController controller;
        public PlayerControls controls;

        public Vector2 movement;
        public bool run;
        public bool jump;
        public bool drop;

        private void OnEnable()
        {
            controls.Enable();
        }

        private void OnDisable()
        {
            controls.Disable();
        }

        void Awake()
        {
            controls = new PlayerControls();
            controls.Player.Movement.performed += ctx => movement = ctx.ReadValue<Vector2>();
            controls.Player.Movement.canceled += ctx => movement = ctx.ReadValue<Vector2>();

            //Press
            controls.Player.Run.performed += ctx => ToggleRun();

            //Hold
            controls.Player.Jump.performed += ctx => jump = ctx.ReadValueAsButton();
            controls.Player.Jump.canceled += ctx => jump = ctx.ReadValueAsButton();
            controls.Player.Drop.performed += ctx => drop = ctx.ReadValueAsButton();
            controls.Player.Drop.canceled += ctx => drop = ctx.ReadValueAsButton();
        }

        private void Start()
        {
            controller = GetComponent<ThirdPersonController>();
        }

        void LateUpdate()
        {
            //if (controller.dummy)
            //{
            //    jump = false;
            //    drop = false;
            //}
        }

        void ToggleRun()
        {
            run = !run;
        }
    }

}