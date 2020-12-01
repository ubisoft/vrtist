using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class VolumeController : ParametersController
    {
        public VolumeParameters parameters = new VolumeParameters();
        public override Parameters GetParameters() { return parameters; }

        private LineRenderer linesFront = null;
        private LineRenderer linesBack = null;
        private List<GameObject> spheres = null;

        private void Awake()
        {
            Init();
        }

        void Start()
        {
            Init();
        }

        private void Init()
        {
            GameObject linesFrontGO = new GameObject();
            linesFrontGO.transform.parent = transform;
            linesFrontGO.transform.localPosition = Vector3.zero;
            linesFront = linesFrontGO.AddComponent<LineRenderer>();
            linesFront.positionCount = 5;
            linesFront.startWidth = 0.001f;
            linesFront.endWidth = 0.001f;

            GameObject linesBackGO = new GameObject();
            linesBackGO.transform.parent = transform;
            linesBackGO.transform.localPosition = Vector3.zero;
            linesBack = linesBackGO.AddComponent<LineRenderer>();
            linesBack.positionCount = 5;
            linesBack.startWidth = 0.001f;
            linesBack.endWidth = 0.001f;

            spheres = new List<GameObject>();
        }

        private void Update()
        {
            Bounds b = parameters.bounds;

            Vector3 C = b.center;
            Vector3 E = b.extents;

            Vector3 ftl = transform.TransformPoint(C + new Vector3(-E.x,  E.y, -E.z));
            Vector3 ftr = transform.TransformPoint(C + new Vector3( E.x,  E.y, -E.z));
            Vector3 fbl = transform.TransformPoint(C + new Vector3(-E.x, -E.y, -E.z));
            Vector3 fbr = transform.TransformPoint(C + new Vector3( E.x, -E.y, -E.z));
            Vector3 btl = transform.TransformPoint(C + new Vector3(-E.x,  E.y,  E.z));
            Vector3 btr = transform.TransformPoint(C + new Vector3( E.x,  E.y,  E.z));
            Vector3 bbl = transform.TransformPoint(C + new Vector3(-E.x, -E.y,  E.z));
            Vector3 bbr = transform.TransformPoint(C + new Vector3( E.x, -E.y,  E.z));
            Vector3 center = transform.TransformPoint(C);

            // NOTE: world space points.
            Debug.DrawLine(ftl, ftr);
            Debug.DrawLine(ftr, fbr);
            Debug.DrawLine(fbr, fbl);
            Debug.DrawLine(fbl, ftl);

            Debug.DrawLine(btl, btr);
            Debug.DrawLine(btr, bbr);
            Debug.DrawLine(bbr, bbl);
            Debug.DrawLine(bbl, btl);

            Debug.DrawLine(ftl, btl);
            Debug.DrawLine(ftr, btr);
            Debug.DrawLine(fbl, bbl);
            Debug.DrawLine(fbr, bbr);

            if (linesFront != null ) linesFront.SetPositions(new Vector3[] { ftl, ftr, fbr, fbl, ftl });
            if (linesBack != null) linesBack.SetPositions(new Vector3[] { btl, btr, bbr, bbl, btl });

#if DO_NOT_DRAW_IT
            float[,,] field = parameters.field;
            float step = parameters.stepSize;
            Vector3 origin = parameters.origin;
            Vector3 boundsOriginCorner = C + new Vector3(-E.x, -E.y, -E.z); // start from front-bottom-left point. Goes right-up-back (+X/Y/Z)

            // minimum 2x2x2
            if (field.GetLength(0) >= 2 && field.GetLength(1) >= 2 && field.GetLength(2) >= 2)
            {
                //Debug.DrawLine(origin, boundsOriginCorner, Color.red);

                int nbSpheres = field.Length;
                if (nbSpheres != spheres.Count)
                {
                    foreach (GameObject go in spheres) { GameObject.Destroy(go); }
                    spheres.Clear();
                    spheres = new List<GameObject>();
                    for (int i = 0; i < nbSpheres; ++i)
                    {
                        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        s.transform.parent = transform;
                        s.transform.localPosition = Vector3.zero;
                        s.transform.localScale = Vector3.zero;
                        s.SetActive(false);
                        spheres.Add(s);
                    }
                }

                for (int z = 0; z < field.GetLength(0); ++z)
                {
                    for (int y = 0; y < field.GetLength(1); ++y)
                    {
                        for (int x = 0; x < field.GetLength(2); ++x)
                        {
                            Vector3 P =  transform.TransformPoint(origin + new Vector3((float)x * step,     (float)y * step,     (float)z * step));
                            Vector3 Px = transform.TransformPoint(origin + new Vector3((float)(x+1) * step, (float)y * step,     (float)z * step));
                            Vector3 Py = transform.TransformPoint(origin + new Vector3((float)x * step,     (float)(y+1) * step, (float)z * step));
                            Vector3 Pz = transform.TransformPoint(origin + new Vector3((float)x * step,     (float)y * step,     (float)(z+1) * step));

                            // Draw grid as LINES
                            //if (x < field.GetLength(2) - 1) Debug.DrawLine(P, Px);
                            //if (y < field.GetLength(1) - 1) Debug.DrawLine(P, Py);
                            //if (z < field.GetLength(0) - 1) Debug.DrawLine(P, Pz);

                            float value = field[z, y, x];

                            // Draw field values as SPHERES
                            float tmpMaxSize = 0.03f;
                            int sphereIndex = x + y * field.GetLength(2) + z * field.GetLength(1) * field.GetLength(2);
                            spheres[sphereIndex].transform.position = P;
                            float scale = value * tmpMaxSize;
                            spheres[sphereIndex].transform.localScale = scale * Vector3.one;
                            if (scale > 0.005f && !spheres[sphereIndex].activeSelf)
                                spheres[sphereIndex].SetActive(true);
                            if (scale <= 0.005f && spheres[sphereIndex].activeSelf)
                                spheres[sphereIndex].SetActive(false);
                        }
                    }
                }
            }
#endif
        }
    }
}
