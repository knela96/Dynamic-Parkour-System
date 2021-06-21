using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VaultingController : MonoBehaviour
{
    public ClimbController climbController;
    public ThirdPersonController controller;
    public Vector3 kneeRaycastOrigin;
    public float kneeRaycastLength = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        climbController = GetComponent<ClimbController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
            CheckVaultObject();
    }

    private void CheckVaultObject()
    {
        RaycastHit hit;

        if (!Physics.Raycast(transform.position + kneeRaycastOrigin, transform.forward, out hit, kneeRaycastLength))
            return;

        if (hit.transform.tag == "Vault")
        {
            controller.characterAnimation.animator.SetBool("Vault", true);
        }
    }
}
