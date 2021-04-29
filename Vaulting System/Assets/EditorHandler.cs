using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Climbing
{
    [CustomEditor(typeof(DrawWireCube))]
    public class DrawWireCubeEditor : Editor
    {
        private void OnSceneGUI()
        {
            DrawWireCube t = target as DrawWireCube;

            if (t.ikPos.Count == 0)
                t.ikPos = t.transform.GetComponent<Point>().iks;

            for (int i = 0; i < t.ikPos.Count; i++)
            {
                if (t.ikPos[i].target != null)
                {
                    Color TargetColor = Color.red;

                    switch (t.ikPos[i].ik)
                    {
                        case AvatarIKGoal.LeftFoot:
                            TargetColor = Color.magenta;
                            break;
                        case AvatarIKGoal.LeftHand:
                            TargetColor = Color.cyan;
                            break;
                        case AvatarIKGoal.RightFoot:
                            TargetColor = Color.yellow;
                            break;
                        case AvatarIKGoal.RightHand:
                            TargetColor = Color.green;
                            break;
                    }

                    Handles.color = TargetColor;
                    Handles.CubeCap(0, t.ikPos[i].target.position, t.ikPos[i].target.rotation, 0.05f);

                    if (t.ikPos[i].hint != null)
                    {
                        Handles.CubeCap(0, t.ikPos[i].hint.position, t.ikPos[i].target.rotation, 0.05f);
                    }
                }
                else
                {
                    t.ikPos = t.transform.GetComponent<Point>().iks;
                }
            }
        }
    }

    [CustomEditor(typeof(DrawLine))]
    public class EditorVis : Editor
    {
        private void OnSceneGUI()
        {
            DrawLine t = target as DrawLine;

            if (t == null)
                return;

            if (t.ConnectedPoints.Count == 0)
                t.ConnectedPoints.AddRange(t.transform.GetComponent<HandlePointConnection>().GetAllConnections());
            
            for (int i = 0; i < t.ConnectedPoints.Count; i++)
            {
                Vector3 pos1 = t.ConnectedPoints[i].target1.transform.position;
                Vector3 pos2 = t.ConnectedPoints[i].target2.transform.position;

                switch (t.ConnectedPoints[i].type)
                {
                    case ConnectionType.direct:
                        Handles.color = Color.red;
                        break;
                    case ConnectionType.inBetween:
                        Handles.color = Color.green;
                        break;
                }

                Handles.DrawLine(pos1, pos2);
                t.refresh = false;
            }
        }
    }

    [CustomEditor(typeof(DrawLineIndividual))]
    public class DrawLineVis : Editor
    {
        private void OnSceneGUI()
        {
            DrawLineIndividual t = target as DrawLineIndividual;

            if (t == null)
                return;

            if (t.ConnectedPoints.Count == 0)
                t.ConnectedPoints.AddRange(t.transform.GetComponent<Point>().neighbours);

            for(int i = 0; i < t.ConnectedPoints.Count; i++)
            {
                if (t.ConnectedPoints[i].target == null)
                    continue;

                Vector3 pos1 = t.transform.position;
                Vector3 pos2 = t.ConnectedPoints[i].target.transform.position;

                switch (t.ConnectedPoints[i].type)
                {
                    case ConnectionType.direct:
                        Handles.color = Color.red;
                        break;
                    case ConnectionType.inBetween:
                        Handles.color = Color.green;
                        break;
                }

                Handles.DrawLine(pos1, pos2);
                t.refresh = false;
            }
        }
    }
}
