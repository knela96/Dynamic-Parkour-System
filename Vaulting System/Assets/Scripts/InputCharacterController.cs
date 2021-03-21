using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonController))]
public class InputCharacterController : MonoBehaviour
{
    private MovementCharacterController controller;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<MovementCharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        controller.AddMovementInput(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));
    }
}
