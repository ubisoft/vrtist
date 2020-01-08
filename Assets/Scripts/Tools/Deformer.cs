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
        public float gap = 0.1f;

        private Vector3 minBound = Vector3.positiveInfinity;
        private Vector3 maxBound = Vector3.negativeInfinity;

        private DeformerPlane activePlane = null;
        private bool deforming = false;
        private float initMagnitude;

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

        public void ComputeSelectionBounds()
        {
            planesContainer.SetActive(gameObject.activeSelf && Selection.selection.Count > 0);
            if(Selection.selection.Count == 0) { return;}

            // Get bounds
            minBound = Vector3.positiveInfinity;
            maxBound = Vector3.negativeInfinity;
            foreach (KeyValuePair<int, GameObject> item in Selection.selection)
            {
                MeshFilter meshFilter = item.Value.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.mesh;
                if (null != mesh)
                {
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
                        vertices[i] = item.Value.transform.TransformPoint(vertices[i]);  // to local space
                        vertices[i] = world.InverseTransformPoint(vertices[i]);          // to world game object space

                        //  Compute min and max bounds
                        if (vertices[i].x < minBound.x) { minBound.x = vertices[i].x; }
                        if (vertices[i].y < minBound.y) { minBound.y = vertices[i].y; }
                        if (vertices[i].z < minBound.z) { minBound.z = vertices[i].z; }

                        if (vertices[i].x > maxBound.x) { maxBound.x = vertices[i].x; }
                        if (vertices[i].y > maxBound.y) { maxBound.y = vertices[i].y; }
                        if (vertices[i].z > maxBound.z) { maxBound.z = vertices[i].z; }
                    }
                }
            }

            // Add a small gap getween the object and the bounding box
            Vector3 minGapBound = minBound - new Vector3(gap, gap, gap);
            Vector3 maxGapBound = maxBound + new Vector3(gap, gap, gap);

            // Set planes (depending on their initial rotation)
            // Top
            planes[0].transform.localPosition = new Vector3((maxGapBound.x + minGapBound.x) * 0.5f, maxGapBound.y, (maxGapBound.z + minGapBound.z) * 0.5f);
            planes[0].transform.localScale = new Vector3((maxGapBound.x - minGapBound.x), 1f, (maxGapBound.z - minGapBound.z)) * 0.1f;

            // Bottom
            planes[1].transform.localPosition = new Vector3((maxGapBound.x + minGapBound.x) * 0.5f, minGapBound.y, (maxGapBound.z + minGapBound.z) * 0.5f);
            planes[1].transform.localScale = new Vector3((maxGapBound.x - minGapBound.x), 1f, (maxGapBound.z - minGapBound.z)) * 0.1f;

            // Left
            planes[2].transform.localPosition = new Vector3(minGapBound.x, (maxGapBound.y + minGapBound.y) * 0.5f, (maxGapBound.z + minGapBound.z) * 0.5f);
            planes[2].transform.localScale = new Vector3((maxGapBound.y - minGapBound.y), 1f, (maxGapBound.z - minGapBound.z)) * 0.1f;

            // Right
            planes[3].transform.localPosition = new Vector3(maxGapBound.x, (maxGapBound.y + minGapBound.y) * 0.5f, (maxGapBound.z + minGapBound.z) * 0.5f);
            planes[3].transform.localScale = new Vector3((maxGapBound.y - minGapBound.y), 1f, (maxGapBound.z - minGapBound.z)) * 0.1f;

            // Front
            planes[4].transform.localPosition = new Vector3((maxGapBound.x + minGapBound.x) * 0.5f, (maxGapBound.y + minGapBound.y) * 0.5f, minGapBound.z);
            planes[4].transform.localScale = new Vector3((maxGapBound.x - minGapBound.x), 1f, (maxGapBound.y - minGapBound.y)) * 0.1f;

            // Back
            planes[5].transform.localPosition = new Vector3((maxGapBound.x + minGapBound.x) * 0.5f, (maxGapBound.y + minGapBound.y) * 0.5f, maxGapBound.z);
            planes[5].transform.localScale = new Vector3((maxGapBound.x - minGapBound.x), 1f, (maxGapBound.y - minGapBound.y)) * 0.1f;
        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            // Base selection update
            base.DoUpdate(position, rotation);

            // Boubds
            ComputeSelectionBounds();

            // Deform
            if (activePlane != null)
            {
                VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
                {
                    InitDeformerMatrix();
                    InitTransforms();
                    VRInput.GetControllerTransform(VRInput.rightController, out initControllerPosition, out initControllerRotation);
                    deforming = true;
                    initControllerPosition -= activePlane.opposite.position;
                    initControllerPosition = Vector3.Scale(initControllerPosition, activePlane.direction);
                    initMagnitude = initControllerPosition.magnitude;
                }, () => {
                    deforming = false;
                });

                if(deforming)
                {
                    Vector3 controllerPosition;
                    Quaternion controllerRotation;
                    VRInput.GetControllerTransform(VRInput.rightController, out controllerPosition, out controllerRotation);
                    Vector3 delta = controllerPosition - activePlane.opposite.position;
                    delta = Vector3.Scale(delta, activePlane.direction);
                    float magnitude = delta.magnitude;
                    float scaleFactor = magnitude / initMagnitude;
                    Vector3 scale = new Vector3(
                        activePlane.direction.x == 0 ? 1f : scaleFactor,
                        activePlane.direction.y == 0 ? 1f : scaleFactor,
                        activePlane.direction.z == 0 ? 1f : scaleFactor
                    );

                    ScaleSelection(scale, activePlane.opposite.position, activePlane.opposite.rotation);
                }
            }
        }

        private void InitDeformerMatrix()
        {
            Vector3 initPosition = activePlane.opposite.position;
            Quaternion initRotation = activePlane.opposite.rotation;
            initControllerMatrix = (transform.parent.localToWorldMatrix * Matrix4x4.TRS(initPosition, initRotation, Vector3.one)).inverse;
        }

        public void SetActivePLane(DeformerPlane plane)
        {
            activePlane = plane;
        }
    }
}
