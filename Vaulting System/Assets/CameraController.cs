using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    CinemachineCameraOffset cameraOffset;

    public Vector3 _offset;
    public Vector3 _default;
    Vector3 _target;
    Vector3 _current;

    public float maxTime = 2.0f;
    float curTime = 0.0f;

    bool anim = false;

    // Start is called before the first frame update
    void Start()
    {
        cameraOffset = GetComponent<CinemachineCameraOffset>();
    }

    // Update is called once per frame
    void Update()
    {
        if (anim)
        {
            curTime += Time.deltaTime / maxTime;
            cameraOffset.m_Offset = Vector3.Lerp(cameraOffset.m_Offset, _target, curTime);
        }

        if (curTime >= 1.0f)
            anim = false;
            
    }

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
