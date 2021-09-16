using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class GoalGizmo : MonoBehaviour
    {

        public GameObject xCurve;
        public GameObject yCurve;
        public GameObject zCurve;

        public RigGoalController Controller;

        private bool isListening;


        public void Init(RigGoalController controller)
        {
            Controller = controller;

            Matrix4x4 targetMatrix = controller.transform.localToWorldMatrix;
            Maths.DecomposeMatrix(targetMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = scale;

            if (!isListening)
            {
                GlobalState.ObjectMovingEvent.AddListener(ResetPosition);
                GlobalState.Animation.onFrameEvent.AddListener(ResetPosition);
                isListening = true;
            }
        }

        public void ResetPosition(GameObject gObject)
        {
            if (Controller == null) return;
            Matrix4x4 targetMatrix = Controller.transform.localToWorldMatrix;
            Maths.DecomposeMatrix(targetMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = scale;
        }

        public void ResetPosition(int frame)
        {
            ResetPosition(Controller.gameObject);
        }

        /// <summary>
        /// Editor call to create the gizmos curves.
        /// </summary>
        [ContextMenu("generate")]
        public void GenerateCurves()
        {
            List<float> cos = new List<float>();
            List<float> sin = new List<float>();

            for (float i = 0; i < 2 * Mathf.PI; i += 0.1f)
            {
                cos.Add(Mathf.Cos(i) * 10);
                sin.Add(Mathf.Sin(i) * 10);
            }

            List<Vector3> curvesX = new List<Vector3>();
            List<Vector3> curvesY = new List<Vector3>();
            List<Vector3> curvesZ = new List<Vector3>();

            for (int j = 0; j < cos.Count; j++)
            {
                curvesX.Add(new Vector3(0, sin[j], cos[j]));
                curvesY.Add(new Vector3(sin[j], 0, cos[j]));
                curvesZ.Add(new Vector3(sin[j], cos[j], 0));
            }

            LineRenderer lineX = xCurve.GetComponent<LineRenderer>();
            LineRenderer lineY = yCurve.GetComponent<LineRenderer>();
            LineRenderer lineZ = zCurve.GetComponent<LineRenderer>();

            lineX.SetPositions(curvesX.ToArray());
            lineX.positionCount = curvesX.Count;

            lineY.SetPositions(curvesY.ToArray());
            lineY.positionCount = curvesY.Count;

            lineZ.SetPositions(curvesZ.ToArray());
            lineZ.positionCount = curvesZ.Count;

            Mesh meshX = new Mesh();
            lineX.BakeMesh(meshX);

            Mesh meshY = new Mesh();
            lineY.BakeMesh(meshY);

            Mesh meshZ = new Mesh();
            lineZ.BakeMesh(meshZ);

            lineX.GetComponent<MeshCollider>().sharedMesh = meshX;
            lineY.GetComponent<MeshCollider>().sharedMesh = meshY;
            lineZ.GetComponent<MeshCollider>().sharedMesh = meshZ;
        }

    }

}