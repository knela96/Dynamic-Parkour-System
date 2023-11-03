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
using UnityEngine.InputSystem;

namespace Climbing
{
    public class InputCharacterController : MonoBehaviour
    {
        private PlayerControls controls = null;

        [HideInInspector] public Vector2 movement;
        [HideInInspector] public bool run;
        [HideInInspector] public bool jump;
        [HideInInspector] public bool drop;

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
            //Hold and Release
            controls = new PlayerControls();
            controls.Player.Movement.performed += ctx => movement = ctx.ReadValue<Vector2>();
            controls.Player.Movement.canceled += ctx => movement = ctx.ReadValue<Vector2>();
            controls.Player.Jump.performed += ctx => jump = ctx.ReadValueAsButton();
            controls.Player.Jump.canceled += ctx => jump = ctx.ReadValueAsButton();
            controls.Player.Drop.performed += ctx => drop = ctx.ReadValueAsButton();
            controls.Player.Drop.canceled += ctx => drop = ctx.ReadValueAsButton();
            controls.Player.Run.performed += ctx => run = ctx.ReadValueAsButton();
            controls.Player.Run.canceled += ctx => run = ctx.ReadValueAsButton();
            controls.GameManager.Exit.performed += ctx => Exit();
        }

        void ToggleRun()
        {
            if (movement.magnitude > 0.2f && run == false)
                run = true;
            else
                run = false;
        }

        void Exit()
        {
            Application.Quit();
        }
    }

}