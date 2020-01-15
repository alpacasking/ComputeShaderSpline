using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpacasking
{
    /// <summary>
    /// Curve test
    /// </summary>
    public class CurveTest : MonoBehaviour
    {
        public enum SplineType {
            CatmullRom,
            Bezier,
            BSpline2D,
            BSpline3D,
        }

        public ComputeShader SplineShader;
        //Waypoint
        public GameObject[] GameObjectList;
        //Coordinates of waypoints
        private List<Vector3> TransDataList = new List<Vector3>();

        public int Amount = 20;
        public float Ratio = 0.2f;

        public SplineType Type = SplineType.CatmullRom;

        void Start()
        {
           
        }

        //Gizmos
        void OnDrawGizmos()
        {
            //1 point cannot draw a curve, 2 points are actually straight lines
            if (GameObjectList.Length <= 1 || SplineShader == null) return;

            TransDataList.Clear();
            for (int i = 0; i < GameObjectList.Length; ++i)
            {
                if(GameObjectList[i] == null)
                {
                    return;
                }
                TransDataList.Add(GameObjectList[i].transform.position);
            }

            int minAmount = 1;
            switch (Type)
            {
                case SplineType.CatmullRom:
                    minAmount = 1;
                    break;
                case SplineType.Bezier:
                    minAmount = 2;
                    break;
                case SplineType.BSpline2D:
                    minAmount = 2;
                    break;
                case SplineType.BSpline3D:
                    minAmount = 3;
                    break;
            }

            if (TransDataList != null && TransDataList.Count > minAmount)
            {
                DrawPathHelper(TransDataList.ToArray(), Color.red);
            }
        }

        //Draw a curve
        private void DrawPathHelper(Vector3[] path, Color color)
        {
            int kernel = 0;
            Vector3[] vector3s = path;
            switch (Type)
            {
                case SplineType.CatmullRom:
                    kernel = SplineShader.FindKernel("CatmullRom");
                    vector3s = PathControlPointGenerator(path);
                    break;
                case SplineType.Bezier:
                    kernel = SplineShader.FindKernel("Bezier");
                    break;
                case SplineType.BSpline2D:
                    kernel = SplineShader.FindKernel("BSpline2D");
                    break;
                case SplineType.BSpline3D:
                    vector3s = PathControlPointGenerator(path);
                    kernel = SplineShader.FindKernel("BSpline3D");
                    break;
            }

            //Line Draw:
            int SmoothAmount = path.Length * Amount;

            ComputeBuffer nodePointBuffer = new ComputeBuffer(SmoothAmount, sizeof(float)*3);
            ComputeBuffer controlPointBuffer = new ComputeBuffer(vector3s.Length, sizeof(float) * 3);
            controlPointBuffer.SetData(vector3s);
            SplineShader.SetBuffer(kernel, "ControlPoints", controlPointBuffer);
            SplineShader.SetBuffer(kernel, "NodePoints", nodePointBuffer);
            SplineShader.SetInt("ControlPointAmount", vector3s.Length);
            SplineShader.SetInt("NodeAmount", SmoothAmount);
            SplineShader.Dispatch(kernel, SmoothAmount, 1, 1);
            var nodePoints = new Vector3[SmoothAmount];
            nodePointBuffer.GetData(nodePoints);
            for (int i = 0; i < SmoothAmount; i++)
            {
                Gizmos.color = color;
                Gizmos.DrawSphere(nodePoints[i], Ratio);
            }
            nodePointBuffer.Release();
            controlPointBuffer.Release();
        }

        public static Vector3[] PathControlPointGenerator(Vector3[] path)
        {
            Vector3[] vector3s;
            //populate calculate path;
            int offset = 2;
            vector3s = new Vector3[path.Length + offset];
            Array.Copy(path, 0, vector3s, 1, path.Length);

            //populate start and end control points:
            vector3s[0] = vector3s[1] + (vector3s[1] - vector3s[2]);
            vector3s[vector3s.Length - 1] = vector3s[vector3s.Length - 2] + (vector3s[vector3s.Length - 2] - vector3s[vector3s.Length - 3]);

            //is this a closed, continuous loop? yes? well then so let's make a continuous Catmull-Rom spline!
            if (vector3s[1] == vector3s[vector3s.Length - 2])
            {
                Vector3[] tmpLoopSpline = new Vector3[vector3s.Length];
                Array.Copy(vector3s, tmpLoopSpline, vector3s.Length);
                tmpLoopSpline[0] = tmpLoopSpline[tmpLoopSpline.Length - 3];
                tmpLoopSpline[tmpLoopSpline.Length - 1] = tmpLoopSpline[2];
                vector3s = new Vector3[tmpLoopSpline.Length];
                Array.Copy(tmpLoopSpline, vector3s, tmpLoopSpline.Length);
            }

            return (vector3s);
        }
    }
}