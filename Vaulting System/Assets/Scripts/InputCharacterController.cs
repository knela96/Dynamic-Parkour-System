using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonController))]
public class InputCharacterController : MonoBehaviour
{
    private ThirdPersonController character;

    // Start is called before the first frame update
    void Start()
    {
        character = GetComponent<ThirdPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        //character.AddMovementInput(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

        //if(Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKey(KeyCode.Joystick1Button10))
        //{
        //    character.ToggleRun();
        //}
        //
        //if(Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button1))
        //{
        //    //character.Jump();
        //}
    }
}
