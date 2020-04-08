using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Selector : SelectorBase
    {
        [Header("Movement & Snapping parameters")]
        public UICheckbox snapToGridCheckbox = null;
        public UISlider snapGridSizeSlider = null;
        public UICheckbox moveOnXCheckbox = null;
        public UICheckbox moveOnYCheckbox = null;
        public UICheckbox moveOnZCheckbox = null;

        public UICheckbox snapRotationCheckbox = null;
        public UISlider snapAngleSlider = null;
        public UICheckbox turnAroundXCheckbox = null;
        public UICheckbox turnAroundYCheckbox = null;
        public UICheckbox turnAroundZCheckbox = null;

        protected bool snapToGrid = false;
        protected float snapPrecision = 1f;    // grid size 1 meter
        protected float snapGap = 0.05f;       // 
        protected bool moveOnX = true;
        protected bool moveOnY = true;
        protected bool moveOnZ = true;

        protected bool snapRotation = false;
        protected float snapAngle = 45f;       // in degrees
        protected float snapAngleGap = 0.2f;   // percentage
        protected bool turnAroundX = true;
        protected bool turnAroundY = true;
        protected bool turnAroundZ = true;

        [Header("Deformer Parameters")]
        public Transform container;
        public Transform[] planes;
        public GameObject planesContainer;
        [CentimeterFloat] public float cameraSpaceGap = 0.01f;
        [CentimeterFloat] public float collidersThickness = 0.05f;
        public SelectorTrigger selectorTrigger;
        public Transform world;
        public UICheckbox uniformScaleCheckbox = null;
        public bool uniformScale = false;

        private Matrix4x4 initPlaneContainerMatrix;
        private Matrix4x4 initInversePlaneContainerMatrix;
        private Matrix4x4 initOppositeMatrix;

        private Vector3 minBound = Vector3.positiveInfinity;
        private Vector3 maxBound = Vector3.negativeInfinity;

        private DeformerPlane activePlane = null;
        private bool deforming = false;
        private float initMagnitude;
        private Vector3 planeControllerDelta;

        private bool deformEnabled = false;

        void Start() 
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();
            InitUIPanel();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitUIPanel();
            if(null != planesContainer) { planesContainer.SetActive(false); }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if(null != planesContainer) { planesContainer.SetActive(false); }
        }

        public void SetSnapToGrid(bool value)
        {
            snapToGrid = value;
            if(null != snapGridSizeSlider) { snapGridSizeSlider.Disabled = !snapToGrid; }
        }

        public void OnChangeSnapGridSize(float value)
        {
            snapPrecision = value;
        }

        public void SetMoveOnX(bool value)
        {
            moveOnX = value;
        }

        public void SetMoveOnY(bool value)
        {
            moveOnY = value;
        }

        public void SetMoveOnZ(bool value)
        {
            moveOnZ = value;
        }

        public void SetSnapRotation(bool value)
        {
            snapRotation = value;
        }

        public void OnChangeSnapAngle(float value)
        {
            snapAngle = value;
        }

        public void SetTurnAroundX(bool value)
        {
            turnAroundX = value;
        }

        public void SetTurnAroundY(bool value)
        {
            turnAroundY = value;
        }

        public void SetTurnAroundZ(bool value)
        {
            turnAroundZ = value;
        }

        public void EnableDeformMode(bool enabled)
        {
            deformEnabled = enabled;
            if(!enabled)
            {
                planesContainer.SetActive(false);
            }
        }

        public void SetUniformScale(bool value)
        {
            uniformScale = value;
        }

        protected virtual void InitUIPanel()
        {
            if(null != snapToGridCheckbox) { snapToGridCheckbox.Checked = snapToGrid; }
            if(null != snapGridSizeSlider)
            {
                snapGridSizeSlider.Value = snapPrecision;
                snapGridSizeSlider.Disabled = !snapToGrid;
            }
            if(null != moveOnXCheckbox) { moveOnXCheckbox.Checked = moveOnX; }
            if(null != moveOnYCheckbox) { moveOnYCheckbox.Checked = moveOnY; }
            if(null != moveOnZCheckbox) { moveOnZCheckbox.Checked = moveOnZ; }

            if(null != snapRotationCheckbox) { snapRotationCheckbox.Checked = snapRotation; }
            if(null != snapAngleSlider) {
                snapAngleSlider.Value = snapAngle;
                snapAngleSlider.Disabled = !snapRotation;
            }
            if(null != turnAroundXCheckbox) { turnAroundXCheckbox.Checked = turnAroundX; }
            if(null != turnAroundYCheckbox) { turnAroundYCheckbox.Checked = turnAroundY; }
            if(null != turnAroundZCheckbox) { turnAroundZCheckbox.Checked = turnAroundZ; }
        }

        public override void OnPreTransformSelection(Matrix4x4 current, ref Matrix4x4 transformed)
        {
            // Constrain movement
            if(!moveOnX || !moveOnY || !moveOnZ || snapToGrid)
            {
                Vector4 oldColumn = current.GetColumn(3);
                Vector4 column = transformed.GetColumn(3);

                float absWorldScale = Mathf.Abs(GlobalState.worldScale);
                Vector3 position = new Vector3(column.x, column.y, column.z);
                Vector3 roundedPosition = new Vector3(
                    Mathf.Round(column.x / snapPrecision) * snapPrecision,
                    Mathf.Round(column.y / snapPrecision) * snapPrecision,
                    Mathf.Round(column.z / snapPrecision) * snapPrecision
                );

                if(!moveOnX) { column.x = oldColumn.x; }
                else if(snapToGrid && Mathf.Abs(position.x - roundedPosition.x) <= snapGap / absWorldScale) {
                    column.x = roundedPosition.x;
                }

                if(!moveOnY) { column.y = oldColumn.y; }
                else if(snapToGrid && Mathf.Abs(position.y - roundedPosition.y) <= snapGap / absWorldScale) {
                    column.y = roundedPosition.y;
                }

                if(!moveOnZ) { column.z = oldColumn.z; }
                else if(snapToGrid && Mathf.Abs(position.z - roundedPosition.z) <= snapGap / absWorldScale) {
                    column.z = roundedPosition.z;
                }

                transformed.SetColumn(3, column);
            }

            // Constrain rotation
            if(!turnAroundX || !turnAroundY || !turnAroundZ || snapRotation)
            {
                Quaternion oldRotation = current.rotation;
                Quaternion rotation = transformed.rotation;
                Vector3 OldEulerAngles = oldRotation.eulerAngles;
                Vector3 eulerAngles = rotation.eulerAngles;
                Vector3 roundedAngles = new Vector3(
                    Mathf.Round(eulerAngles.x / snapAngle) * snapAngle,
                    Mathf.Round(eulerAngles.y / snapAngle) * snapAngle,
                    Mathf.Round(eulerAngles.z / snapAngle) * snapAngle
                );

                if(!turnAroundX) { eulerAngles.x = OldEulerAngles.x; }
                else if(snapRotation && Mathf.Abs(eulerAngles.x - roundedAngles.x) <= snapAngleGap * snapAngle) {
                    eulerAngles.x = roundedAngles.x;
                }

                if(!turnAroundY) { eulerAngles.y = OldEulerAngles.y; }
                else if(snapRotation && Mathf.Abs(eulerAngles.y - roundedAngles.y) <= snapAngleGap * snapAngle) {
                    eulerAngles.y = roundedAngles.y;
                }

                if(!turnAroundZ) { eulerAngles.z = OldEulerAngles.z; }
                else if(snapRotation && Mathf.Abs(eulerAngles.z - roundedAngles.z) <= snapAngleGap * snapAngle) {
                    eulerAngles.z = roundedAngles.z;
                }

                Vector3 position = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                rotation = Quaternion.Euler(eulerAngles);
                Vector3 scale = new Vector3(transformed.GetColumn(0).magnitude, transformed.GetColumn(1).magnitude, transformed.GetColumn(2).magnitude);
                transformed.SetTRS(position, rotation, scale);
            }
        }

        public override void OnSelectorTriggerEnter(Collider other)
        {
            Tooltips.SetTooltipVisibility(gripTooltip, true);
        }

        public override void OnSelectorTriggerExit(Collider other)
        {
            Tooltips.SetTooltipVisibility(gripTooltip, false);
        }

        protected void OnStartDeform()
        {
            deforming = true;
        }

        protected void OnEndDeform()
        {
            deforming = false;
            SetActivePLane(null);

            ManageMoveObjectsUndo();
        }

        private Mesh CreatePlaneMesh(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = v1;
            vertices[1] = v2;
            vertices[2] = v3;
            vertices[3] = v4;

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(0, 1);

            int[] indices = { 0, 1, 2, 0, 2, 3 };
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void SetPlaneCollider(Transform plane, Vector3 center, Vector3 size)
        {
            var collider = plane.GetComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        // Tell whether the current selection contains a hierarchical object (mesh somewhere in children) or not.
        // Camera and lights are known hierarchical objects.
        // TODO: check for multiselection of a light and and simple primitive for example
        private bool IsHierarchical()
        {
            foreach(KeyValuePair<int, GameObject> item in Selection.selection)
            {
                GameObject gObject = item.Value;
                if(gObject.GetComponent<LightController>() != null || gObject.GetComponent<CameraController>() != null)
                {
                    return true;
                }
                MeshFilter meshFilter = gObject.GetComponentInChildren<MeshFilter>();
                if(meshFilter.gameObject != gObject)
                {
                    return true;
                }
            }
            return false;
        }

        public void ComputeSelectionBounds()
        {
            planesContainer.SetActive(gameObject.activeSelf && Selection.selection.Count > 0);
            if(Selection.selection.Count == 0 || Selection.IsHandleSelected())
            {
                planesContainer.SetActive(false);
                return;
            }

            // Get bounds
            minBound = Vector3.positiveInfinity;
            maxBound = Vector3.negativeInfinity;
            bool foundBounds = false;
            int selectionCount = Selection.selection.Count;

            bool foundHierarchicalObject = false;
            if(selectionCount == 1)
            {
                foundHierarchicalObject = IsHierarchical();
            }

            if(selectionCount == 1 && !foundHierarchicalObject)
            {
                // NOTE: pourquoi un foreach si on a un seul element?
                foreach(KeyValuePair<int, GameObject> item in Selection.selection)
                {
                    Transform transform = item.Value.GetComponentInChildren<MeshFilter>().transform;
                    planesContainer.transform.parent = transform.parent;
                    planesContainer.transform.localPosition = transform.localPosition;
                    planesContainer.transform.localRotation = transform.localRotation;
                    planesContainer.transform.localScale = transform.localScale;
                }
            }
            else
            {
                planesContainer.transform.parent = container;
                planesContainer.transform.localPosition = Vector3.zero;
                planesContainer.transform.localRotation = Quaternion.identity;
                planesContainer.transform.localScale = Vector3.one;
            }

            foreach(KeyValuePair<int, GameObject> item in Selection.selection)
            {
                MeshFilter meshFilter = item.Value.GetComponentInChildren<MeshFilter>();
                if(null != meshFilter)
                {
                    Matrix4x4 transform;
                    if(selectionCount > 1 || foundHierarchicalObject)
                    {
                        if(meshFilter.gameObject != item.Value)
                        {
                            transform = container.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                        }
                        else
                        {
                            transform = container.worldToLocalMatrix * item.Value.transform.localToWorldMatrix;
                        }
                    }
                    else
                    {
                        transform = Matrix4x4.identity;
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

                    for(int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = transform.MultiplyPoint(vertices[i]);
                        //  Compute min and max bounds
                        if(vertices[i].x < minBound.x) { minBound.x = vertices[i].x; }
                        if(vertices[i].y < minBound.y) { minBound.y = vertices[i].y; }
                        if(vertices[i].z < minBound.z) { minBound.z = vertices[i].z; }

                        if(vertices[i].x > maxBound.x) { maxBound.x = vertices[i].x; }
                        if(vertices[i].y > maxBound.y) { maxBound.y = vertices[i].y; }
                        if(vertices[i].z > maxBound.z) { maxBound.z = vertices[i].z; }
                    }
                    foundBounds = true;
                }
            }

            if(!foundBounds)
            {
                planesContainer.SetActive(false);
                return;
            }

            Vector3 bs = planesContainer.transform.localScale; // boundsScale

            // Collider Scale
            Vector3 cs = new Vector3(
                collidersThickness * (1.0f / world.localScale.x) * (1.0f / bs.x),
                collidersThickness * (1.0f / world.localScale.y) * (1.0f / bs.y),
                collidersThickness * (1.0f / world.localScale.z) * (1.0f / bs.z)
            );

            // GAP: fixed in camera space. Scales with world and objet scales, inverse.
            Vector3 g = new Vector3(
                cameraSpaceGap * (1.0f / world.localScale.x) * (1.0f / bs.x),
                cameraSpaceGap * (1.0f / world.localScale.y) * (1.0f / bs.y),
                cameraSpaceGap * (1.0f / world.localScale.z) * (1.0f / bs.z)
            );

            Vector3 minGapBound = minBound - new Vector3(g.x, g.y, g.z);
            Vector3 maxGapBound = maxBound + new Vector3(g.x, g.y, g.z);

            Vector3 delta = (maxGapBound - minGapBound) * 0.5f;

            // Set planes (depending on their initial rotation)
            // Top
            planes[0].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, maxBound.y, (maxBound.z + minBound.z) * 0.5f);
            planes[0].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, g.y, -delta.z), new Vector3(-delta.x, g.y, delta.z), new Vector3(delta.x, g.y, delta.z), new Vector3(delta.x, g.y, -delta.z));
            SetPlaneCollider(planes[0], new Vector3(0, g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Bottom
            planes[1].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, minBound.y, (maxBound.z + minBound.z) * 0.5f);
            planes[1].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -g.y, -delta.z), new Vector3(delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, -delta.z));
            SetPlaneCollider(planes[1], new Vector3(0, -g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Left
            planes[2].transform.localPosition = new Vector3(minBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
            planes[2].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-g.x, -delta.y, -delta.z), new Vector3(-g.x, -delta.y, delta.z), new Vector3(-g.x, delta.y, delta.z), new Vector3(-g.x, delta.y, -delta.z));
            SetPlaneCollider(planes[2], new Vector3(-g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Right
            planes[3].transform.localPosition = new Vector3(maxBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
            planes[3].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(g.x, delta.y, -delta.z), new Vector3(g.x, delta.y, delta.z), new Vector3(g.x, -delta.y, delta.z), new Vector3(g.x, -delta.y, -delta.z));
            SetPlaneCollider(planes[3], new Vector3(g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Front
            planes[4].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, minBound.z);
            planes[4].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, -delta.y, -g.z), new Vector3(-delta.x, delta.y, -g.z), new Vector3(delta.x, delta.y, -g.z), new Vector3(delta.x, -delta.y, -g.z));
            SetPlaneCollider(planes[4], new Vector3(0, 0, -g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));

            // Back
            planes[5].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, maxBound.z);
            planes[5].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -delta.y, g.z), new Vector3(delta.x, delta.y, g.z), new Vector3(-delta.x, delta.y, g.z), new Vector3(-delta.x, -delta.y, g.z));
            SetPlaneCollider(planes[5], new Vector3(0, 0, g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));

            planesContainer.SetActive(true);
        }

        protected Vector3 FilterControllerDirection()
        {
            Vector3 controllerPosition;
            Quaternion controllerRotation;
            VRInput.GetControllerTransform(VRInput.rightController, out controllerPosition, out controllerRotation);
            controllerPosition = rightHandle.parent.TransformPoint(controllerPosition); // controller in absolute coordinates

            controllerPosition = initInversePlaneContainerMatrix.MultiplyPoint(controllerPosition);     //controller in planesContainer coordinates
            controllerPosition = Vector3.Scale(controllerPosition, activePlane.direction);              // apply direction (local to planeContainer)
            controllerPosition = initPlaneContainerMatrix.MultiplyPoint(controllerPosition);            // back to absolute coordinates
            return controllerPosition;
        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            // Base selection update
            base.DoUpdate(position, rotation);

            // Deform
            if(deformEnabled && activePlane != null)
            {
                VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
                {
                    InitDeformerMatrix();
                    InitTransforms();

                    planeControllerDelta = FilterControllerDirection() - activePlane.transform.position; // in absolute coordinates

                    Vector3 initDelta = activePlane.transform.position - activePlane.opposite.position;
                    initMagnitude = initDelta.magnitude; // initial scale value

                    OnStartDeform();
                }, () =>
                {
                    OnEndDeform();
                });

            }

            if(deformEnabled && deforming)
            {
                Vector3 controllerPosition = FilterControllerDirection();
                controllerPosition -= planeControllerDelta;

                Vector3 delta = controllerPosition - activePlane.opposite.position;
                float magnitude = delta.magnitude;

                float scaleFactor = magnitude / initMagnitude;

                Vector3 scale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

                int selectionCount = Selection.selection.Count;
                bool foundLightOrCamera = false;
                if(selectionCount == 1)
                {
                    foundLightOrCamera = IsHierarchical();
                }

                bool scaleAll = Selection.selection.Count != 1 || foundLightOrCamera || uniformScale;
                if(!scaleAll)
                {
                    scale = new Vector3(
                        activePlane.direction.x == 0f ? 1f : scale.x,
                        activePlane.direction.y == 0f ? 1f : scale.y,
                        activePlane.direction.z == 0f ? 1f : scale.z
                    );
                }

                Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
                Matrix4x4 transformationMatrix = initOppositeMatrix * scaleMatrix;

                TransformSelection(transformationMatrix);
            }

            // Bounds
            if(deformEnabled)
                ComputeSelectionBounds();
        }

        protected override void ShowTool(bool show)
        {
            Transform sphere = gameObject.transform.Find("Sphere");
            if(sphere != null)
            {
                sphere.gameObject.SetActive(show);
            }

            if(rightController != null)
            {
                rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        private void InitDeformerMatrix()
        {
            initTransformation = activePlane.opposite.worldToLocalMatrix;
            initPlaneContainerMatrix = planesContainer.transform.localToWorldMatrix;
            initInversePlaneContainerMatrix = planesContainer.transform.worldToLocalMatrix;
            initOppositeMatrix = activePlane.opposite.localToWorldMatrix;
        }

        public DeformerPlane ActivePlane()
        {
            return activePlane;
        }
        public void SetActivePLane(DeformerPlane plane)
        {
            if(!deforming)
            {
                if(activePlane)
                    activePlane.gameObject.GetComponent<MeshRenderer>().material.SetColor("_PlaneColor", new Color(128f / 255f, 128f / 255f, 128f / 255f, 0.2f));

                activePlane = plane;
                if(plane != null)
                {
                    Color selectColor = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.2f);
                    activePlane.gameObject.GetComponent<MeshRenderer>().material.SetColor("_PlaneColor", selectColor);

                    Tooltips.SetTooltipVisibility(triggerTooltip, true);
                }
                else
                {
                    Tooltips.SetTooltipVisibility(triggerTooltip, false);
                }

                selectorTrigger.enabled = (plane == null);
            }
        }
    }
}
