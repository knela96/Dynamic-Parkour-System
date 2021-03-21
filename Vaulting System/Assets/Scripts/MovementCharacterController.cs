using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonController))]
public class MovementCharacterController : MonoBehaviour
{
    public Transform cam;
    public float speed = 6f;
    private ThirdPersonController controller;
    //public CharacterController charactercontroller;
    public  Transform Transform_Mesh;

    private Rigidbody rb;
    private float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    private Vector3 velocity;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        AddMovementInput(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

        //charactercontroller.Move(Velocity.normalized * speed * Time.deltaTime);
        rb.velocity = new Vector3(Velocity.x * speed, rb.velocity.y, Velocity.z * speed);
    }

    public void AddMovementInput(float vertical, float horizontal)
    {
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if(direction.magnitude >= 0.1f)
        {
            //Get direction with camera rotation
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            //Rotate Mesh to Movement
            float angle = Mathf.SmoothDampAngle(Transform_Mesh.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            Transform_Mesh.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 translation = cam.transform.forward * vertical + cam.transform.right * horizontal;
            translation.y = 0;

            //Move Player to camera directin
            Velocity = translation;
        }
        else
        {
            Velocity = Vector3.zero;
        }
        
    }

    public Vector3 Velocity { get => velocity; set => velocity = value; }

}
