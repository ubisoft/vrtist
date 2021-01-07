using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorBase : ToolBase
    {
        [Header("Selector Parameters")]
        //[SerializeField] protected Transform world;
        [SerializeField] protected Material selectionMaterial;
        [SerializeField] private float deadZoneDistance = 0.005f;
        [SerializeField] private NavigationOptions navigation;
        [SerializeField] protected UICheckbox lockedCheckbox;

        float selectorRadius;
        protected Color selectionColor = new Color(0f, 167f / 255f, 1f);
        protected Color eraseColor = new Color(1f, 0f, 0f);

        protected Dictionary<GameObject, Matrix4x4> initParentMatrix = new Dictionary<GameObject, Matrix4x4>();
        protected Dictionary<GameObject, Vector3> initPositions = new Dictionary<GameObject, Vector3>();
        protected Dictionary<GameObject, Quaternion> initRotations = new Dictionary<GameObject, Quaternion>();
        protected Dictionary<GameObject, Vector3> initScales = new Dictionary<GameObject, Vector3>();
        protected Dictionary<GameObject, float> initFocals = new Dictionary<GameObject, float>();
        protected Vector3 initControllerPosition;
        protected Quaternion initControllerRotation;
        protected Matrix4x4 initMouthPieceWorldToLocal;
        protected Ray bottomRay;

        public enum SelectorModes { Select = 0, Eraser }
        public SelectorModes mode = SelectorModes.Select;

        const float deadZone = 0.3f;

        float scale = 1f;
        bool outOfDeadZone = false;
        private CommandGroup gripCmdGroup = null;
        private bool Gripping { get { return null != gripCmdGroup; } }


        protected bool gripPrevented = false;
        protected bool gripInterrupted = false;

        protected GameObject triggerTooltip;
        protected GameObject gripTooltip;
        protected GameObject joystickTooltip;

        protected Dopesheet dopesheet;
        protected UIShotManager shotManager;

        protected SelectorTrigger selectorTrigger;

        private CommandSetValue<float> cameraFocalCommand = null;
        private bool joystickScaling = false;

        private GameObject ATooltip = null;
        private string prevATooltipText;

        // snap parameters
        [Header("Snap Parameters")]
        bool isSnapping = false;
        private float snapDistance = 0.1f;
        protected Transform rightHanded;
        private Transform[] planes;
        protected GameObject planesContainer;
        [CentimeterFloat] public float cameraSpaceGap = 0.01f;
        [CentimeterFloat] public float collidersThickness = 0.05f;
        private Vector3 minBound = Vector3.positiveInfinity;
        private Vector3 maxBound = Vector3.negativeInfinity;
        private bool hasBounds = false;
        private Vector3[] planePositions;
        private Matrix4x4 planeContainerMatrix;

        struct ControllerDamping
        {
            public ControllerDamping(float time, Vector3 position, Quaternion rotation)
            {
                this.time = time;
                this.position = position;
                this.rotation = rotation;
            }

            public float time;
            public Vector3 position;
            public Quaternion rotation;
        }
        List<ControllerDamping> damping = new List<ControllerDamping>();
        protected Vector3 rightControllerPosition;
        protected Quaternion rightControllerRotation;

        void Start()
        {
            Init();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnSelectMode();
            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        protected override void OnDisable()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
            if (Gripping)
                OnEndGrip();
            EndUndoGroup(); // secu
            SubmitCameraFocalCommand();
            base.OnDisable();
        }

        protected void SubmitCameraFocalCommand()
        {
            if (null != cameraFocalCommand)
            {
                cameraFocalCommand.Submit();
                cameraFocalCommand = null;
            }
        }

        public virtual void OnSelectorTriggerEnter(Collider other)
        {
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, true);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, true);
        }

        public virtual void OnSelectorTriggerExit(Collider other)
        {
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
        }

        protected override void Init()
        {
            base.Init();

            SetTooltips();

            selectorRadius = mouthpiece.localScale.x;
            mouthpiece.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            selectorTrigger = mouthpiece.GetComponent<SelectorTrigger>();

            updateButtonsColor();

            Selection.selectionMaterial = selectionMaterial;

            dopesheet = GameObject.FindObjectOfType<Dopesheet>();
            UnityEngine.Assertions.Assert.IsNotNull(dopesheet);

            shotManager = GameObject.FindObjectOfType<UIShotManager>();
            UnityEngine.Assertions.Assert.IsNotNull(shotManager);

            GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);

            // bounding box
            rightHanded = Utils.FindWorld().transform.Find("RightHanded");
            planesContainer = rightHanded.Find("DeformerPlanes").gameObject;
            planes = new Transform[6];
            planes[0] = planesContainer.transform.Find("Top");
            planes[1] = planesContainer.transform.Find("Bottom");
            planes[2] = planesContainer.transform.Find("Left");
            planes[3] = planesContainer.transform.Find("Right");
            planes[4] = planesContainer.transform.Find("Front");
            planes[5] = planesContainer.transform.Find("Back");
        }
        private void OnAnimationStateChanged(AnimationState state)
        {
            if (null == ATooltip)
                return;

            if (state == AnimationState.Recording)
            {
                Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Stop Record");
            }
            else
            {
                Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Duplicate");
            }
        }

        public override void SetTooltips()
        {
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Duplicate");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Secondary, Tooltips.Action.Push, "Switch Tool");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Trigger, Tooltips.Action.Push, "Select");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Grip, Tooltips.Action.Push, "Move");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Joystick, Tooltips.Action.Push, "Scale");
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Joystick, false);
        }

        protected override void DoUpdate()
        {
            if (VRInput.GetValue(VRInput.primaryController, CommonUsages.grip) <= deadZone)
            {
                if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                {
                    // Change selector size
                    Vector2 val = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                    if (val != Vector2.zero)
                    {
                        float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                        if (val.y > deadZone) { selectorRadius *= scaleFactor; }
                        if (val.y < -deadZone) { selectorRadius /= scaleFactor; }
                        selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                        mouthpiece.localScale = new Vector3(selectorRadius, selectorRadius, selectorRadius);
                    }
                }
            }

            switch (mode)
            {
                case SelectorModes.Select: UpdateSelect(); break;
                case SelectorModes.Eraser: UpdateEraser(); break;
            }
        }

        // Tell whether the current selection contains a hierarchical object (mesh somewhere in children) or not.
        // Camera and lights are known hierarchical objects.
        // TODO: check for multiselection of a light and and simple primitive for example
        protected bool IsHierarchical(List<GameObject> objects)
        {
            foreach (GameObject gObject in objects)
            {
                if (gObject.GetComponent<LightController>() != null || gObject.GetComponent<CameraController>() != null)
                {
                    return true;
                }
                MeshFilter meshFilter = gObject.GetComponentInChildren<MeshFilter>();
                if (meshFilter.gameObject != gObject)
                {
                    return true;
                }
            }
            return false;
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

        protected void InitTransforms()
        {
            initParentMatrix.Clear();
            initPositions.Clear();
            initRotations.Clear();
            initScales.Clear();
            initFocals.Clear();
            foreach (GameObject obj in Selection.GetGrippedOrSelection())
            {
                initParentMatrix[obj] = obj.transform.parent.localToWorldMatrix;
                initPositions[obj] = obj.transform.localPosition;
                initRotations[obj] = obj.transform.localRotation;
                initScales[obj] = obj.transform.localScale;

                CameraController cameraController = obj.GetComponent<CameraController>();
                if (null != cameraController)
                    initFocals[obj] = cameraController.focal;
            }
            scale = 1f;
        }

        public void OnGripWorld(bool value)
        {
            if (!gameObject.activeSelf)
                return;

            if (value)
            {
                if (!gripPrevented && Gripping) // no need to interrupt if the grip was prevented
                {
                    OnEndGrip(); // prematurely end the grip action
                    gripInterrupted = true; // set bool to return immediately in the "real" OnEndGrip called when ungripping the controller.
                }
            }
            else
            {
                gripInterrupted = false;
                gripPrevented = false;
            }
        }

        protected void EndUndoGroup()
        {
            if (Gripping)
            {
                gripCmdGroup.Submit();
                gripCmdGroup = null;
                GlobalState.Instance.selectionGripped = false;
            }
        }

        protected virtual void OnStartGrip()
        {
            EndUndoGroup(); // secu
            if (GlobalState.IsGrippingWorld)
            {
                gripPrevented = true;
                return;
            }

            enableToggleTool = false; // NO secondary button tool switch while gripping.

            Selection.SetGrippedObject(Selection.GetHoveredObject());
            SetControllerVisible(Selection.GetGrippedOrSelection().Count == 0);

            ComputeSelectionBounds();
            UpdateSelectionPlanes();

            InitControllerMatrix();
            InitTransforms();
            planesContainer.SetActive(true);
            outOfDeadZone = false;

            gripCmdGroup = new CommandGroup("Grip Selection");
            GlobalState.Instance.selectionGripped = true;
        }

        protected virtual void OnEndGrip()
        {
            planesContainer.SetActive(false);
            SetControllerVisible(true);
            enableToggleTool = true; // TODO: put back the original value, not always true (atm all tools have it to true).

            if (gripPrevented)
            {
                gripPrevented = false;
                return;
            }

            if (gripInterrupted)
            {
                gripInterrupted = false;
                return;
            }

            List<ParametersController> controllers = new List<ParametersController>();
            foreach (var obj in Selection.GetGrippedOrSelection())
            {
                LightController lightController = obj.GetComponentInChildren<LightController>();
                if (null != lightController)
                {
                    controllers.Add(lightController);
                    continue;
                }
                CameraController cameraController = obj.GetComponentInChildren<CameraController>();
                if (null != cameraController)
                {
                    controllers.Add(cameraController);
                    continue;
                }
            }
            if (controllers.Count > 0)
            {
                GlobalState.SetGizmosVisible(controllers.ToArray(), GlobalState.Settings.DisplayGizmos);
            }

            if (!Selection.IsHandleSelected())
            {
                ManageMoveObjectsUndo();
                ManageCamerasFocalsUndo();
                ManageAutoKeyframe();
            }

            EndUndoGroup();
            Selection.SetGrippedObject(null);
        }


        protected void ManageAutoKeyframe()
        {
            if (!GlobalState.Animation.autoKeyEnabled)
                return;
            foreach (GameObject obj in Selection.GetGrippedOrSelection())
            {
                if (!initPositions.ContainsKey(obj))
                    continue;
                if (initPositions[obj] == obj.transform.localPosition && initRotations[obj] == obj.transform.localRotation && initScales[obj] == obj.transform.localScale)
                    continue;
                new CommandAddKeyframes(obj).Submit();
            }
        }

        protected void ManageMoveObjectsUndo()
        {
            List<string> objects = new List<string>();
            List<Vector3> beginPositions = new List<Vector3>();
            List<Quaternion> beginRotations = new List<Quaternion>();
            List<Vector3> beginScales = new List<Vector3>();
            List<Vector3> endPositions = new List<Vector3>();
            List<Quaternion> endRotations = new List<Quaternion>();
            List<Vector3> endScales = new List<Vector3>();

            foreach (GameObject obj in Selection.GetGrippedOrSelection())
            {
                if (!initPositions.ContainsKey(obj))
                    continue;
                if (initPositions[obj] == obj.transform.localPosition && initRotations[obj] == obj.transform.localRotation && initScales[obj] == obj.transform.localScale)
                    continue;
                objects.Add(obj.name);
                beginPositions.Add(initPositions[obj]);
                beginRotations.Add(initRotations[obj]);
                beginScales.Add(initScales[obj]);

                endPositions.Add(obj.transform.localPosition);
                endRotations.Add(obj.transform.localRotation);
                endScales.Add(obj.transform.localScale);
            }

            // A unique command at the end of the move operation
            if (objects.Count > 0)
                new CommandMoveObjects(objects, beginPositions, beginRotations, beginScales, endPositions, endRotations, endScales).Submit();
        }

        protected void SendCameraFocal(CameraController cameraController)
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = cameraController.gameObject.transform;
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
        }

        protected void ManageCamerasFocalsUndo()
        {
            foreach (GameObject obj in initFocals.Keys)
            {
                float oldValue = initFocals[obj];
                CameraController cameraController = obj.GetComponent<CameraController>();
                if (oldValue != cameraController.focal)
                {
                    new CommandSetValue<float>(obj, "Camera Focal", "/CameraController/focal", oldValue).Submit();
                    SendCameraFocal(cameraController);
                }
            }

        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            outOfDeadZone = false;

            int numSelected = Selection.selection.Count;
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Joystick, numSelected > 0);

            // Update locked checkbox if anyone
            if (null != lockedCheckbox)
            {
                int numLocked = 0;
                int numUnlocked = 0;
                foreach (GameObject gobject in Selection.GetGrippedOrSelection())
                {
                    ParametersController parameters = gobject.GetComponent<ParametersController>();
                    if (null != parameters)
                    {
                        if (parameters.Lock) { ++numLocked; }
                        else { ++numUnlocked; }
                    }
                }
                lockedCheckbox.Disabled = false;
                if (numLocked > 0 && numUnlocked == 0)
                {
                    lockedCheckbox.Checked = true;
                }
                else if (numUnlocked > 0 && numLocked == 0)
                {
                    lockedCheckbox.Checked = false;
                }
                else
                {
                    lockedCheckbox.Disabled = true;
                }
            }
        }

        protected CameraController GetSingleSelectedCamera()
        {
            List<GameObject> objecs = Selection.GetGrippedOrSelection();
            if (objecs.Count != 1)
                return null;

            CameraController controller = null;
            foreach (GameObject gObject in objecs)
            {
                controller = gObject.GetComponent<CameraController>();
            }
            return controller;
        }

        protected bool HasDamping()
        {
            if (!Gripping)
                return false;

            List<GameObject> objecs = Selection.GetGrippedOrSelection();
            if (objecs.Count != 1)
                return false;

            foreach (GameObject gObject in objecs)
            {
                if (null == gObject.GetComponent<CameraController>())
                    return false;
            }
            return true;
        }

        private void GetControllerPositionRotation()
        {
            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.primaryController, out position, out rotation);

            if (!HasDamping())
            {
                damping.Clear();
                rightControllerPosition = position;
                rightControllerRotation = rotation;
                return;
            }

            // Compute damping
            float curretnTime = Time.time;
            ControllerDamping dampingElement = new ControllerDamping(curretnTime, position, rotation);
            damping.Add(dampingElement);

            // remove too old values
            ControllerDamping elem = damping[0];
            float dampingDuration = GlobalState.Settings.cameraDamping / 100f * 0.5f;
            while (curretnTime - elem.time > dampingDuration)
            {
                damping.RemoveAt(0);
                elem = damping[0];
            }

            Vector3 positionSum = Vector3.zero;
            Quaternion rotationSum = Quaternion.identity;
            rotationSum.w = 0f;
            float count = (float) damping.Count;
            float factor = 1f / count;
            foreach (ControllerDamping dampingElem in damping)
            {
                positionSum += factor * dampingElem.position;
                rotationSum.x += factor * dampingElem.rotation.x;
                rotationSum.y += factor * dampingElem.rotation.y;
                rotationSum.z += factor * dampingElem.rotation.z;
                rotationSum.w += factor * dampingElem.rotation.w;
            }
            rightControllerPosition = positionSum;
            rightControllerRotation = rotationSum.normalized;
        }

        // A ref for snapping controller
        //private Matrix4x4 SnapController(Matrix4x4 controllerMatrix)
        //{
        //    Matrix4x4 controllerMatrixInWorld = world.worldToLocalMatrix * controllerMatrix;
        //    Vector3 controllerTranslate, controllerScale;
        //    Quaternion controllerRotation;
        //    Maths.DecomposeMatrix(controllerMatrixInWorld, out controllerTranslate, out controllerRotation, out controllerScale);
        //    controllerTranslate = new Vector3((float)Math.Round(controllerTranslate.x, 0), (float)Math.Round(controllerTranslate.y, 0), (float)Math.Round(controllerTranslate.z, 0));

        //    float snapXAngle = 90;
        //    float snapYAngle = 90;
        //    float snapZAngle = 90;
        //    Vector3 eulerAngles = controllerRotation.eulerAngles;
        //    eulerAngles = new Vector3((float)Math.Round(eulerAngles.x / snapXAngle, 0) * snapXAngle, (float)Math.Round(eulerAngles.y / snapYAngle, 0) * snapYAngle, (float)Math.Round(eulerAngles.z / snapZAngle, 0) * snapZAngle);
        //    controllerRotation.eulerAngles = eulerAngles;

        //    return world.localToWorldMatrix * Matrix4x4.TRS(controllerTranslate, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, controllerRotation, Vector3.one);
        //}

        private void UpdateSelect()
        {
            GetControllerPositionRotation();
            // Move & Duplicate selection

            // Duplicate / Stop Record
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primaryButton,
                () => { },
                () =>
                {
                    if (GlobalState.Animation.animationState == AnimationState.Recording || GlobalState.Animation.animationState == AnimationState.Preroll)
                    {
                        GlobalState.Animation.Pause();
                        return;
                    }

                    if (!Selection.IsHandleSelected())
                    {
                        List<GameObject> objects = Selection.GetGrippedOrSelection();

                        if (objects.Count > 0)
                        {
                            DuplicateSelection();

                            InitControllerMatrix();
                            InitTransforms();
                            outOfDeadZone = true;
                        }
                    }
                }
            );

            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip, OnStartGrip, OnEndGrip);

            if (Gripping)
            {
                Vector3 p = rightControllerPosition;
                Quaternion r = rightControllerRotation;

                if (!outOfDeadZone && Vector3.Distance(p, initControllerPosition) > deadZoneDistance)
                    outOfDeadZone = true;

                if (!outOfDeadZone)
                    return;

                // Joystick zoom only for non-handle objects
                if (!Selection.IsHandleSelected())
                {
                    if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                    {
                        Vector2 joystickAxis = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                        float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                        if (joystickAxis.y > deadZone)
                            scale *= scaleFactor;
                        if (joystickAxis.y < -deadZone)
                            scale /= scaleFactor;
                    }
                }

                TransformSelection(Snap(rightMouthpieces.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * scale)));
                ComputeSelectionBounds();
                UpdateSelectionPlanes();
            }


            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 joystickAxis = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);

                if (joystickAxis != Vector2.zero)
                {
                    CameraController cameraController = GetSingleSelectedCamera();
                    if (null != cameraController)
                    {
                        if (null == cameraFocalCommand && !Gripping)
                        {
                            // allow camera focal change when not gripped
                            cameraFocalCommand = new CommandSetValue<float>(cameraController.gameObject, "Camera Focal", "/CameraController/focal");
                        }

                        float currentFocal = cameraController.focal;
                        float focalFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;

                        if (joystickAxis.x > deadZone)
                            currentFocal *= focalFactor;
                        if (joystickAxis.x < -deadZone)
                            currentFocal /= focalFactor;

                        if (currentFocal < 10f)
                            currentFocal = 10f;
                        if (currentFocal > 300f)
                            currentFocal = 300f;

                        // Don't RoundToInt here since currentFocal may stick to the lowest value 10
                        cameraController.focal = currentFocal;
                        SendCameraFocal(cameraController);
                    }

                    foreach (GameObject obj in Selection.selection.Values)
                    {
                        ParametersController controller = obj.GetComponent<ParametersController>();
                        if (null == controller)
                        {
                            Vector3 currentScale = obj.transform.localScale;
                            float newScale = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                            if (joystickAxis.x > deadZone)
                                currentScale *= newScale;
                            if (joystickAxis.x < -deadZone)
                                currentScale /= newScale;

                            if (currentScale.x < 0.0001f)
                                continue;
                            if (currentScale.x > 1000f)
                                continue;

                            obj.transform.localScale = currentScale;
                        }

                        if (!joystickScaling && !Gripping)
                        {
                            InitTransforms();
                            joystickScaling = true;
                        }
                    }
                }
                else
                {
                    SubmitCameraFocalCommand();
                    if (joystickScaling)
                    {
                        joystickScaling = false;
                        ManageMoveObjectsUndo();
                        ManageAutoKeyframe();
                    }
                }
            }
        }

        public void ComputeSelectionBounds()
        {
            // Get bounds
            minBound = Vector3.positiveInfinity;
            maxBound = Vector3.negativeInfinity;
            hasBounds = false;
            List<GameObject> selectedObjects = Selection.GetGrippedOrSelection();
            int selectionCount = selectedObjects.Count;

            bool foundHierarchicalObject = false;
            if (selectionCount == 1)
            {
                foundHierarchicalObject = IsHierarchical(selectedObjects);
            }            

            foreach (GameObject obj in selectedObjects)
            {
                MeshFilter meshFilter = obj.GetComponentInChildren<MeshFilter>();
                if (null != meshFilter)
                {
                    Matrix4x4 transformMatrix;
                    if (selectionCount > 1 || foundHierarchicalObject)
                    {
                        if (meshFilter.gameObject != obj)
                        {
                            transformMatrix = rightHanded.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                        }
                        else
                        {
                            transformMatrix = rightHanded.worldToLocalMatrix * obj.transform.localToWorldMatrix;
                        }
                    }
                    else
                    {
                        transformMatrix = Matrix4x4.identity;
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
                        vertices[i] = transformMatrix.MultiplyPoint(vertices[i]);
                        //  Compute min and max bounds
                        if (vertices[i].x < minBound.x) { minBound.x = vertices[i].x; }
                        if (vertices[i].y < minBound.y) { minBound.y = vertices[i].y; }
                        if (vertices[i].z < minBound.z) { minBound.z = vertices[i].z; }

                        if (vertices[i].x > maxBound.x) { maxBound.x = vertices[i].x; }
                        if (vertices[i].y > maxBound.y) { maxBound.y = vertices[i].y; }
                        if (vertices[i].z > maxBound.z) { maxBound.z = vertices[i].z; }
                    }
                    hasBounds = true;
                }
            }
            if(hasBounds)
            {
                planePositions = new Vector3[6];
                planePositions[0] = new Vector3((maxBound.x + minBound.x) * 0.5f, maxBound.y, (maxBound.z + minBound.z) * 0.5f);
                planePositions[1] = new Vector3((maxBound.x + minBound.x) * 0.5f, minBound.y, (maxBound.z + minBound.z) * 0.5f);
                planePositions[2] = new Vector3(minBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
                planePositions[3] = new Vector3(maxBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
                planePositions[4] = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, minBound.z);
                planePositions[5] = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, maxBound.z);
            }

            if (selectionCount == 1 && !foundHierarchicalObject)
            {
                Transform transform = selectedObjects[0].GetComponentInChildren<MeshFilter>().transform;
                planeContainerMatrix = rightHanded.worldToLocalMatrix * transform.localToWorldMatrix;
            }
            else
            {
                planeContainerMatrix = Matrix4x4.identity;
            }
        }

        public void UpdateSelectionPlanes()
        {
            Maths.DecomposeMatrix(planeContainerMatrix, out Vector3 planePosition, out Quaternion planeRotation, out Vector3 planeScale);
            planesContainer.transform.localPosition = planePosition;
            planesContainer.transform.localRotation = planeRotation;
            planesContainer.transform.localScale = planeScale;
            
            if (!hasBounds)
            {
                planesContainer.SetActive(false);
                return;
            }

            Vector3 bs = planesContainer.transform.localScale; // boundsScale

            // Collider Scale
            Vector3 cs = new Vector3(
                collidersThickness * (1.0f / bs.x),
                collidersThickness * (1.0f / bs.y),
                collidersThickness * (1.0f / bs.z)
            );

            // GAP: fixed in camera space. Scales with world and objet scales, inverse.
            Vector3 g = new Vector3(
                cameraSpaceGap * (1.0f / bs.x),
                cameraSpaceGap * (1.0f / bs.y),
                cameraSpaceGap * (1.0f / bs.z)
            );

            Vector3 minGapBound = minBound - new Vector3(g.x, g.y, g.z);
            Vector3 maxGapBound = maxBound + new Vector3(g.x, g.y, g.z);

            Vector3 delta = (maxGapBound - minGapBound) * 0.5f;

            // Set planes (depending on their initial rotation)
            // Top
            planes[0].transform.localPosition = planePositions[0];
            planes[0].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, g.y, -delta.z), new Vector3(-delta.x, g.y, delta.z), new Vector3(delta.x, g.y, delta.z), new Vector3(delta.x, g.y, -delta.z));
            SetPlaneCollider(planes[0], new Vector3(0, g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Bottom
            planes[1].transform.localPosition = planePositions[1];
            planes[1].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -g.y, -delta.z), new Vector3(delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, -delta.z));
            SetPlaneCollider(planes[1], new Vector3(0, -g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Left
            planes[2].transform.localPosition = planePositions[2];
            planes[2].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-g.x, -delta.y, -delta.z), new Vector3(-g.x, -delta.y, delta.z), new Vector3(-g.x, delta.y, delta.z), new Vector3(-g.x, delta.y, -delta.z));
            SetPlaneCollider(planes[2], new Vector3(-g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Right
            planes[3].transform.localPosition = planePositions[3];
            planes[3].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(g.x, delta.y, -delta.z), new Vector3(g.x, delta.y, delta.z), new Vector3(g.x, -delta.y, delta.z), new Vector3(g.x, -delta.y, -delta.z));
            SetPlaneCollider(planes[3], new Vector3(g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Front
            planes[4].transform.localPosition = planePositions[4];
            planes[4].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, -delta.y, -g.z), new Vector3(-delta.x, delta.y, -g.z), new Vector3(delta.x, delta.y, -g.z), new Vector3(delta.x, -delta.y, -g.z));
            SetPlaneCollider(planes[4], new Vector3(0, 0, -g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));

            // Back
            planes[5].transform.localPosition = planePositions[5];
            planes[5].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -delta.y, g.z), new Vector3(delta.x, delta.y, g.z), new Vector3(-delta.x, delta.y, g.z), new Vector3(-delta.x, -delta.y, g.z));
            SetPlaneCollider(planes[5], new Vector3(0, 0, g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));

            planesContainer.SetActive(true);
        }

        protected void InitControllerMatrix()
        {
            if (!hasBounds)
                return;

            initMouthPieceWorldToLocal = rightMouthpieces.worldToLocalMatrix;

            Vector3 worldPlanePosition = planesContainer.transform.TransformPoint(planePositions[1]);
            bottomRay = new Ray(rightMouthpieces.transform.InverseTransformPoint(worldPlanePosition), rightMouthpieces.transform.InverseTransformDirection(-planesContainer.transform.up));
        }

        private Matrix4x4 Snap(Matrix4x4 currentMouthPieceLocalToWorld)
        {
            int layersMask = LayerMask.GetMask(new string[] { "Default" });

            Ray ray = new Ray(rightMouthpieces.TransformPoint(bottomRay.origin), rightMouthpieces.TransformDirection(bottomRay.direction));
            if (Physics.Raycast(ray, out RaycastHit hit, snapDistance / GlobalState.WorldScale, layersMask))
            {
                Vector3 hitPoint = rightMouthpieces.TransformPoint(rightMouthpieces.InverseTransformPoint(hit.point) - bottomRay.origin);

                // set position to hit point
                currentMouthPieceLocalToWorld.SetColumn(3, new Vector4(hitPoint.x, hitPoint.y, hitPoint.z, 1));

                // compute rotation to align up vector to hit normal
                Matrix4x4 T = Matrix4x4.Translate(-hit.point);
                Matrix4x4 R = Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(-ray.direction, hit.normal), Vector3.one);

                return T.inverse * R * T * currentMouthPieceLocalToWorld * initMouthPieceWorldToLocal;
            }
            else
            {
                return currentMouthPieceLocalToWorld * initMouthPieceWorldToLocal;
            }
        }
        protected void TransformSelection(Matrix4x4 transformation)
        {
            foreach (GameObject obj in Selection.GetGrippedOrSelection())
            {
                // Check constraints
                if (ConstraintManager.IsLocked(obj)) { continue; }

                var meshParentTransform = obj.transform.parent;
                Matrix4x4 meshParentMatrixInverse = new Matrix4x4();
                if (meshParentTransform)
                    meshParentMatrixInverse = meshParentTransform.worldToLocalMatrix;
                else
                    meshParentMatrixInverse = Matrix4x4.identity;
                Matrix4x4 transformed = meshParentMatrixInverse * transformation * initParentMatrix[obj] * Matrix4x4.TRS(initPositions[obj], initRotations[obj], initScales[obj]);

                if (obj.transform.localToWorldMatrix != transformed)
                {
                    // UI objects
                    if (obj.GetComponent<UIHandle>())
                    {
                        obj.transform.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                        obj.transform.localRotation = Quaternion.LookRotation(transformed.GetColumn(2), transformed.GetColumn(1));
                        //obj.transform.localScale = new Vector3(transformed.GetColumn(0).magnitude, transformed.GetColumn(1).magnitude, transformed.GetColumn(2).magnitude);
                    }
                    // Standard game objects
                    else
                    {
                        Matrix4x4 mat = transformed;  // copy

                        // Constraints and locked properties may change the final transform
                        // TODO

                        OnPreTransformSelection(obj.transform, ref mat);
                        // Set matrix
                        SyncData.SetTransform(obj.name, mat);

                        // Send a live sync while moving
                        CommandManager.SendEvent(MessageType.Transform, obj.transform);
                        GlobalState.FireObjectMoving(obj);
                    }
                }
            }
        }

        public void OnPreTransformSelection(Transform transform, ref Matrix4x4 transformed)
        {
            // Constrain movement
            bool lockPosition = false;
            bool lockRotation = false;
            bool lockScale = false;
            ParametersController parametersController = transform.gameObject.GetComponent<ParametersController>();
            if (null != parametersController)
            {
                lockPosition = parametersController.lockPosition;
                lockRotation = parametersController.lockRotation;
                lockScale = parametersController.lockScale;
            }

            Maths.DecomposeMatrix(transformed, out Vector3 newPosition, out Quaternion newRotation, out Vector3 newScale);

            // Translate
            if (lockPosition)
            {
                newPosition = transform.localPosition;
            }

            if (lockRotation)
            {
                newRotation = transform.localRotation;
            }

            if (lockScale)
            {
                newScale = transform.localScale;
            }
            // transformation matrix (local)
            transformed = Matrix4x4.TRS(newPosition, newRotation, newScale);

        }

        private void UpdateEraser()
        {
            // Nothing for now
        }

        private List<GameObject> GetGroupSiblings(GameObject gObject)
        {
            List<GameObject> objects = new List<GameObject>();
            Transform parent = gObject.transform.parent;
            if (parent.name.StartsWith("Group__"))
            {
                for (int i = 0; i < parent.childCount; i++)
                    objects.Add(parent.GetChild(i).gameObject);
            }
            else
            {
                objects.Add(gObject);
            }
            return objects;
        }

        public bool AddToSelection(GameObject gObject)
        {
            // Selection is EXCLUSIVE between windows and objects.
            if (gObject.GetComponent<UIHandle>()) // if we select a UI handle, deselect all other objects first.
            {
                ClearSelection();
                bool res = Selection.AddToSelection(gObject);
                new CommandAddToSelection(gObject).Submit();
                return res;
            }
            else if (!Selection.IsHandleSelected()) // Dont select things if we have a window selected.
            {
                bool res = Selection.AddToSelection(gObject);
                new CommandAddToSelection(gObject).Submit();
                return res;
            }

            return false;
        }

        public void AddSiblingsToSelection(GameObject gObject, bool haptic = true)
        {
            List<GameObject> objects = GetGroupSiblings(gObject);
            List<GameObject> objectsAddedToSelection = new List<GameObject>();
            foreach (GameObject gobj in objects)
            {
                if (Selection.IsSelected(gobj))
                    continue;
                if (AddToSelection(gobj))
                    objectsAddedToSelection.Add(gobj);
            }

            if (haptic && objectsAddedToSelection.Count > 0)
            {
                VRInput.SendHapticImpulse(VRInput.primaryController, 0, 1, 0.1f);
            }
        }

        public bool RemoveFromSelection(GameObject gObject)
        {
            new CommandRemoveFromSelection(gObject).Submit();
            return Selection.RemoveFromSelection(gObject);
        }

        public void RemoveSiblingsFromSelection(GameObject gObject, bool haptic = true)
        {
            List<GameObject> objects = GetGroupSiblings(gObject);

            List<GameObject> objectsRemovedFromSelection = new List<GameObject>();
            foreach (GameObject gobj in objects)
            {
                if (!Selection.IsSelected(gobj))
                    continue;

                if (RemoveFromSelection(gobj))
                {
                    objectsRemovedFromSelection.Add(gobj);
                }
            }

            if (haptic && objectsRemovedFromSelection.Count > 0)
            {
                VRInput.SendHapticImpulse(VRInput.primaryController, 0, 1, 0.1f);
            }
        }

        public void ClearSelection()
        {
            List<GameObject> objects = new List<GameObject>();
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                objects.Add(data.Value);
            }
            new CommandRemoveFromSelection(objects).Submit();

            Selection.ClearSelection();
        }

        public GameObject DuplicateObject(GameObject source, bool withVFX = true)
        {
            if (source.GetComponent<UIHandle>())
                return null;

            GameObject clone = SyncData.Duplicate(source);
            if (null == clone)
                return null;
            new CommandDuplicateGameObject(clone, source).Submit();

            // Add a selectionVFX instance on the duplicated objects
            if (withVFX)
            {
                ToolsUIManager.Instance.SpawnCreateInstanceVFX(clone);
            }

            if (Selection.IsSelected(source))
            {
                Selection.RemoveFromSelection(source);
                new CommandRemoveFromSelection(source).Submit();
                Selection.AddToSelection(clone);
                new CommandAddToSelection(clone).Submit();
            }
            if (source == Selection.GetGrippedObject())
            {
                Selection.SetGrippedObject(clone);
            }

            selectorTrigger.ClearCollidedObjects();
            return clone;
        }

        public void DuplicateSelection()
        {
            CommandGroup group = new CommandGroup("Duplicate Selection");
            try
            {
                List<GameObject> objectsToBeDuplicated = Selection.GetGrippedOrSelection();
                foreach (GameObject obj in objectsToBeDuplicated)
                    DuplicateObject(obj);
                ManageMoveObjectsUndo();
            }
            finally
            {
                group.Submit();
            }
        }

        public void EraseSelection()
        {
            List<GameObject> selectedObjects = Selection.GetGrippedOrSelection();

            CommandGroup group = new CommandGroup("Erase Selection");
            try
            {
                foreach (GameObject o in selectedObjects)
                {
                    //RemoveCollidedObject(o);
                    RemoveSiblingsFromSelection(o, false);
                    new CommandRemoveGameObject(o).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        public void OnSelectMode()
        {
            mode = SelectorModes.Select;
            mouthpiece.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            updateButtonsColor();
        }

        public void OnEraserMode()
        {
            mode = SelectorModes.Eraser;
            mouthpiece.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", eraseColor);
            updateButtonsColor();
        }

        public Color GetModeColor()
        {
            if (mode == SelectorModes.Select) { return selectionColor; }
            return eraseColor;
        }

        void updateButtonsColor()
        {
            if (!panel)
                return;

            // NOTE: currently the SelectorPanel has 4 children of type UIButton
            //       which have on Canvas children, with in turn has one Image and one Text children.
            // TODO: do a proper Radio Button Group
            for (int i = 0; i < panel.childCount; i++)
            {
                GameObject child = panel.GetChild(i).gameObject;
                UIButton button = child.GetComponent<UIButton>();
                if (button != null)
                {
                    button.Checked = false;

                    if (child.name == "Select" && mode == SelectorModes.Select)
                    {
                        button.Checked = true;
                    }
                    if (child.name == "Eraser" && mode == SelectorModes.Eraser)
                    {
                        button.Checked = true;
                    }
                }
            }
        }

        public void OnLinkAction()
        {
            // TODO
            /*
            if (Selection.selection.Count <= 1)
                return;

            GameObject container = new GameObject("Group__" + groupId.ToString());
            groupId++;

            SortedSet<Transform> groups = new SortedSet<Transform>();

            bool groupHasParent = false;
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                Transform parent = gobject.transform.parent;
                if (parent && parent.name.StartsWith("Group__"))
                {
                    if (!groupHasParent)
                    {
                        container.transform.parent = parent.parent;
                        container.transform.localPosition = Vector3.zero;
                        container.transform.localRotation = Quaternion.identity;
                        container.transform.localScale = Vector3.one;
                        groupHasParent = true;
                    }

                    // reparent
                    groups.Add(gobject.transform.parent);
                    gobject.transform.parent = container.transform;
                }
                else
                {
                    if (!groupHasParent)
                    {
                        container.transform.parent = data.Value.transform.parent;
                        groupHasParent = true;
                    }
                    data.Value.transform.parent = container.transform;
                }
            }

            foreach (Transform group in groups)
            {
                if (group.childCount == 0)
                    Destroy(group.gameObject);
            }
            */
        }

        public void OnUnlinkAction()
        {
            // TODO
            /*
            SortedSet<Transform> groups = new SortedSet<Transform>();
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                Transform parent = gobject.transform.parent;
                if (parent && parent.name.StartsWith("Group__"))
                {
                    groups.Add(gobject.transform.parent);
                }
            }

            foreach (Transform group in groups)
            {
                for (int i = group.childCount - 1; i >= 0; i--)
                {
                    group.GetChild(i).parent = group.parent;
                }
                Destroy(group.gameObject);
            }
            */
        }

    }
}
