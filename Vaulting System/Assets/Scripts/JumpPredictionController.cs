using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JumpPredictionController : MonoBehaviour
{
    [SerializeField]
    public float maxHeight = 1.5f;
    [SerializeField]
    public float maxDistance = 5.0f;
    [SerializeField]
    public float maxTime = 2.0f;

    Vector3 origin;
    Vector3 target;

    bool move = false;

    protected float animationTime = 0;

    public int accuracy = 50;

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            origin = transform.position;

        if (origin == Vector3.zero)
            return;

        //Draw the parabola by sample a few times
        Vector3 lastP = origin;
        for (float i = 0; i < accuracy; i++)
        {
            Vector3 p = SampleParabola(origin, target, maxHeight, i / accuracy);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lastP, p);
            Gizmos.DrawWireSphere(p, 0.02f);
            lastP = p;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void FollowParabola()
    {
        if (move == true)
        {
            if (animationTime > 1.0f)
            {
                animationTime = 0.0f;
                move = false;
            }
            else
            {
                animationTime += (Time.deltaTime * 0.8f);
                transform.position = SampleParabola(origin, target, maxHeight, Mathf.Clamp(animationTime, 0.0f, 1.0f));
            }
        }
    }

    public bool hasArrived()
    {
        return !move;
    }

    public bool SetParabola(Transform start, Transform end)
    {
        Vector2 a = new Vector2(start.position.x, start.position.z);
        Vector2 b = new Vector2(end.position.x, end.position.z);

        if (end.position.y - start.position.y > maxHeight || (Vector2.Distance(a, b) > maxDistance))
            return false;

        origin = start.position;
        target = end.position;
        move = true;

        return true;
    }

    Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
    {
        float parabolicT = t * 2 - 1;
        if (Mathf.Abs(start.y - end.y) < 0.1f)
        {
            //start and end are roughly level, pretend they are - simpler solution with less steps
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += (-parabolicT * parabolicT + 1) * height;
            return result;
        }
        else
        {
            //start and end are not level, gets more complicated
            Vector3 travelDirection = end - start;
            Vector3 levelDirecteion = end - new Vector3(start.x, end.y, start.z);
            Vector3 right = Vector3.Cross(travelDirection, levelDirecteion);
            Vector3 up = Vector3.Cross(right, levelDirecteion);
            if (end.y > start.y) up = -up;
            Vector3 result = start + t * travelDirection;
            result += ((-parabolicT * parabolicT + 1) * height) * up.normalized;
            return result;
        }
    }

}