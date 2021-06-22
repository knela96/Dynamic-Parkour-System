using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VaultingController : MonoBehaviour
{
    public ClimbController climbController;
    public ThirdPersonController controller;
    public Vector3 kneeRaycastOrigin;
    public float kneeRaycastLength = 1.0f;
    public float landOffset = 0.2f;
    Vector3 targetPos;
    Vector3 startPos;
    bool isVaulting;
    float vaultTime = 0.0f;
    float animLength = 0.0f;
    public bool debug = false;
    public AnimationClip clip;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        climbController = GetComponent<ClimbController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space) && !isVaulting)
            CheckVaultObject();

        if (isVaulting)
        {
            float actualSpeed = Time.deltaTime / animLength;
            vaultTime += actualSpeed;

            if (vaultTime > 1)
            {
                controller.characterAnimation.animator.SetBool("Vault", false);
                isVaulting = false;
                controller.EnableController();
            }

            transform.position = Vector3.Lerp(startPos, targetPos, vaultTime);
        }
    }

    private void CheckVaultObject()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + kneeRaycastOrigin;
        Vector3 origin2 = origin + transform.forward * kneeRaycastLength;

        if (Physics.Raycast(origin, transform.forward, out hit, kneeRaycastLength))
        {
            RaycastHit hit2;
            if (Physics.Raycast(origin2, Vector3.down, out hit2, 1)) //Ground Hit
            {
                if (hit.transform.tag == "Vault")
                {
                    controller.characterAnimation.animator.CrossFade("Vault", 0.2f);
                    isVaulting = true;
                    startPos = transform.position;
                    targetPos = hit2.point + (-hit.normal * (hit.transform.localScale.z + landOffset));
                    controller.DisableController();
                    vaultTime = 0;
                    animLength = clip.length;
                }
            }
        }

        if (debug)
        {
            Debug.DrawLine(origin, origin + transform.forward * kneeRaycastLength);//Forward Raycast
            Debug.DrawLine(hit.point, hit.point + hit.normal, Color.cyan); //Face Normal
            Debug.DrawLine(origin2, origin2 + Vector3.down);//Down Raycast
        }
    }

    private void OnDrawGizmos()
    {
        if (debug && isVaulting)
            Gizmos.DrawSphere(targetPos, 0.08f);
    }
}
