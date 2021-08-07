using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    CinemachineCameraOffset cameraOffset;

    public Vector3 _offset;
    public Vector3 _default;
    private Vector3 _target;
    private Vector3 _current;

    public float maxTime = 2.0f;
    private float curTime = 0.0f;

    private bool anim = false;


    void Start()
    {
        cameraOffset = GetComponent<CinemachineCameraOffset>();
    }


    void Update()
    {
        //Lerps Camera Position to the new offset
        if (anim)
        {
            curTime += Time.deltaTime / maxTime;
            cameraOffset.m_Offset = Vector3.Lerp(cameraOffset.m_Offset, _target, curTime);
        }

        if (curTime >= 1.0f)
            anim = false;
    }

    /// <summary>
    /// Adds Offset to the camera while being on Climbing or inGround
    /// </summary>
    public void newOffset(bool offset)
    {
        if (offset)
            _target = _offset;
        else
            _target = _default;

        anim = true;
        curTime = 0;
    }
}
