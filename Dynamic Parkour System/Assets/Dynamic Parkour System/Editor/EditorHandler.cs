using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Climbing
{
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
                if (t.ConnectedPoints[i].target1 == null || t.ConnectedPoints[i].target2 == null)
                    continue;

                Vector3 pos1 = t.ConnectedPoints[i].target1.transform.position;
                Vector3 pos2 = t.ConnectedPoints[i].target2.transform.position;

                Handles.color = Color.red;
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

                Handles.color = Color.red;
                Handles.DrawLine(pos1, pos2);
                t.refresh = false;
            }
        }
    }
}
