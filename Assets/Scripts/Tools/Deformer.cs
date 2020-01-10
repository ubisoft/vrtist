using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Deformer : Selector
    {
        public Transform world;
        public Transform[] planes;
        public GameObject planesContainer;
        public float gap = 0.0f;
        public SelectorTrigger selectorTrigger;

        private Vector3 minBound = Vector3.positiveInfinity;
        private Vector3 maxBound = Vector3.negativeInfinity;

        private DeformerPlane activePlane = null;
        private bool deforming = false;
        private float initMagnitude;
        private Vector3 initScale;
        private Vector3 planeControllerDelta;

        void Start()
        {
            Init();
        }

        void OnEnable()
        {
            planesContainer.SetActive(true);
        }

        private void OnDisable()
        {
            planesContainer.SetActive(false);
        }

        protected void OnStartDeform()
        {
        }

        protected void OnEndDeform()
        {
            ManageMoveObjectsUndo();
        }

        private Mesh CreatePlaneMesh(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = v1;
            vertices[1] = v2;
            vertices[2] = v3;
            vertices[3] = v4;

            int[] indices = { 0, 1, 2, 0, 2, 3 };
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void SetPlaneCollider(Transform plane, Vector3 size)
        {
            var collider = plane.GetComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;
        }

        public void ComputeSelectionBounds()
        {
            planesContainer.SetActive(gameObject.activeSelf && Selection.selection.Count > 0);
            if (Selection.selection.Count == 0)
            {
                planesContainer.SetActive(false);
                return;
            }

            // Get bounds
            minBound = Vector3.positiveInfinity;
            maxBound = Vector3.negativeInfinity;
            bool foundBounds = false;
            foreach (KeyValuePair<int, GameObject> item in Selection.selection)
            {
                MeshFilter meshFilter = item.Value.GetComponentInChildren<MeshFilter>();
                if (null != meshFilter)
                {
                    Matrix4x4 transform;
                    if(meshFilter.gameObject != item.Value)
                    {
                        transform = world.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                    }
                    else
                    {
                        transform = world.worldToLocalMatrix * item.Value.transform.localToWorldMatrix;
                    }

                    Mesh mesh = meshFilter.mesh;
                    // Get vertices
                    Vector3[] vertices = new Vector3[8];
                    vertices[0] = new Vector3(mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.min.z);
                    vertices[1] = new Vector3(mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.max.z);
                    vertices[2] = new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.min.z);
                    vertices[3] = new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.max.z);
                    vertices[4] = new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.min.z);
                    vertices[5] = new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.max.z);
                    vertices[6] = new Vector3(mesh.bounds.max.x, mesh.bounds.max.y, mesh.bounds.min.z);
                    vertices[7] = new Vector3(mesh.bounds.max.x, mesh.bounds.max.y, mesh.bounds.max.z);

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = transform.MultiplyPoint(vertices[i]);
                        //vertices[i] = item.Value.transform.TransformPoint(vertices[i]);  // to root (absolute) space
                        //vertices[i] = world.InverseTransformPoint(vertices[i]);          // to world game object space

                        //  Compute min and max bounds
                        if (vertices[i].x < minBound.x) { minBound.x = vertices[i].x; }
                        if (vertices[i].y < minBound.y) { minBound.y = vertices[i].y; }
                        if (vertices[i].z < minBound.z) { minBound.z = vertices[i].z; }

                        if (vertices[i].x > maxBound.x) { maxBound.x = vertices[i].x; }
                        if (vertices[i].y > maxBound.y) { maxBound.y = vertices[i].y; }
                        if (vertices[i].z > maxBound.z) { maxBound.z = vertices[i].z; }
                    }
                    foundBounds = true;
                }
            }

            if (!foundBounds)
            {
                planesContainer.SetActive(false);
                return;
            }

            // Add a small gap getween the object and the bounding box
            Vector3 minGapBound = minBound - new Vector3(gap, gap, gap);
            Vector3 maxGapBound = maxBound + new Vector3(gap, gap, gap);

            Vector3 delta = (maxGapBound - minGapBound) * 0.5f;

            // Set planes (depending on their initial rotation)
            // Top
            planes[0].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, maxBound.y, (maxBound.z + minBound.z) * 0.5f);
            planes[0].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, gap, -delta.z), new Vector3(-delta.x, gap, delta.z), new Vector3(delta.x, gap, delta.z), new Vector3(delta.x, gap, -delta.z));
            SetPlaneCollider(planes[0], new Vector3(delta.x * 2f, 0.5f, delta.z * 2f));

            // Bottom
            planes[1].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, minBound.y, (maxBound.z + minBound.z) * 0.5f);
            planes[1].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -gap, -delta.z), new Vector3(delta.x, -gap, delta.z), new Vector3(-delta.x, -gap, delta.z), new Vector3(-delta.x, -gap, -delta.z));
            SetPlaneCollider(planes[1], new Vector3(delta.x * 2f, 0.5f, delta.z * 2f));

            // Left
            planes[2].transform.localPosition = new Vector3(minBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
            planes[2].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-gap, -delta.y, -delta.z), new Vector3(-gap, -delta.y, delta.z), new Vector3(-gap, delta.y, delta.z), new Vector3(-gap, delta.y, -delta.z));
            SetPlaneCollider(planes[2], new Vector3(0.5f, delta.y * 2f, delta.z * 2f));

            // Right
            planes[3].transform.localPosition = new Vector3(maxBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
            planes[3].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(gap, delta.y, -delta.z), new Vector3(gap, delta.y, delta.z), new Vector3(gap, -delta.y, delta.z), new Vector3(gap, -delta.y, -delta.z));
            SetPlaneCollider(planes[3], new Vector3(0.5f, delta.y * 2f, delta.z * 2f));

            // Front
            planes[4].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, minBound.z);
            planes[4].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, -delta.y,-gap), new Vector3(-delta.x, delta.y, -gap), new Vector3(delta.x, delta.y, -gap), new Vector3(delta.x, -delta.y, -gap));
            SetPlaneCollider(planes[4], new Vector3(delta.x * 2f, delta.y * 2f, 0.5f));

            // Back
            planes[5].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, maxBound.z);
            planes[5].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -delta.y, gap), new Vector3(delta.x, delta.y, gap), new Vector3(-delta.x, delta.y, gap), new Vector3(-delta.x, -delta.y, gap));
            SetPlaneCollider(planes[5], new Vector3(delta.x * 2f, delta.y * 2f, 0.5f));

            planesContainer.SetActive(true);
        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            // Base selection update
            base.DoUpdate(position, rotation);
            

            // Deform
            if (activePlane != null)
            {

                VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
                {
                    InitDeformerMatrix();
                    InitTransforms();

                    Vector3 initControllerPosition;
                    Quaternion initControllerRotation;
                    VRInput.GetControllerTransform(VRInput.rightController, out initControllerPosition, out initControllerRotation);
                    initControllerPosition = transform.parent.TransformPoint(initControllerPosition);

                    initControllerPosition = world.InverseTransformPoint(initControllerPosition);
                    initControllerPosition = Vector3.Scale(initControllerPosition, activePlane.direction);
                    initControllerPosition = world.TransformPoint(initControllerPosition);

                    planeControllerDelta = initControllerPosition - activePlane.transform.position;

                    deforming = true;

                    Vector3 initDelta = activePlane.transform.position - activePlane.opposite.position;
                    initMagnitude = initDelta.magnitude;

                    OnStartDeform();
                }, () =>
                {
                    deforming = false;
                    SetActivePLane(null);
                    OnEndDeform();
                });
                
            }

            if(deforming)
            {
                Vector3 controllerPosition;
                Quaternion controllerRotation;
                VRInput.GetControllerTransform(VRInput.rightController, out controllerPosition, out controllerRotation);
                controllerPosition = transform.parent.TransformPoint(controllerPosition);

                controllerPosition = world.InverseTransformPoint(controllerPosition);
                controllerPosition = Vector3.Scale(controllerPosition, activePlane.direction);
                controllerPosition = world.TransformPoint(controllerPosition);

                controllerPosition -= planeControllerDelta;

                Vector3 delta = controllerPosition - activePlane.opposite.position;
                float magnitude = delta.magnitude;

                float scaleFactor = magnitude / initMagnitude;

                Vector3 scale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

                Matrix4x4 transformationMatrix = activePlane.opposite.localToWorldMatrix * scaleMatrix;

                TransformSelection(transformationMatrix);
                
            }

            // Bounds
            ComputeSelectionBounds();
        }

        private void InitDeformerMatrix()
        {            
            Matrix4x4 oppositeMatrix = activePlane.opposite.localToWorldMatrix;
            initScale = new Vector3(oppositeMatrix.GetColumn(0).magnitude, oppositeMatrix.GetColumn(1).magnitude, oppositeMatrix.GetColumn(2).magnitude);
            initTransformation = activePlane.opposite.worldToLocalMatrix;
        }

        public DeformerPlane ActivePlane()
        {
            return activePlane;
        }
        public void SetActivePLane(DeformerPlane plane)
        {
            if (!deforming)
            {
                if(activePlane)
                    activePlane.gameObject.GetComponent<MeshRenderer>().material.SetColor("_UnlitColor", new Color(128f / 255f, 128f / 255f, 128f / 255f, 0.2f));

                activePlane = plane;
                if (plane != null)
                {
                    Color selectColor = selectionColor;
                    selectColor.a = 0.2f;
                    activePlane.gameObject.GetComponent<MeshRenderer>().material.SetColor("_UnlitColor", selectColor);
                }

                selectorTrigger.enabled = (plane == null);
            }
        }
    }
}
