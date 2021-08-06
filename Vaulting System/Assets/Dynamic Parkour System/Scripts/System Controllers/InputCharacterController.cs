using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Climbing
{
    public class InputCharacterController : MonoBehaviour
    {
        ThirdPersonController controller;
        public PlayerControls controls = null;

        public Vector2 movement;
        public bool run;
        public bool jump;
        public bool drop;

        private void OnEnable()
        {
            if(controls != null)
                controls.Enable();
        }

        private void OnDisable()
        {
            if (controls != null)
                controls.Disable();
        }

        void Awake()
        {
            controls = new PlayerControls();
            controls.Player.Movement.performed += ctx => movement = ctx.ReadValue<Vector2>();
            controls.Player.Movement.canceled += ctx => movement = ctx.ReadValue<Vector2>();

            //Press
            //controls.Player.Run.performed += ctx => ToggleRun();

            //Hold
            controls.Player.Jump.performed += ctx => jump = ctx.ReadValueAsButton();
            controls.Player.Jump.canceled += ctx => jump = ctx.ReadValueAsButton();
            controls.Player.Drop.performed += ctx => drop = ctx.ReadValueAsButton();
            controls.Player.Drop.canceled += ctx => drop = ctx.ReadValueAsButton();
            controls.Player.Run.performed += ctx => run = ctx.ReadValueAsButton();
            controls.Player.Run.canceled += ctx => run = ctx.ReadValueAsButton();
        }

        private void Start()
        {
            controller = GetComponent<ThirdPersonController>();
        }
        private void Update()
        {
            //if (jump)
            //    Time.timeScale += 0.1f;
            //else if (drop)
            //    Time.timeScale -= 0.1f;
            //else if (run)
            //    Time.timeScale = 1.0f;
        }

        void ToggleRun()
        {
            if (movement.magnitude > 0.2f && run == false)
                run = true;
            else
                run = false;
        }
    }

}