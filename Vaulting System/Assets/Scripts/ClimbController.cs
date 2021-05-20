using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Climbing;

public class ClimbController : MonoBehaviour
{
    bool ledgeFound = false;
    public bool onLedge = false;

    public DetectionCharacterController characterDetection;
    public ThirdPersonController characterController;
    public float rootOffset;
    Vector3 target = Vector3.zero;

    public GameObject limitLHand;
    public GameObject limitRHand;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!characterController.dummy)
        {
            onLedge = false;
            RaycastHit hit;
            ledgeFound = characterDetection.FindLedgeCollision(out hit);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button1))
            {
                if (ledgeFound)
                    ReachLedge(hit);
            }
        }

        if (onLedge)
        {
            ClimbMovement(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

            if (Input.GetKeyDown(KeyCode.C))
                characterController.EnableController();
        }
    }

    public void ClimbMovement(float vertical, float horizontal)
    {
        Vector3 translation = transform.right * horizontal * 0.025f;

        if (CheckValidMovement(translation))
            transform.position += translation;
    }

    bool CheckValidMovement(Vector3 translation)
    {
        bool ret = false;

        if (translation.normalized.x < 0)
            ret = characterController.characterDetection.ThrowRayToLedge(limitLHand.transform.position);
        if (translation.normalized.x > 0)
            ret = characterController.characterDetection.ThrowRayToLedge(limitRHand.transform.position);
        return ret;
    }

    void ReachLedge(RaycastHit hit)
    {
        List<Point> points = hit.transform.parent.GetComponentInChildren<HandlePoints>().pointsInOrder;

        float dist = float.PositiveInfinity;
        for (int i = 0; i < points.Count; i++)
        {
            float point2root = Vector3.Distance(points[i].transform.position, transform.position);

            if (point2root < dist)
            {
                dist = point2root;
                target = points[i].transform.position;
                if (i == 0)//Left Point
                {
                    target.x += 0.5f;
                }
                else if(i == points.Count - 1)//Right Point
                {
                    target.x -= 0.5f;
                }
            }
        }

        characterController.DisableController();
        characterController.characterAnimation.HangLedge();
        onLedge = true;
        characterController.characterAnimation.animator.CrossFade("Hanging Idle", 0.0f);
        transform.rotation = Quaternion.LookRotation(-hit.normal);
        transform.position = target - new Vector3(0, rootOffset, 0);
    }

}
