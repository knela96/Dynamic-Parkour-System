using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
namespace Climbing
{
    public class CurvesHolder : MonoBehaviour
    {
        public List<Curve> curves = new List<Curve>();

        public BezierCurve ReturnCurve(CurveType t)
        {
            BezierCurve retVal = null;

            for (int i = 0; i < curves.Count; i++)
            {
                if(t == curves[i].curveType)
                {
                    retVal = curves[i].bCurve;
                    break;
                }
            }
            return retVal;
        }

        [System.Serializable]
        public class Curve
        {
            public CurveType curveType;
            public BezierCurve bCurve;
        }

        public enum CurveType
        {
            horizontal,
            vertical,
            dismount,
            mount
        }
    }

}*/