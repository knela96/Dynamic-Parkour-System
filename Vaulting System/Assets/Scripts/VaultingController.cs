using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VaultingController : MonoBehaviour
{
    public ClimbController climbController;
    public Vector3 kneeRaycastOrigin;
    public float kneeRaycastLength = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        
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
        Physics.Raycast(kneeRaycastOrigin, transform.forward, out hit, kneeRaycastLength);
       
        if (hit.transform.tag == "Vault")
        {
            //Play anim
        }
    }
}
